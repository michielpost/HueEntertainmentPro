using HueApi.ColorConverters;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects.Layers
{
  [HueEffect(Name = "Sparkle", Group = "Party", DefaultColor = "#0000FF")]
  public class SparkleEffect : IHueEffect
  {
    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      if (!color.HasValue)
        color = RGBColor.Random();

      var white = new RGBColor("FFFFFF");

      //Dim base color on all lights, with random lights sparkling white on top
      layer.SetState(cancellationToken, color, 0.2, TimeSpan.FromMilliseconds(200));

      while (!cancellationToken.IsCancellationRequested)
      {
        var sparkles = layer.OrderBy(x => Guid.NewGuid()).Take(Math.Max(1, layer.Count / 5)).ToList();

        sparkles.SetState(cancellationToken, white, 1, TimeSpan.Zero);
        await Task.Delay(waitTime() / 8);
        sparkles.SetState(cancellationToken, color, 0.2, waitTime() / 4);
        await Task.Delay(waitTime() / 8);
      }
    }
  }
}
