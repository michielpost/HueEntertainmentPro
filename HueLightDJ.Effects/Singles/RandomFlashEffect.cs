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
  [HueEffect(Name = "Flashing Stars", IsBaseEffect = false, HasColorPicker = true, DefaultColor = "#FFFFFF")]
  public class RandomFlashEffect : IHueEffect
  {
    public Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      //Non repeating effects should not run on baselayer
      if (layer.IsBaseLayer)
        return Task.CompletedTask;

      if (!color.HasValue)
        color = RGBColor.Random();

      var groupCount = Math.Max(1, layer.Count / 3);

      Func<TimeSpan> customWaitMS = () => TimeSpan.FromMilliseconds((waitTime().TotalMilliseconds) / groupCount);

      return layer.OrderBy(x => Guid.NewGuid()).ChunkByGroupNumber(groupCount).FlashQuick(cancellationToken, color, IteratorEffectMode.AllIndividual, IteratorEffectMode.RandomOrdered, waitTime: customWaitMS, duration: waitTime());
    }
  }
}
