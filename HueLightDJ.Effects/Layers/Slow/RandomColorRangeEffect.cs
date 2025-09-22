using HueApi.ColorConverters;
using HueApi.ColorConverters.HSB;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects
{
  [HueEffect(Name = "Random color range", Group = "Slow", HasColorPicker = true)]
  public class RandomColorRangeEffect : IHueEffect
  {
    public bool UseTransition { get; set; } = true;

    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      Random r = new Random();
      while (!cancellationToken.IsCancellationRequested)
      {
        var nextcolor = color ?? RGBColor.Random();
        var hsb = nextcolor.GetHSB();

        foreach (var light in layer)
        {
          var addHue = r.Next(-6000, 6000);
          var addBri = r.Next(-100, 100);
          var randomHsb = new HSB(hsb.Hue + addHue, hsb.Saturation, WrapValue(255, hsb.Brightness + addBri));
          light.SetState(cancellationToken, randomHsb.GetRGB(), 1, UseTransition ? waitTime() / 2 : TimeSpan.Zero);
        }
        await Task.Delay(waitTime());
      }
    }

    private int WrapValue(int max, int value)
    {
      var result = ((value % max) + max) % max;

      //At least 50, to avoid dark/off lights
      if (result < 50)
        result += 50;

      return result;
    }
  }
}
