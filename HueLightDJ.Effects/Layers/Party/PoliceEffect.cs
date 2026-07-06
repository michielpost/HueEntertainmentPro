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
  [HueEffect(Name = "Police", Group = "Party", HasColorPicker = false)]
  public class PoliceEffect : IHueEffect
  {
    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      var red = new RGBColor("FF0000");
      var blue = new RGBColor("0000FF");

      var left = layer.Where(x => x.LightLocation.X <= 0).ToList();
      var right = layer.Where(x => x.LightLocation.X > 0).ToList();

      while (!cancellationToken.IsCancellationRequested)
      {
        //Double flash left in red, then double flash right in blue
        for (int i = 0; i < 2; i++)
        {
          left.SetState(cancellationToken, red, 1, TimeSpan.Zero);
          await Task.Delay(waitTime() / 8);
          left.SetBrightness(cancellationToken, 0, TimeSpan.Zero);
          await Task.Delay(waitTime() / 8);
        }

        for (int i = 0; i < 2; i++)
        {
          right.SetState(cancellationToken, blue, 1, TimeSpan.Zero);
          await Task.Delay(waitTime() / 8);
          right.SetBrightness(cancellationToken, 0, TimeSpan.Zero);
          await Task.Delay(waitTime() / 8);
        }
      }
    }
  }
}
