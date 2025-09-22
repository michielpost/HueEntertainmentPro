using HueApi.ColorConverters;
using HueApi.Entertainment.Effects;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects
{
  [HueEffect(Name = "Fill from center", IsBaseEffect = false)]
  public class FillFromCenterEffect : IHueEffect
  {
    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      //Non repeating effects should not run on baselayer
      if (layer.IsBaseLayer)
        return;

      var center = EffectSettings.LocationCenter;
      var orderedByDistance = layer.OrderBy(x => x.LightLocation.Distance(center.X, center.Y, center.Z));

      if (!color.HasValue)
        color = RGBColor.Random();

      Func<TimeSpan> customWaitTime = () => waitTime() / layer.Count;


      await orderedByDistance.To2DGroup().SetColor(cancellationToken, color.Value, IteratorEffectMode.Single, IteratorEffectMode.All, customWaitTime);
      layer.SetBrightness(cancellationToken, 0, transitionTime: TimeSpan.FromMilliseconds(0));
    }
  }
}
