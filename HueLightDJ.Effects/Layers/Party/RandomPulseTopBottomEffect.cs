using HueApi.ColorConverters;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects
{
  [HueEffect(Name = "Random Pulse top bottom", Group = "Party", HasColorPicker = false)]
  public class RandomPulseTopBottomEffect : IHueEffect
  {
    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      Func<TimeSpan> customWaitTime = () => waitTime() / 10;

      var bottomPulseEffect = new HueApi.Entertainment.Effects.Examples.RandomPulseEffect(waitTime: customWaitTime);
      bottomPulseEffect.AutoRepeat = false;
      bottomPulseEffect.Y = -1;
      layer.PlaceEffect(bottomPulseEffect);

      var topPulseEffect = new HueApi.Entertainment.Effects.Examples.RandomPulseEffect(waitTime: customWaitTime);
      topPulseEffect.AutoRepeat = false;
      topPulseEffect.Y = 1;
      layer.PlaceEffect(topPulseEffect);

      while (!cancellationToken.IsCancellationRequested)
      {
        bottomPulseEffect.Start();

        await Task.Delay(waitTime() * 2.2);

        topPulseEffect.Start();

        await Task.Delay(waitTime() * 2.2);
      }

      cancellationToken.Register(() =>
      {
        bottomPulseEffect.Stop();
        layer.RemoveEffect(bottomPulseEffect);

        topPulseEffect.Stop();
        layer.RemoveEffect(topPulseEffect);
      });

    }
  }
}
