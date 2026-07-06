using HueApi.ColorConverters;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects.Touch
{
  [HueEffect(Name = "Ripple Touch", IsBaseEffect = false, HasColorPicker = false)]
  class RippleTouchEffect : IHueTouchEffect
  {
    public Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken, double x, double y)
    {
      Func<TimeSpan> customWaitTime = () => waitTime() / 10;

      HueApi.Entertainment.Effects.Examples.RandomPulseEffect effect = new HueApi.Entertainment.Effects.Examples.RandomPulseEffect(false, customWaitTime);
      effect.X = x;
      effect.Y = y;
      effect.AutoRepeat = false;
      layer.PlaceEffect(effect);
      effect.Start();

      return Task.Run(async () =>
      {
        await Task.Delay(waitTime() * 2);
        effect.Stop();
        await Task.Delay(TimeSpan.FromSeconds(1));
        layer.RemoveEffect(effect);
      });
    }
  }
}
