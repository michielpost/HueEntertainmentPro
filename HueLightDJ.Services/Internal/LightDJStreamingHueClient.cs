using HueApi.Entertainment;
using HueApi.Entertainment.Models;
using HueLightDJ.Services.Interfaces;
using HueLightDJ.Services.Interfaces.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HueLightDJ.Services
{
  public class LightDJStreamingHueClient : StreamingHueClient
  {
    private bool _demoMode;
    private readonly IHubService _hub;
    private string _bridgeIp;

    public Guid GroupId { get; set; }

    public LightDJStreamingHueClient(IHubService hub, string ip, string appKey, string clientKey, bool demoMode) : base(ip, appKey, clientKey)
    {
      this._hub = hub;
      _bridgeIp = ip;
      _demoMode = demoMode;

    }

    protected override void Send(IEnumerable<IEnumerable<StreamingChannel>> chunks)
    {
      if (!_demoMode)
        base.Send(chunks);

      var flatten = chunks.SelectMany(x => x);

      _hub.SendPreview(flatten.Select(x => new PreviewModel()
      {
        Bridge = _bridgeIp,
        Id = x.Id,
        X = x.ChannelLocation.X,
        Y = x.ChannelLocation.Y,
        Z = x.ChannelLocation.Z,
        Hex = x.State.RGBColor.ToHex(),
        Bri = x.State.Brightness
      }));
    }
  }
}
