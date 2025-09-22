using HueApi.ColorConverters;
using HueApi.ColorConverters.HSB;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects.Layers
{
  [HueEffect(Order = 3, Name = "Color Wheel Effect", Group = "Rotating", HasColorPicker = false)]
  public class ColorWheelEffect : IHueEffect
  {

    protected virtual int AddRotation => 9;
    protected virtual int StartRotation { get; set; } = 0;

    public int Chunks { get; set; } = 3;
    private List<RGBColor> _colors = new List<RGBColor>();

    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      var r = new Random();
      var center = EffectSettings.LocationCenter;
      var orderedLayer = layer.OrderByDescending(x => x.LightLocation.Angle(center.X, center.Y));

      var hsb = r.Next(HSB.HueMaxValue);
      for (int i = 0; i < Chunks; i++)
      {
        var hsbColor = new HSB(hsb, 255, 255);
        _colors.Add(hsbColor.GetRGB());
        hsb += (HSB.HueMaxValue / Chunks);
      }

      while (!cancellationToken.IsCancellationRequested)
      {

        foreach (var light in orderedLayer)
        {
          var angle = light.LightLocation.Angle(center.X, center.Y).Move360(StartRotation);
          double normalAngle = WrapValue(360, (int)angle);

          int arrayIndex = (int)(normalAngle / 361 * Chunks);
          light.SetState(cancellationToken, _colors[arrayIndex], 1);
        }

        StartRotation += AddRotation;
        StartRotation = WrapValue(360, StartRotation);
        await Task.Delay(waitTime() / 6);
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
