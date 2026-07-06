using HueApi.ColorConverters;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects
{
  [HueEffect(Order = 7, Name = "Strobe", Group = "Flash", DefaultColor = "#FFFFFF")]
  public class StrobeEffect : IHueEffect
  {
    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      if (!color.HasValue)
        color = new RGBColor("FFFFFF");

      while (!cancellationToken.IsCancellationRequested)
      {
        layer.SetState(cancellationToken, color, 1, TimeSpan.Zero);
        await Task.Delay(TimeSpan.FromMilliseconds(40));
        layer.SetBrightness(cancellationToken, 0, TimeSpan.Zero);
        await Task.Delay(waitTime() / 4);
      }
    }
  }
}
