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
  [HueEffect(Order = 6, Name = "Quick Random Flash", Group = "Flash", DefaultColor = "#FFFFFF")]
  public class QuickFlashRandomEffect : IHueEffect
  {
    public Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      if (!color.HasValue)
        color = RGBColor.Random();

      var groupCount = Math.Max(1, layer.Count / 3);

      Func<TimeSpan> customWaitMS = () => TimeSpan.FromMilliseconds((waitTime().TotalMilliseconds * 2) / groupCount);

      return layer.OrderBy(x => Guid.NewGuid()).ChunkByGroupNumber(groupCount).FlashQuick(cancellationToken, color, IteratorEffectMode.Random, waitTime: customWaitMS);
    }
  }
}
