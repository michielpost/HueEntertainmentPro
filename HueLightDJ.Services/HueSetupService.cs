using HueApi;
using HueApi.Models.Requests;
using HueLightDJ.Services.Interfaces;
using HueLightDJ.Services.Interfaces.Models;
using HueLightDJ.Services.Interfaces.Models.Requests;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueLightDJ.Services
{
  public class HueSetupService : IHueSetupService
  {
    public async Task<EntertainmentGroupResult> GetEntertainmentGroupsAsync(HueSetupRequest request, CallContext context = default)
    {
      try
      {
        var hueClient = new LocalHueApi(request.Ip, request.Key);
        var entConfigsResult = await hueClient.EntertainmentConfiguration.GetAllAsync();

        var groups = entConfigsResult.Data.Select(x => new SimpleEntertainmentGroup()
        {
          Id = x.Id,
          Name = x.Metadata?.Name,
          LightCount = x.Locations.ServiceLocations.Count()
        });

        return new EntertainmentGroupResult
        {
          Groups = groups
        };
      }
      catch (UnauthorizedAccessException)
      {
        return new EntertainmentGroupResult
        {
          ErrorMessage = "Unauthorized. Please check if your key is correct."
        };
      }
      catch (Exception)
      {
        return new EntertainmentGroupResult()
        {
          ErrorMessage = $"Could not connect to {request.Ip}"
        };
      }
    }

    public async Task IdentifyGroupsAsync(HueSetupRequest request, CallContext context = default)
    {
      if (!request.GroupId.HasValue)
        throw new ArgumentNullException("GroupId is null");

      var localHueClient = new LocalHueApi(request.Ip, request.Key);

      //Turn all lights in this entertainment group on
      var result = await localHueClient.EntertainmentConfiguration.GetByIdAsync(request.GroupId.Value);
      var entServices = result.Data.First().Locations.ServiceLocations.Select(x => x.Service?.Rid).ToList();

      var allEntResources = await localHueClient.Entertainment.GetAllAsync();

      var renderResources = allEntResources.Data.Where(x => entServices.Contains(x.Id)).Select(x => x.RendererReference).ToList();

      var lights = renderResources.Where(x => x?.Rtype == "light").ToList();

      var update = new UpdateLight
      {
        Identify = new Identify()
      };

      foreach (var light in lights)
      {
        if (light == null)
          continue;

        var updateResult = await localHueClient.Light.UpdateAsync(light.Rid, update);
        await Task.Delay(100); //prevent rate limitiing
      }

    }

    public async Task<IEnumerable<LocatedBridge>> LocateBridgesAsync(CallContext context = default)
    {
      var bridgeLocator = new HueApi.BridgeLocator.HttpBridgeLocator();
      IEnumerable<LocatedBridge> ips = new List<LocatedBridge>();

      try
      {
        var result = await bridgeLocator.LocateBridgesAsync(TimeSpan.FromSeconds(2));
        ips = result.Select(x => new LocatedBridge() { IpAddress = x.IpAddress, BridgeId = x.BridgeId, Port = x.Port });
      }
      catch { }

      return ips;
    }

    public async Task<Interfaces.Models.RegisterEntertainmentResult?> RegisterAsync(HueSetupRequest request, CallContext context = default)
    {
      request.Ip = request.Ip.Trim();

      if (request.Ip.Contains(":") || request.Ip.Contains("/"))
        throw new Exception($"Not a valid ip: {request.Ip}");

      try
      {

        var result = await LocalHueApi.RegisterAsync(request.Ip, "HueLightDJ", "Web", generateClientKey: true);
        if (result == null)
          return null;

        return new Interfaces.Models.RegisterEntertainmentResult()
        {
          Ip = request.Ip,
          StreamingClientKey = result.StreamingClientKey,
          Username = result.Username
        };
      }
      catch (Exception ex) when (ex.Message.Contains("link button not pressed", StringComparison.InvariantCultureIgnoreCase))
      {
        return new Interfaces.Models.RegisterEntertainmentResult()
        {
          Ip = request.Ip,
          ErrorMessage = "Link button not pressed. Please press the link button on the bridge and try again."
        };
      }
      catch (Exception)
      {
        return new Interfaces.Models.RegisterEntertainmentResult()
        {
          Ip = request.Ip,
          ErrorMessage = $"Could not connect to {request.Ip}"
        };
      }
    }
  }
}
