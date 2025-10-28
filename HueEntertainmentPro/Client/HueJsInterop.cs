using HueLightDJ.Services.Interfaces.Models;
using Microsoft.JSInterop;

namespace HueLightDJ.Blazor.Controls
{
  // This class provides an example of how JavaScript functionality can be wrapped
  // in a .NET class for easy consumption. The associated JavaScript module is
  // loaded on demand when first needed.
  //
  // This class can be registered as scoped DI service and then injected into Blazor
  // components for use.

  public class HueJsInterop : IAsyncDisposable
  {
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public HueJsInterop(IJSRuntime jsRuntime)
    {
      moduleTask = new(() =>
      {
        return jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./hueJsInterop.js").AsTask();
      });
    }

    public async ValueTask<string> Prompt(string message)
    {
      var module = await moduleTask.Value;
      return await module.InvokeAsync<string>("showPrompt", message);
    }

    public async ValueTask InitializePreview()
    {
      var module = await moduleTask.Value;
      await module.InvokeVoidAsync("initPreview");
    }

    public async ValueTask DisposeAsync()
    {
      if (moduleTask.IsValueCreated)
      {
        var module = await moduleTask.Value;
        await module.DisposeAsync();
      }
    }

    public async ValueTask ShowPreview(IEnumerable<PreviewModel> list)
    {
      var module = await moduleTask.Value;
      await module.InvokeVoidAsync("showLights", list);
    }
  }
}
