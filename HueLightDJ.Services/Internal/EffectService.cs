using HueApi.ColorConverters;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects;
using HueLightDJ.Effects.Base;
using HueLightDJ.Effects.Layers;
using HueLightDJ.Services.Interfaces;
using HueLightDJ.Services.Interfaces.Models;
using HueLightDJ.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Services
{
  public class EffectService
  {
    private static List<TypeInfo> EffectTypes { get; set; } = new List<TypeInfo>();
    private static List<TypeInfo> GroupEffectTypes { get; set; } = new List<TypeInfo>();
    private static List<TypeInfo> TouchEffectTypes { get; set; } = new List<TypeInfo>();
    private static Dictionary<EntertainmentLayer, RunningEffectInfo> layerInfo = new Dictionary<EntertainmentLayer, RunningEffectInfo>();

    private static CancellationTokenSource? autoModeCts;
    public static bool AutoModeHasRandomEffects = true;
    private readonly IHubService hub;

    public EffectService(IHubService hub)
    {
      this.hub = hub;
    }

    public static List<TypeInfo> GetEffectTypes()
    {
      if (!EffectTypes.Any())
      {
        var all = LoadAllEffects<IHueEffect>();
        EffectTypes = all;
      }

      return EffectTypes;
    }

    public static List<TypeInfo> GetGroupEffectTypes()
    {
      if (!GroupEffectTypes.Any())
      {
        var all = LoadAllEffects<IHueGroupEffect>();
        GroupEffectTypes = all;
      }

      return GroupEffectTypes;
    }

    public static List<TypeInfo> GetTouchEffectTypes()
    {
      if (!TouchEffectTypes.Any())
      {
        var all = LoadAllEffects<IHueTouchEffect>();
        TouchEffectTypes = all;
      }

      return TouchEffectTypes;
    }

    public static EffectsVM GetEffectViewModels()
    {
      var all = GetEffectTypes();
      var groupEffectsTypes = GetGroupEffectTypes();



      List<EffectList> baseEffects = new();
      List<EffectViewModel> shortEffects = new List<EffectViewModel>();
      List<EffectViewModel> groupEffects = new List<EffectViewModel>();
      foreach (var type in all)
      {
        var hueEffectAtt = type.GetCustomAttribute<HueEffectAttribute>();
        if (hueEffectAtt == null)
          continue;

        var effect = new EffectViewModel()
        {
          Name = hueEffectAtt.Name,
          TypeName = type.Name,
        };
        effect.HasColorPicker = hueEffectAtt.HasColorPicker;

        if (!string.IsNullOrEmpty(hueEffectAtt.DefaultColor))
        {
          effect.Color = hueEffectAtt.DefaultColor;
          effect.IsRandom = false;
        }

        if (hueEffectAtt.IsBaseEffect)
        {
          if (!baseEffects.Where(x => x.Title == hueEffectAtt.Group).Any())
            baseEffects.Add(new EffectList { Title = hueEffectAtt.Group });

          var current = baseEffects.Where(x => x.Title == hueEffectAtt.Group).Select(x => x).Single();
          current.Effects.Add(effect);
        }
        else
          shortEffects.Add(effect);
      }

      foreach (var type in groupEffectsTypes)
      {
        var hueEffectAtt = type.GetCustomAttribute<HueEffectAttribute>();
        if (hueEffectAtt == null)
          continue;

        var effect = new EffectViewModel()
        {
          Name = hueEffectAtt.Name,
          TypeName = type.Name
        };
        effect.HasColorPicker = hueEffectAtt.HasColorPicker;

        if (!string.IsNullOrEmpty(hueEffectAtt.DefaultColor))
        {
          effect.Color = hueEffectAtt.DefaultColor;
          effect.IsRandom = false;
        }

        groupEffects.Add(effect);
      }

      List<string> iteratorNames = new List<string>();
      foreach (var name in Enum.GetNames(typeof(IteratorEffectMode)))
      {
        iteratorNames.Add(name);
      }

      List<string> secondaryIteratorNames = new List<string>() {
        IteratorEffectMode.All.ToString(),
        IteratorEffectMode.AllIndividual.ToString(),
        IteratorEffectMode.Bounce.ToString(),
        IteratorEffectMode.Single.ToString(),
        IteratorEffectMode.Random.ToString(),
      };

      var vm = new EffectsVM();
      vm.BaseEffects = baseEffects;
      vm.ShortEffects = shortEffects;
      vm.GroupEffects = groupEffects;
      vm.IteratorModes = iteratorNames;
      vm.SecondaryIteratorModes = secondaryIteratorNames;

      return vm;
    }

    public void StopEffects()
    {
      foreach (var layer in layerInfo)
      {
        layer.Value?.CancellationTokenSource?.Cancel();
      }
      hub.StatusChanged();
    }

    public void StartAutoMode()
    {
      autoModeCts?.Cancel();
      autoModeCts = new CancellationTokenSource();

      Task.Run(async () =>
      {
        while (!autoModeCts.IsCancellationRequested)
        {
          StartRandomEffect(AutoModeHasRandomEffects);

          var secondsToWait = StreamingSetup.WaitTime.Value.TotalSeconds > 1 ? 18 : 6; //low bpm? play effect longer
          await Task.Delay(TimeSpan.FromSeconds(secondsToWait));
        }
      }, autoModeCts.Token);

    }

    public void StopAutoMode()
    {
      autoModeCts?.Cancel();
      hub.StatusChanged();
    }

    public static bool IsAutoModeRunning()
    {
      if (autoModeCts == null || autoModeCts.IsCancellationRequested)
        return false;

      return true;
    }

    public void StartEffect(string typeName, string? colorHex, string? group = null, IteratorEffectMode iteratorMode = IteratorEffectMode.All, IteratorEffectMode secondaryIteratorMode = IteratorEffectMode.All)
    {
      var all = GetEffectTypes();
      var allGroup = GetGroupEffectTypes();

      var effectType = all.Where(x => x.Name == typeName).FirstOrDefault();
      var groupEffectType = allGroup.Where(x => x.Name == typeName).FirstOrDefault();

      bool isGroupEffect = groupEffectType != null && !string.IsNullOrEmpty(group);
      var selectedEffect = isGroupEffect ? groupEffectType : effectType;

      if (selectedEffect != null)
      {
        var hueEffectAtt = selectedEffect.GetCustomAttribute<HueEffectAttribute>();
        if (hueEffectAtt == null)
          return;

        var isBaseLayer = hueEffectAtt.IsBaseEffect && iteratorMode != IteratorEffectMode.Single && iteratorMode != IteratorEffectMode.RandomOrdered;
        var layer = GetLayer(isBaseLayer);

        if (layerInfo.ContainsKey(layer))
        {
          //Cancel currently running job
          layerInfo[layer].CancellationTokenSource?.Cancel();
        }

        CancellationTokenSource cts = new CancellationTokenSource();
        layerInfo[layer] = new RunningEffectInfo() { Name = hueEffectAtt.Name, CancellationTokenSource = cts };

        Func<TimeSpan> waitTime = () => StreamingSetup.WaitTime;
        RGBColor? color = null;
        if (!string.IsNullOrEmpty(colorHex))
          color = new RGBColor(colorHex);


        if (isGroupEffect)
        {
          //get group
          var selectedGroup = GroupService.GetAll(layer).Where(x => x.Name == group).Select(x => x.Lights).FirstOrDefault();

          if (selectedGroup != null)
            StartEffect(cts.Token, selectedEffect, selectedGroup.SelectMany(x => x), group!, waitTime, color, iteratorMode, secondaryIteratorMode);
        }
        else
        {
          StartEffect(cts.Token, selectedEffect, layer, waitTime, color);
        }

      }
    }

    private void StartEffect(CancellationToken ctsToken, TypeInfo selectedEffect, IEnumerable<IEnumerable<EntertainmentLight>> group, string groupName, Func<TimeSpan> waitTime, RGBColor? color, IteratorEffectMode iteratorMode = IteratorEffectMode.All, IteratorEffectMode secondaryIteratorMode = IteratorEffectMode.All)
    {
      MethodInfo? methodInfo = selectedEffect.GetMethod("Start");
      if (methodInfo == null)
        return;

      //get group
      if (group == null)
        group = GroupService.GetRandomGroup();

      object?[] parametersArray = new object?[] { group, waitTime, color, iteratorMode, secondaryIteratorMode, ctsToken };

      object? classInstance = Activator.CreateInstance(selectedEffect, null);
      methodInfo.Invoke(classInstance, parametersArray);

      hub.SendAsync("StartingEffect", $"Starting: {selectedEffect.Name} {groupName}, {iteratorMode}-{secondaryIteratorMode} {color?.ToHex()}",
          new EffectLogMsg()
          {
            EffectType = "group",
            Name = selectedEffect.Name,
            RGBColor = color?.ToHex(),
            Group = groupName,
            IteratorMode = iteratorMode.ToString(),
            SecondaryIteratorMode = secondaryIteratorMode.ToString(),

          });

    }

    private void StartTouchEffect(CancellationToken ctsToken, TypeInfo selectedEffect, Func<TimeSpan> waitTime, RGBColor? color, double x, double y)
    {
      MethodInfo? methodInfo = selectedEffect.GetMethod("Start");
      if (methodInfo == null)
        return;

      var layer = GetLayer(isBaseLayer: false);

      object?[] parametersArray = new object?[] { layer, waitTime, color, ctsToken, x, y };

      object? classInstance = Activator.CreateInstance(selectedEffect, null);
      methodInfo.Invoke(classInstance, parametersArray);
    }

    private void StartEffect(CancellationToken ctsToken, TypeInfo selectedEffect, EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color)
    {
      MethodInfo? methodInfo = selectedEffect.GetMethod("Start");
      if (methodInfo == null)
        return;

      object?[] parametersArray = new object?[] { layer, waitTime, color, ctsToken };

      object? classInstance = Activator.CreateInstance(selectedEffect, null);
      methodInfo.Invoke(classInstance, parametersArray);

      hub.SendAsync("StartingEffect", $"Starting: {selectedEffect.Name} {color?.ToHex()}", new EffectLogMsg() { Name = selectedEffect.Name, RGBColor = color?.ToHex() });
      hub.StatusChanged();
    }

    public void StartRandomEffect(bool withRandomEffects = true)
    {
      var r = new Random();

      var all = GetEffectTypes();
      var allGroup = GetGroupEffectTypes();

      all ??= new List<TypeInfo>();


      if (r.NextDouble() <= (withRandomEffects ? 0.4 : 0))
        StartRandomGroupEffect();
      else
      {
        string? effect = all
          .Where(x => x.Name != typeof(ChristmasEffect).Name)
          .Where(x => x.Name != typeof(AllOffEffect).Name)
          .Where(x => x.Name != typeof(SetColorEffect).Name)
          .Where(x => x.Name != typeof(DemoEffect).Name)
          .OrderBy(x => Guid.NewGuid()).FirstOrDefault()?.Name;

        GenerateRandomEffectSettings(out RGBColor hexColor, out _, out _);

        if (effect != null)
          StartEffect(effect, hexColor.ToHex());
      }

    }

    private void StartRandomGroupEffect(bool useMultipleEffects = true)
    {
      Func<TimeSpan> waitTime = () => StreamingSetup.WaitTime;

      Random r = new Random();
      var allGroupEffects = GetGroupEffectTypes();

      //Always run on baselayer
      var layer = GetLayer(isBaseLayer: true);

      //Random group that supports multiple effects
      var group = GroupService.GetAll(layer).OrderBy(x => Guid.NewGuid()).FirstOrDefault();
      if (group == null)
        return;

      //Get same number of effects as groups in the light list
      var effects = allGroupEffects.OrderBy(x => Guid.NewGuid()).Take(group.MaxEffects).ToList();

      //Cancel current
      if (layerInfo.ContainsKey(layer))
      {
        //Cancel currently running job
        layerInfo[layer].CancellationTokenSource?.Cancel();
      }

      CancellationTokenSource cts = new CancellationTokenSource();
      layerInfo[layer] = new RunningEffectInfo() { Name = "Double random", CancellationTokenSource = cts };


      for (int i = 0; i < group.Lights.Count; i++)
      {
        var section = group.Lights[i];
        GenerateRandomEffectSettings(out RGBColor hexColor, out IteratorEffectMode iteratorMode, out IteratorEffectMode iteratorSecondaryMode);

        if (group.Lights.Count == 1
          && iteratorSecondaryMode != IteratorEffectMode.All
          && (effects[i] == typeof(HueLightDJ.Effects.Group.RandomColorsEffect) || effects[i] == typeof(HueLightDJ.Effects.Group.RandomColorloopEffect))
          )
        {
          //Random colors on all individual is boring, start another effect!
          StartRandomEffect();
          break;
        }

        StartEffect(cts.Token, effects[i], section, group.Name, waitTime, hexColor, iteratorMode, iteratorSecondaryMode);
      }


    }

    private static void GenerateRandomEffectSettings(out RGBColor hexColor, out IteratorEffectMode iteratorMode, out IteratorEffectMode iteratorSecondaryMode)
    {
      Random r = new Random();
      hexColor = RGBColor.Random(r);
      while (hexColor.R < 0.15 && hexColor.G < 0.15 && hexColor.B < 0.15)
        hexColor = RGBColor.Random(r);

      Array values = Enum.GetValues(typeof(IteratorEffectMode));
      iteratorMode = (IteratorEffectMode?)values.GetValue(r.Next(values.Length)) ?? IteratorEffectMode.All;
      iteratorSecondaryMode = (IteratorEffectMode?)values.GetValue(r.Next(values.Length)) ?? IteratorEffectMode.Random;

      //Bounce and Single are no fun for random mode
      if (iteratorMode == IteratorEffectMode.Bounce || iteratorMode == IteratorEffectMode.Single)
        iteratorMode = IteratorEffectMode.Cycle;
      else if (iteratorMode == IteratorEffectMode.RandomOrdered) //RandomOrdered only runs once
        iteratorMode = IteratorEffectMode.Random;
    }

    private static EntertainmentLayer GetLayer(bool isBaseLayer)
    {
      if (StreamingSetup.Layers == null || !StreamingSetup.Layers.Any())
        throw new Exception("No layers found.");

      if (isBaseLayer)
        return StreamingSetup.Layers.First();

      return StreamingSetup.Layers.Last();
    }

    private static List<TypeInfo> LoadAllEffects<T>()
    {
      Dictionary<TypeInfo, HueEffectAttribute> result = new Dictionary<TypeInfo, HueEffectAttribute>();

      //Get all effects that implement IHueEffect
      Assembly ass = typeof(T).Assembly;

      foreach (TypeInfo ti in ass.DefinedTypes)
      {
        if (ti.ImplementedInterfaces.Contains(typeof(T)))
        {
          var hueEffectAtt = ti.GetCustomAttribute<HueEffectAttribute>();

          if (hueEffectAtt != null)
          {
            result.Add(ti, hueEffectAtt);
          }
        }
      }

      return result.OrderBy(x => x.Value.Order).Select(x => x.Key).ToList();
    }

    public void CancelAllEffects()
    {
      StopAutoMode();

      foreach (var layer in layerInfo)
      {
        layer.Value?.CancellationTokenSource?.Cancel();
      }
    }

    public void StartRandomTouchEffect(double x, double y)
    {
      var effectLayer = GetLayer(isBaseLayer: false);

      var randomTouch = GetTouchEffectTypes().OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
      if (randomTouch == null)
        return;

      Func<TimeSpan> waitTime = () => StreamingSetup.WaitTime;

      StartTouchEffect(CancellationToken.None, randomTouch, waitTime, null, x, y);
    }

    public void Beat(double intensity)
    {
      var effectLayer = GetLayer(isBaseLayer: false);

      //var effects = GetEffectTypes().Where(x => x.GetType() == typeof(RandomFlashEffect)).FirstOrDefault();

      Func<TimeSpan> waitTime = () => TimeSpan.FromMilliseconds(100);

      StartEffect(default(CancellationToken), typeof(FlashFadeEffect).GetTypeInfo(), effectLayer, waitTime, RGBColor.Random());
    }
  }
}
