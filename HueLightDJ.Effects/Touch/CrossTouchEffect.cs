using HueApi.ColorConverters;
using HueApi.Entertainment.Effects.Examples;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects.Touch
{
  [HueEffect(Name = "Cross Touch", IsBaseEffect = false, HasColorPicker = false)]
  class CrossTouchEffect : IHueTouchEffect
  {
    public Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken, double x, double y)
    {
      Func<TimeSpan> customWaitTime = () => waitTime() / 10;


      if (!color.HasValue)
        color = RGBColor.Random();

      HorizontalScanLineEffect hline = new HorizontalScanLineEffect(customWaitTime, color);
      hline.X = x;
      hline.AutoRepeat = false;
      layer.PlaceEffect(hline);

      VerticalScanLineEffect vline = new VerticalScanLineEffect(customWaitTime, color);
      vline.Y = y;
      vline.AutoRepeat = false;
      layer.PlaceEffect(vline);

      hline.Start();
      vline.Start();

      return Task.Run(async () =>
      {
        await Task.Delay(customWaitTime() * 15);
        hline.Stop();
        vline.Stop();
        await Task.Delay(TimeSpan.FromSeconds(1));
        layer.RemoveEffect(hline);
        layer.RemoveEffect(vline);
      });
    }
  }
}
