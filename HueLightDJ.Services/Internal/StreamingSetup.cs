using HueApi;
using HueApi.BridgeLocator;
using HueApi.ColorConverters;
using HueApi.ColorConverters.Original.Extensions;
using HueApi.Entertainment.Models;
using HueApi.Extensions;
using HueApi.Models;
using HueApi.Models.Requests;
using HueLightDJ.Effects;
using HueLightDJ.Services.Interfaces;
using HueLightDJ.Services.Interfaces.Models;
using HueLightDJ.Services.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Services
{
  public class StreamingSetup
  {
    public static Guid demo1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static Guid demo2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static List<StreamingGroup> StreamingGroups { get; set; } = new List<StreamingGroup>();
    private static List<LightDJStreamingHueClient> StreamingHueClients { get; set; } = new List<LightDJStreamingHueClient>();
    public static List<EntertainmentLayer>? Layers { get; set; }
    private static int BPM { get; set; } = 120;
    public static Ref<TimeSpan> WaitTime { get; set; } = TimeSpan.FromMilliseconds(500);



    public static GroupConfiguration? CurrentConnection { get; set; }



    private static Guid _groupId;
    private static CancellationTokenSource _cts = new();
    private bool demoMode;
    private readonly IHubService hub;
    private readonly List<GroupConfiguration> fullConfig;

    public StreamingSetup(IHubService hub, IOptions<List<GroupConfiguration>> fullConfig)
    {
      this.hub = hub;
      this.fullConfig = fullConfig.Value;
    }

    [Obsolete]
    public async Task<List<MultiBridgeHuePosition>> GetLocationsAsync(string groupName)
    {
      var configSection = await GetGroupConfigurationsAsync();
      var currentGroup = configSection.Where(x => x.Name == groupName).FirstOrDefault();

      if (currentGroup == null)
        return new List<MultiBridgeHuePosition>();

      return await GetLocationsAsync(currentGroup);
    }

    public async Task<List<MultiBridgeHuePosition>> GetLocationsAsync(GroupConfiguration currentGroup)
    {
      var locations = new List<MultiBridgeHuePosition>();

      if (currentGroup == null)
        return locations;

      foreach (var bridgeConfig in currentGroup.Connections)
      {
        if (!bridgeConfig.GroupId.HasValue)
          continue;

        var localClient = new LocalHueApi(bridgeConfig.Ip, bridgeConfig.Key);
        var group = await localClient.EntertainmentConfiguration.GetByIdAsync(bridgeConfig.GroupId.Value);

        var serviceLocations = group.Data.First().Locations.ServiceLocations;

        foreach (var serviceLocation in serviceLocations)
        {
          for (int i = 0; i < serviceLocation.Positions.Count; i++)
          {
            locations.Add(new MultiBridgeHuePosition()
            {
              Bridge = bridgeConfig.Ip,
              GroupId = bridgeConfig.GroupId.Value,
              Id = serviceLocation.Service!.Rid,
              PositionIndex = i,
              X = serviceLocation.Positions[i].X,
              Y = serviceLocation.Positions[i].Y
            });
          }
        }


      }

      return locations;
    }

    public async Task SetLocations(List<MultiBridgeHuePosition> locations)
    {
      var configSection = await GetGroupConfigurationsAsync();

      var grouped = locations.GroupBy(x => x.Bridge);

      foreach (var group in grouped)
      {
        var ip = group.Key;
        var groupId = group.First().GroupId;
        var config = configSection.SelectMany(x => x.Connections).Where(x => x.Ip == ip && x.GroupId == groupId).FirstOrDefault();
        if (config != null)
        {
          var serviceLocations = group.GroupBy(x => x.Id);

          var client = new LocalHueApi(config.Ip, config.Key);
          UpdateEntertainmentConfiguration updateReq = new UpdateEntertainmentConfiguration();
          updateReq.Locations = new Locations();

          foreach (var location in serviceLocations)
          {
            var serviceLoc = new HueServiceLocation
            {
              Service = new ResourceIdentifier() { Rid = location.Key, Rtype = "entertainment" }
            };

            foreach (var pos in location)
            {
              serviceLoc.Positions.Add(new HuePosition(pos.X, pos.Y, 0));
            }

            updateReq.Locations.ServiceLocations.Add(serviceLoc);
          }

          await client.EntertainmentConfiguration.UpdateAsync(groupId, updateReq);
        }
      }
    }

    public async Task AlertLight(MultiBridgeHuePosition light)
    {
      var configSection = await GetGroupConfigurationsAsync();

      var config = configSection.Where(x => x.Connections.Any(c => c.Ip == light.Bridge)).FirstOrDefault();
      if (config != null)
      {
        foreach (var conn in config.Connections)
        {
          if (!conn.GroupId.HasValue)
            continue;

          var client = new LocalHueApi(conn.Ip, conn.Key);
          var allCommand = new UpdateLight().TurnOn().SetColor(new RGBColor("0000FF")); //All blue

          var result = await client.EntertainmentConfiguration.GetByIdAsync(conn.GroupId.Value);

          var entServices = result.Data.First().Locations.ServiceLocations.Select(x => x.Service?.Rid).ToList();
          var allResources = await client.Resource.GetAllAsync();

          var devices = allResources.Data.Where(x => entServices.Contains(x.Id)).Select(x => x.Owner?.Rid).ToList();
          var lights = allResources.Data.Where(x => devices.Contains(x.Id)).Select(x => x.Services?.Where(x => x.Rtype == "light").FirstOrDefault()?.Rid).ToList();

          foreach (var singleLightId in lights.Where(x => x.HasValue))
          {
            await client.Light.UpdateAsync(singleLightId!.Value, allCommand);
          }


          //Only selected light red
          if (conn.Ip == light.Bridge)
          {
            var alertCommand = new UpdateLight().TurnOn().SetColor(new RGBColor("FF0000")); ;
            alertCommand.Alert = new UpdateAlert();

            var device = allResources.Data.Where(x => x.Id == light.Id).Select(x => x.Owner?.Rid).FirstOrDefault();
            var lightDeviceId = allResources.Data.Where(x => x.Id == device).Select(x => x.Services?.Where(x => x.Rtype == "light").FirstOrDefault()?.Rid).FirstOrDefault();

            if (lightDeviceId.HasValue)
              await client.Light.UpdateAsync(lightDeviceId.Value, alertCommand);
          }
        }
      }
    }

    [Obsolete]
    public async Task SetupAndReturnGroupAsync(string groupName)
    {
      var configSection = await GetGroupConfigurationsAsync();
      var currentGroup = configSection.Where(x => x.Name == groupName).FirstOrDefault();
      if (currentGroup == null)
        throw new ArgumentNullException($"Group not found ({groupName})", nameof(groupName));

      await SetupAndReturnGroupAsync(currentGroup);
    }

    public async Task SetupAndReturnGroupAsync(GroupConfiguration currentGroup)
    {
      demoMode = currentGroup.Id == Guid.Empty
        || currentGroup.Id == demo1Id
        || currentGroup.Id == demo2Id
        || currentGroup.Name == "DEMO" || currentGroup.Connections.First().Key == "DEMO";
      bool useSimulator = demoMode ? true : currentGroup.Connections.First().UseSimulator;

      //Disconnect any current connections

      await this.DisconnectAsync();
      _cts = new CancellationTokenSource();

      List<Task> connectTasks = new List<Task>();
      foreach (var bridgeConfig in currentGroup.Connections)
      {
        connectTasks.Add(Connect(demoMode, useSimulator, bridgeConfig));

      }
      //Connect in parallel and wait for all tasks to finish
      await Task.WhenAll(connectTasks);

      var baseLayer = GetNewLayer(isBaseLayer: true);
      var effectLayer = GetNewLayer(isBaseLayer: false);

      Layers = new List<EntertainmentLayer>() { baseLayer, effectLayer };
      CurrentConnection = currentGroup;
      HuePosition? locationCenter = new HuePosition(0, 0, 0);
      if (currentGroup.LocationCenter != null)
        locationCenter = new HuePosition(currentGroup.LocationCenter.X, currentGroup.LocationCenter.Y, currentGroup.LocationCenter.Z);

      EffectSettings.LocationCenter = locationCenter ?? new HuePosition(0, 0, 0);

      //Optional: calculated effects that are placed on this layer
      baseLayer.AutoCalculateEffectUpdate(_cts.Token);
      effectLayer.AutoCalculateEffectUpdate(_cts.Token);

      await hub.StatusChanged();
    }

    private async Task Connect(bool demoMode, bool useSimulator, ConnectionConfiguration bridgeConfig)
    {
      await hub.SendAsync("StatusMsg", $"Connecting to bridge {bridgeConfig.Ip}");
      if (!bridgeConfig.GroupId.HasValue)
      {
        await hub.SendAsync("StatusMsg", $"No group id specified. Abort connection.");
        return;
      }

      try
      {
        //Initialize streaming client
        var client = new LightDJStreamingHueClient(hub, bridgeConfig.Ip, bridgeConfig.Key, bridgeConfig.EntertainmentKey, demoMode);

        //Get the entertainment group
        Dictionary<int, HuePosition>? locations = new Dictionary<int, HuePosition>();
        if (demoMode)
        {
          //var dirPath = Assembly.GetExecutingAssembly().Location;
          //dirPath = Path.GetDirectoryName(dirPath) ?? string.Empty;
          //var filePath = Path.GetFullPath(Path.Combine(dirPath, $"{bridgeConfig.Ip}_{bridgeConfig.GroupId}.json"));

          //string demoJson = await File.ReadAllTextAsync(filePath);
          //locations = JsonSerializer.Deserialize<Dictionary<int, HuePosition>>(demoJson);

          var assembly = Assembly.GetExecutingAssembly();
          var resourceName = "HueLightDJ.Services." + $"{bridgeConfig.Ip}_{bridgeConfig.GroupId}.json";

          using (Stream? streamFile = assembly.GetManifestResourceStream(resourceName))
          using (StreamReader reader = new StreamReader(streamFile!))
          {
            string demoJson = reader.ReadToEnd(); //Make string equal to full file
            locations = JsonSerializer.Deserialize<Dictionary<int, HuePosition>>(demoJson);
          }
          _groupId = bridgeConfig.GroupId.Value;
        }
        else
        {
          var all = await client.LocalHueApi.EntertainmentConfiguration.GetAllAsync();
          var group = all.Data.Where(x => x.Id == bridgeConfig.GroupId).FirstOrDefault();

          if (group == null)
            throw new Exception($"No Entertainment Group found with id {bridgeConfig.GroupId}. Create one using the Philips Hue App or the HueApi.UniversalWindows.Sample");
          else
          {
            await hub.SendAsync("StatusMsg", $"Using Entertainment Group {group.Id} for bridge {bridgeConfig.Ip}");
            Console.WriteLine($"Using Entertainment Group {group.Id}");
            _groupId = group.Id;
          }

          locations = group.Channels.ToDictionary(x => x.ChannelId, x => x.Position);
        }

        if (locations == null)
          throw new Exception("No locations found.");

        //new locations
        Dictionary<int, Tuple<HuePosition, List<Guid>>> newLocations = new Dictionary<int, Tuple<HuePosition, List<Guid>>>();
        foreach (var location in locations)
        {
          newLocations.Add(location.Key, new Tuple<HuePosition, List<Guid>>(location.Value, new List<Guid>()));
        }
        //Create a streaming group
        var stream = new StreamingGroup(newLocations);
        stream.IsForSimulator = useSimulator;

        if (!demoMode)
        {
          //Stop streaming to the group, to stop all previous streams
          await client.LocalHueApi.SetStreamingAsync(_groupId, active: false);

          //Connect to the streaming group
          await client.ConnectAsync(_groupId, simulator: useSimulator);
        }

        //Start auto updating this entertainment group
        client.AutoUpdateAsync(stream, _cts.Token, 50, onlySendDirtyStates: false);

        StreamingHueClients.Add(client);
        StreamingGroups.Add(stream);

        await hub.SendAsync("StatusMsg", $"Succesfully connected to bridge {bridgeConfig.Ip}");

      }
      catch (Exception ex)
      {
        await hub.SendAsync("StatusMsg", $"Failed to connect to bridge {bridgeConfig.Ip}, exception: " + ex);

        throw;
      }

    }

    [Obsolete]
    public async Task<List<GroupConfiguration>> GetGroupConfigurationsAsync()
    {
      IEnumerable<HueApi.BridgeLocator.LocatedBridge> bridges = new List<HueApi.BridgeLocator.LocatedBridge>();
      try
      {
        IBridgeLocator bridgeLocator = new HttpBridgeLocator();
        bridges = await bridgeLocator.LocateBridgesAsync(TimeSpan.FromSeconds(2));
      }
      catch { }

      if (bridges == null || !bridges.Any())
        return fullConfig ?? new();
      else
      {
        return fullConfig.Where(x =>
        x.Connections.Select(c => c.Ip).Intersect(bridges.Select(b => b.IpAddress)).Any()
        || x.IsAlwaysVisible
        ).ToList();
      }
    }

    public async Task SetBrightnessFilter(double value)
    {
      if (value > 1)
        value = value / 100;

      if (value > 1)
        return;

      foreach (var stream in StreamingGroups)
      {
        stream.BrightnessFilter = value;
      }

      await hub.SendAsync("StatusMsg", $"Brightness set to: {(1 - value) * 100}");


      await hub.StatusChanged();
    }

    private static EntertainmentLayer GetNewLayer(bool isBaseLayer = false)
    {
      var layer = new EntertainmentLayer(isBaseLayer);
      foreach (var stream in StreamingGroups)
      {
        var all = stream.GetNewLayer(isBaseLayer);
        layer.AddRange(all);
      }
      return layer;
    }

    public async Task DisconnectAsync()
    {
      if (_cts != null)
        _cts.Cancel();

      if (!demoMode)
      {
        foreach (var client in StreamingHueClients)
        {
          try
          {
            await client.LocalHueApi.SetStreamingAsync(_groupId, active: false);
            client.Close();
          }
          catch { }
        }
      }

      Layers = null;
      StreamingHueClients.Clear();
      StreamingGroups.Clear();
      CurrentConnection = null;

      await hub.SendAsync("StatusMsg", "Disconnected");
      await hub.StatusChanged();
    }

    public static EntertainmentLayer GetFirstLayer()
    {
      if (Layers == null || !Layers.Any())
        throw new Exception("No layers found.");

      return Layers.First();
    }

    public async static Task<bool> IsStreamingActive()
    {
      //Optional: Check if streaming is currently active
      var entServices = await StreamingHueClients.First().LocalHueApi.Entertainment.GetAllAsync();
      if (!entServices.Data.Any())
        return false;

      var numSupported = entServices.Data.Sum(x => x.MaxStreams);

      var entConfigs = await StreamingHueClients.First().LocalHueApi.EntertainmentConfiguration.GetAllAsync();
      if (!entConfigs.Data.Any())
        return false;

      var active = entConfigs.Data.Where(x => x.Status == EntertainmentConfigurationStatus.active).Count();

      var streamingChannelsLeft = numSupported - active;

      Console.WriteLine($"{streamingChannelsLeft} our of {numSupported} streaming channels left");

      return streamingChannelsLeft <= 0;

    }

    public static int GetBPM()
    {
      return BPM;
    }

    public async Task<int> SetBPM(int bpm)
    {
      BPM = bpm;
      WaitTime.Value = TimeSpan.FromMilliseconds((60 * 1000) / bpm);

      await hub.SendAsync("StatusMsg", $"BPM set to: {bpm}");

      await hub.StatusChanged();
      return GetBPM();
    }

    public async Task<int> IncreaseBPM(int value)
    {
      var result = await SetBPM(BPM + value);
      return result;
    }
  }
}
