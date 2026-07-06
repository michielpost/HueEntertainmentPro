using HueApi.ColorConverters;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects.Layers
{
  [HueEffect(Order = 6, Name = "Scanner Beam", Group = "Rotating", DefaultColor = "#FF0000")]
  public class ScannerEffect : IHueEffect
  {
    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      if (!color.HasValue)
        color = new RGBColor("FF0000");

      const int steps = 30;
      const double beamWidth = 0.5;

      int step = 0;
      int direction = 1;

      while (!cancellationToken.IsCancellationRequested)
      {
        //Beam sweeps from left (-1) to right (1) and bounces back
        var beamPosition = -1 + ((2.0 * step) / steps);
        var stepTime = waitTime() * 2 / steps;

        foreach (var light in layer)
        {
          var distance = Math.Abs(light.LightLocation.X - beamPosition);
          var brightness = Math.Max(0, 1 - (distance / beamWidth));
          light.SetState(cancellationToken, color, brightness, stepTime);
        }

        step += direction;
        if (step >= steps || step <= 0)
          direction = -direction;

        await Task.Delay(stepTime);
      }
    }
  }
}
