using HueApi.ColorConverters;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects
{
  [HueEffect(Name = "Single random pulse from center", IsBaseEffect = false, HasColorPicker = false)]
  public class SinglePulseEffect : IHueEffect
  {
    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      //Non repeating effects should not run on baselayer
      if (layer.IsBaseLayer)
        return;

      Func<TimeSpan> customWaitTime = () => waitTime() / 10;

      var center = EffectSettings.LocationCenter;
      var randomPulseEffect = new HueApi.Entertainment.Effects.Examples.RandomPulseEffect(fadeToZero: false, waitTime: customWaitTime);
      randomPulseEffect.X = center.X;
      randomPulseEffect.Y = center.Y;
      cancellationToken.Register(() =>
      {
        try
        {
          randomPulseEffect.Stop();
        }
        catch { }
        layer.RemoveEffect(randomPulseEffect);
      });

      layer.PlaceEffect(randomPulseEffect);
      randomPulseEffect.AutoRepeat = false;
      randomPulseEffect.Start();

      await Task.Delay(waitTime() * 2, cancellationToken);
      randomPulseEffect.Stop();
    }
  }
}
