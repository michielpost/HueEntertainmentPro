using Microsoft.JSInterop;

namespace HueLightDJ.Blazor.Controls
{
  // Wraps the client-side audio beat detection module (Web Audio API).
  // Captures audio from a microphone or PC audio (screen/tab share) and
  // calls back into .NET when a beat is detected.
  public class AudioBeatJsInterop : IAsyncDisposable
  {
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public AudioBeatJsInterop(IJSRuntime jsRuntime)
    {
      moduleTask = new(() =>
      {
        return jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./js/audioBeatDetector.js").AsTask();
      });
    }

    public async ValueTask Start<T>(DotNetObjectReference<T> dotNetRef, string sourceType, string containerId) where T : class
    {
      var module = await moduleTask.Value;
      await module.InvokeVoidAsync("start", dotNetRef, sourceType, containerId);
    }

    public async ValueTask Stop()
    {
      var module = await moduleTask.Value;
      await module.InvokeVoidAsync("stop");
    }

    public async ValueTask SetThreshold(double multiplier)
    {
      var module = await moduleTask.Value;
      await module.InvokeVoidAsync("setThreshold", multiplier);
    }

    public async ValueTask DisposeAsync()
    {
      if (moduleTask.IsValueCreated)
      {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("stop");
        await module.DisposeAsync();
      }
    }
  }
}
