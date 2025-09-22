using HueApi.ColorConverters;
using HueApi.ColorConverters.HSB;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects.Layers
{
  [HueEffect(Order = 3, Name = "Rainbow Bottom Top Effect", Group = "Party", HasColorPicker = false)]
  public class RainbowBottomTopEffect : IHueEffect
  {

    protected virtual int Steps => (int)(HSB.HueMaxValue * 0.85);
    protected virtual int StartStep { get; set; } = 0;
    protected virtual bool DipToBlack { get; set; } = true;

    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      var r = new Random();
      var center = EffectSettings.LocationCenter;
      var orderedLayer = layer.OrderByDescending(x => x.LightLocation.Angle(center.X, center.Y));

      while (!cancellationToken.IsCancellationRequested)
      {

        foreach (var light in orderedLayer)
        {
          Task.Run(async () =>
          {
            var distance = 1 + light.LightLocation.Y;
            var timeSpan = waitTime() / 2 * distance;
            var addHue = (int)(Steps / 2 * distance);
            await Task.Delay(timeSpan);
            //Debug.WriteLine($"{light.Id} Angle {angle} and timespan {timeSpan.TotalMilliseconds}");
            var hsbColor = new HSB(StartStep + addHue, 255, 255);
            light.SetState(cancellationToken, hsbColor.GetRGB(), 1, waitTime() / 2);

            if (DipToBlack)
            {
              await Task.Delay(waitTime() * 0.8);
              light.SetBrightness(cancellationToken, 0, waitTime() * 0.1);
            }

          });

        }

        StartStep += Steps;
        await Task.Delay(waitTime() * 1.1);
      }

    }
  }
}
