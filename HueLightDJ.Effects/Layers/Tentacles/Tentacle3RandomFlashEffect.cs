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
  [HueEffect(Name = "Tentacles (3) Random Flash", Group = "Tentacles", HasColorPicker = false)]
  public class Tentacles3RandomFlashEffect : IHueEffect
  {
    public async Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      var center = EffectSettings.LocationCenter;

      var tentacles = layer.GroupBy(x => (int)((x.LightLocation.Angle(center.X, center.Y) / 3.6 / 3))).OrderBy(x => x.Key);

      var grouped = tentacles.ChunkByGroupNumber(3).Select(x => x.SelectMany(l => l));

      while (!cancellationToken.IsCancellationRequested)
      {
        CancellationTokenSource tempCancel = new CancellationTokenSource();

        foreach (var group in grouped)
        {
          var nextColor = RGBColor.Random();
          group.To2DGroup().FlashQuick(cancellationToken, null, IteratorEffectMode.All, IteratorEffectMode.All, waitTime, transitionTimeOff: waitTime, duration: waitTime());
          await Task.Delay(waitTime(), cancellationToken);
        }

        tempCancel.Cancel();
      }

    }
  }
}
