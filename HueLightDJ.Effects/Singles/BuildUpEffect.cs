using HueApi.ColorConverters;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects
{
  [HueEffect(Name = "Build-up & Drop", IsBaseEffect = false, DefaultColor = "#FFFFFF")]
  public class BuildUpEffect : IHueEffect
  {
    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      //Non repeating effects should not run on baselayer
      if (layer.IsBaseLayer)
        return;

      if (!color.HasValue)
        color = new RGBColor("FFFFFF");

      const int buildUpBeats = 8;

      //Flashes accelerate and get brighter over the build-up
      var elapsed = TimeSpan.Zero;
      double progress = 0;

      while (progress < 1 && !cancellationToken.IsCancellationRequested)
      {
        var interval = TimeSpan.FromMilliseconds(Math.Max(50, 400 * (1 - progress)));

        layer.SetState(cancellationToken, color, 0.3 + (0.7 * progress), TimeSpan.Zero);
        await Task.Delay(interval);
        layer.SetBrightness(cancellationToken, 0, TimeSpan.Zero);
        await Task.Delay(interval);

        elapsed += interval + interval;
        progress = elapsed.TotalMilliseconds / (waitTime() * buildUpBeats).TotalMilliseconds;
      }

      //The drop: short blackout, then full blast fading out
      layer.SetBrightness(cancellationToken, 0, TimeSpan.Zero);
      await Task.Delay(waitTime() / 2);
      layer.SetState(cancellationToken, color, 1, TimeSpan.Zero);
      await Task.Delay(waitTime());
      layer.SetBrightness(cancellationToken, 0, waitTime());
    }
  }
}
