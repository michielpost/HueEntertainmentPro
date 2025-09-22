using HueApi.ColorConverters;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects
{
  [HueEffect(Name = "Random Pulse from center", Group = "Party", HasColorPicker = false)]
  public class RandomPulseEffect : IHueEffect
  {
    public Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      Func<TimeSpan> customWaitTime = () => waitTime() / 10;
      var center = EffectSettings.LocationCenter;

      var randomPulseEffect = new HueApi.Entertainment.Effects.Examples.RandomPulseEffect(fadeToZero: false, waitTime: customWaitTime);
      randomPulseEffect.X = center.X;
      randomPulseEffect.Y = center.Y;
      layer.PlaceEffect(randomPulseEffect);
      randomPulseEffect.Start();

      cancellationToken.Register(() =>
      {
        randomPulseEffect.Stop();
        layer.Effects.Remove(randomPulseEffect);
      });

      return Task.CompletedTask;
    }
  }
}
