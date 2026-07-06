using HueApi.ColorConverters;
using HueApi.Entertainment.Effects.BasEffects;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueLightDJ.Effects.Layers
{
  [HueEffect(Name = "Rotating Effect", Group = "Rotating", HasColorPicker = false)]
  public class RotatingEffect : IHueEffect
  {
    public Task Start(EntertainmentLayer layer, Func<TimeSpan> waitTime, RGBColor? color, CancellationToken cancellationToken)
    {
      Func<TimeSpan> customWaitTime = () => waitTime() / 10;

      if (!color.HasValue)
        color = RGBColor.Random();

      var center = EffectSettings.LocationCenter;
      var rotatingEffect = new RotatingLineEffect(cancellationToken, color.Value, customWaitTime);
      rotatingEffect.X = center.X;
      rotatingEffect.Y = center.Y; layer.PlaceEffect(rotatingEffect);
      rotatingEffect.Start();

      cancellationToken.Register(() =>
      {
        rotatingEffect.Stop();
        layer.RemoveEffect(rotatingEffect);
      });

      return Task.CompletedTask;
    }
  }

  public class RotatingLineEffect : AngleEffect
  {
    private CancellationToken _ct;
    private RGBColor _color;
    private Func<TimeSpan> _waitTime;

    public RotatingLineEffect(CancellationToken ct, RGBColor color, Func<TimeSpan> waitTime)
    {
      Width = 1;
      X = 0;
      Y = 0;

      _ct = ct;
      _color = color;
      _waitTime = waitTime;
    }

    public override void Start()
    {
      base.Start();

      CurrentAngle = 90;

      var state = new HueApi.Entertainment.Models.StreamingState();
      state.SetBrightness(1);
      state.SetRGBColor(_color);

      this.State = state;

      Rotate(_ct, waitTime: _waitTime);
    }
  }
}
