using HueApi.ColorConverters;
using HueApi.Entertainment.Effects;
using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects.Group
{
  [HueEffect(Name = "Quick Flash", DefaultColor = "#FFFFFF")]
  public class QuickFlashEffect : IHueGroupEffect
  {
    public Task Start(IEnumerable<IEnumerable<EntertainmentLight>> layer, Func<TimeSpan> waitTime, RGBColor? color, IteratorEffectMode iteratorMode, IteratorEffectMode secondaryIteratorMode, CancellationToken cancellationToken)
    {
      if (!color.HasValue)
        color = RGBColor.Random();

      if (iteratorMode != IteratorEffectMode.All)
      {
        if (secondaryIteratorMode == IteratorEffectMode.Bounce
          || secondaryIteratorMode == IteratorEffectMode.Cycle
          || secondaryIteratorMode == IteratorEffectMode.Random
          || secondaryIteratorMode == IteratorEffectMode.RandomOrdered
          || secondaryIteratorMode == IteratorEffectMode.Single)
        {
          Func<TimeSpan> customWaitMS = () => TimeSpan.FromMilliseconds((waitTime().TotalMilliseconds * layer.Count()) / layer.SelectMany(x => x).Count());

          return layer.FlashQuick(cancellationToken, color, iteratorMode, secondaryIteratorMode, waitTime: customWaitMS);
        }
      }

      return layer.FlashQuick(cancellationToken, color, iteratorMode, secondaryIteratorMode, waitTime: waitTime);
    }
  }
}
