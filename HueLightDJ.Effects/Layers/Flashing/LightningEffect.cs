using HueApi.ColorConverters;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects
{
  [HueEffect(Order = 8, Name = "Lightning", Group = "Flash", HasColorPicker = false)]
  public class LightningEffect : IHueEffect
  {
    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      var r = new Random();
      var white = new RGBColor("FFFFFF");

      layer.SetBrightness(cancellationToken, 0, TimeSpan.Zero);

      while (!cancellationToken.IsCancellationRequested)
      {
        //Strike hits a random cluster of lights, with a random number of quick flashes
        var strikeLights = layer.OrderBy(x => Guid.NewGuid()).Take(Math.Max(1, layer.Count / 3)).ToList();
        var flashes = r.Next(2, 5);

        for (int i = 0; i < flashes; i++)
        {
          strikeLights.SetState(cancellationToken, white, 1, TimeSpan.Zero);
          await Task.Delay(r.Next(30, 80));
          strikeLights.SetBrightness(cancellationToken, 0, TimeSpan.Zero);
          await Task.Delay(r.Next(40, 150));
        }

        await Task.Delay(waitTime() * (1 + (r.NextDouble() * 2)));
      }
    }
  }
}
