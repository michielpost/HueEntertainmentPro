using HueLightDJ.Services.Interfaces;
using HueLightDJ.Services.Interfaces.Models;

namespace HueEntertainmentPro.Client.Services
{
  public class HubService : IHubService
  {
    public event EventHandler<string?>? LogMsgEvent;
    public event EventHandler<IEnumerable<PreviewModel>>? PreviewEvent;
    public event EventHandler? StatusChangedEvent;

    public Task StatusChanged()
    {
      StatusChangedEvent?.Invoke(this, EventArgs.Empty);
      return Task.CompletedTask;
    }

    public Task SendAsync(string method, params object?[] arg1)
    {
      LogMsgEvent?.Invoke(this, method + " " + (string?)arg1.FirstOrDefault());

      Console.WriteLine(method);
      return Task.CompletedTask;
    }

    public Task SendPreview(IEnumerable<PreviewModel> list)
    {
      PreviewEvent?.Invoke(this, list);
      return Task.CompletedTask;

    }
  }
}
