using HueApi.ColorConverters;
using HueApi.ColorConverters.HSB;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects.Layers
{
  [HueEffect(Name = "Fire", Group = "Slow", HasColorPicker = false)]
  public class FireEffect : IHueEffect
  {
    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      var r = new Random();

      //Warm hue range, from red towards yellow
      var maxHue = (int)(HSB.HueMaxValue / 8);

      while (!cancellationToken.IsCancellationRequested)
      {
        foreach (var light in layer)
        {
          var hsb = new HSB(r.Next(0, maxHue), 255, 255);
          var brightness = 0.4 + (r.NextDouble() * 0.6);
          light.SetState(cancellationToken, hsb.GetRGB(), brightness, waitTime() / 4);
        }

        await Task.Delay(waitTime() / 4);
      }
    }
  }
}
