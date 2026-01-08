using HueApi.Entertainment.Extensions;
using HueLightDJ.Services.Interfaces;
using HueLightDJ.Services.Interfaces.Models;
using HueLightDJ.Services.Interfaces.Models.Requests;
using ProtoBuf.Grpc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HueLightDJ.Services
{
  public class LightDJService : ILightDJService
  {
    private readonly EffectService effectService;
    private readonly StreamingSetup streamingSetup;

    public LightDJService(EffectService effectService, StreamingSetup streamingSetup)
    {
      this.effectService = effectService;
      this.streamingSetup = streamingSetup;
    }

    public Task Connect(GroupConfiguration config, CallContext context = default)
    {
      //Connect
      effectService.StopEffects();
      return streamingSetup.SetupAndReturnGroupAsync(config);
    }

    public Task<StatusModel> GetStatus(CallContext context = default)
    {
      StatusModel vm = new StatusModel();
      vm.Bpm = StreamingSetup.GetBPM();
      vm.IsAutoMode = EffectService.IsAutoModeRunning();
      vm.AutoModeHasRandomEffects = EffectService.AutoModeHasRandomEffects;
      vm.ShowDisconnect = !(StreamingSetup.CurrentConnection?.HideDisconnect ?? false);
      vm.CurrentGroup = StreamingSetup.CurrentConnection;

      if (StreamingSetup.CurrentConnection != null)
      {
        var groups = GroupService.GetAll();
        vm.Groups = groups.Select(x => new GroupInfoViewModel() { Name = x.Name }).ToList();
      }

      return Task.FromResult(vm);
    }

    public Task<EffectsVM> GetEffects(CallContext context = default)
    {
      return Task.FromResult(EffectService.GetEffectViewModels());
    }

    public Task StartEffect(StartEffectRequest request, CallContext context = default)
    {
      effectService.StartEffect(request.TypeName, request.ColorHex);
      return Task.CompletedTask;
    }

    public Task StartGroupEffect(StartEffectRequest request, CallContext context = default)
    {
      effectService.StartEffect(request.TypeName, request.ColorHex, request.GroupName, Enum.Parse<IteratorEffectMode>(request.IteratorMode!), Enum.Parse<IteratorEffectMode>(request.SecondaryIteratorMode!));
      return Task.CompletedTask;
    }

    public async Task IncreaseBPM(IntRequest req, CallContext context = default)
    {
      await streamingSetup.IncreaseBPM(req.Value);
      await GetStatus();
    }

    public async Task SetBPM(IntRequest req, CallContext context = default)
    {
      await streamingSetup.SetBPM(req.Value);
      await GetStatus();
    }

    public Task SetBri(DoubleRequest req, CallContext context = default)
    {
      var filterValue = 100 - req.Value;

      return streamingSetup.SetBrightnessFilter(filterValue);
    }


    public Task StartRandom(CallContext context = default)
    {
      effectService.StartRandomEffect();
      return Task.CompletedTask;
    }

    public Task StartAutoMode(CallContext context = default)
    {
      effectService.StartAutoMode();
      return GetStatus();
    }

    public Task StopAutoMode(CallContext context = default)
    {
      effectService.StopAutoMode();
      return Task.CompletedTask;
    }

    public Task SetAutoRandomMode(BoolRequest req, CallContext context = default)
    {
      EffectService.AutoModeHasRandomEffects = req.Value;
      return Task.CompletedTask;
    }

    [Obsolete]
    public Task ToggleAutoRandomMode(CallContext context = default)
    {
      EffectService.AutoModeHasRandomEffects = !EffectService.AutoModeHasRandomEffects;
      return Task.CompletedTask;
    }

    public Task StopEffects(CallContext context = default)
    {
      effectService.StopAutoMode();
      effectService.StopEffects();
      return Task.CompletedTask;
    }

    //public void SetColors(string[,] matrix)
    //{
    //  ManualControlService.SetColors(matrix);
    //}
    //public void SetColorsList(List<List<string>> matrix)
    //{
    //  ManualControlService.SetColors(matrix);
    //}
    public Task Beat(CallContext context = default)
    {
      effectService.Beat();
      return Task.CompletedTask;
    }

    public Task Disconnect(CallContext context = default)
    {
      effectService.CancelAllEffects();

      return streamingSetup.DisconnectAsync();
    }


  }
}
