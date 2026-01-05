using HueApi;
using HueEntertainmentPro.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace HueEntertainmentPro.Client.Services
{
  public class ResourceExplorerService(NavigationManager NavigationManager)
  {
    public LocalHueApi GetHueClient(Bridge bridge)
    {
      //Use new HttpClient because setting DangerousAcceptAnyServerCertificateValidator is not supported
      var localHueApi = new LocalHueApi(bridge.Ip, bridge.Username, new HttpClient());
      localHueApi.SetBaseAddress(new Uri($"{NavigationManager.BaseUri}hueproxy/{bridge.Ip}/"));

      // Console.WriteLine("Base URL: " + $"{NavigationManager.BaseUri}/hueproxy/{bridge.Ip}/");

      return localHueApi;
    }

    public LocalHueApi GetHueClient(string ip)
    {
      //Use new HttpClient because setting DangerousAcceptAnyServerCertificateValidator is not supported
      var localHueApi = new LocalHueApi(ip, null, new HttpClient());
      localHueApi.SetBaseAddress(new Uri($"{NavigationManager.BaseUri}hueproxy/{ip}/"));

      // Console.WriteLine("Base URL: " + $"{NavigationManager.BaseUri}/hueproxy/{bridge.Ip}/");

      return localHueApi;
    }

    public string CreateResourceLink(Guid id, string? rtype, Guid? rid = null)
    {
      if (rtype == null)
        return $"/resource-explorer/{id}";

      if (rid == null)
        return $"/resource-explorer/{id}/{rtype}";

      return $"/resource-explorer/{id}/{rtype}/{rid}";
    }
  }
}
