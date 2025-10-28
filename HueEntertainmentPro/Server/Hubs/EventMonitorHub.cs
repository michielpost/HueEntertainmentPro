using HueApi;
using HueApi.Models.Responses;
using HueEntertainmentPro.Server.Services;
using HueEntertainmentPro.Services;
using HueEntertainmentPro.Shared.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace HueEntertainmentPro.Server.Hubs
{
  public class EventMonitorHub : Hub<IEventMonitorClient>
  {
    private static readonly ConcurrentDictionary<string, HashSet<Guid>> _subscribers = new ConcurrentDictionary<string, HashSet<Guid>>();
    private static readonly ConcurrentDictionary<Guid, LocalHueApi> _bridgeConnections = new ConcurrentDictionary<Guid, LocalHueApi>();
    private readonly BridgeService _bridgeDatabase;

    public EventMonitorHub(BridgeService bridgeDatabase)
    {
      _bridgeDatabase = bridgeDatabase;
    }

    public async Task Subscribe(Guid bridgeId)
    {
      // Get or create the set of bridgeIds for this connection
      var connectionBridges = _subscribers.GetOrAdd(Context.ConnectionId, _ => new HashSet<Guid>());

      if (connectionBridges.Add(bridgeId))
      {
        await Groups.AddToGroupAsync(Context.ConnectionId, bridgeId.ToString());

        if (!_bridgeConnections.ContainsKey(bridgeId))
        {
          var bridgeInfo = await _bridgeDatabase.GetBridge(bridgeId);
          if (bridgeInfo == null)
          {
            await Clients.Caller.Error($"No bridge found for ID: {bridgeId}");
            connectionBridges.Remove(bridgeId);
            return;
          }

          var localHueClient = new LocalHueApi(bridgeInfo.Ip, bridgeInfo.Username);
          localHueClient.OnEventStreamMessage += (bridgeIp, events) => EventStreamMessage(bridgeId, bridgeIp, events);

          try
          {
            localHueClient.StartEventStream();
            _bridgeConnections.TryAdd(bridgeId, localHueClient);
          }
          catch (Exception ex)
          {
            await Clients.Caller.Error($"Failed to connect to bridge {bridgeId}: {ex.Message}");
            connectionBridges.Remove(bridgeId);
            return;
          }
        }

        await Clients.Caller.Subscribed(bridgeId);
      }
    }

    public async Task Unsubscribe(Guid bridgeId)
    {
      if (_subscribers.TryGetValue(Context.ConnectionId, out var connectionBridges) && connectionBridges.Remove(bridgeId))
      {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, bridgeId.ToString());

        if (!_subscribers.Values.Any(set => set.Contains(bridgeId)))
        {
          if (_bridgeConnections.TryRemove(bridgeId, out var localHueClient))
          {
            try
            {
              localHueClient.StopEventStream();
              localHueClient.OnEventStreamMessage -= (bridgeIp, events) => EventStreamMessage(bridgeId, bridgeIp, events);
            }
            catch (Exception ex)
            {
              Console.WriteLine($"Error stopping event stream for bridge {bridgeId}: {ex.Message}");
            }
          }
        }

        await Clients.Caller.Unsubscribed(bridgeId);

        // Clean up if no bridges remain for this connection
        if (!connectionBridges.Any())
        {
          _subscribers.TryRemove(Context.ConnectionId, out _);
        }
      }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      if (_subscribers.TryRemove(Context.ConnectionId, out var connectionBridges))
      {
        foreach (var bridgeId in connectionBridges)
        {
          await Groups.RemoveFromGroupAsync(Context.ConnectionId, bridgeId.ToString());

          if (!_subscribers.Values.Any(set => set.Contains(bridgeId)))
          {
            if (_bridgeConnections.TryRemove(bridgeId, out var localHueClient))
            {
              try
              {
                localHueClient.StopEventStream();
                localHueClient.OnEventStreamMessage -= (bridgeIp, events) => EventStreamMessage(bridgeId, bridgeIp, events);
              }
              catch (Exception ex)
              {
                Console.WriteLine($"Error stopping event stream for bridge {bridgeId}: {ex.Message}");
              }
            }
          }
        }
      }
      await base.OnDisconnectedAsync(exception);
    }

    private async void EventStreamMessage(Guid bridgeId, string bridgeIp, List<EventStreamResponse> events)
    {
      Console.WriteLine($"{DateTimeOffset.UtcNow} | {events.Count} new events");

      foreach (var hueEvent in events)
      {
        foreach (var data in hueEvent.Data)
        {
          Console.WriteLine($"Bridge IP: {bridgeIp} | Data: {data.Metadata?.Name} / {data.IdV1}");
          foreach (var jsonData in data.ExtensionData)
          {
            Console.WriteLine(jsonData);
          }
          Console.WriteLine();

          var eventData = new EventData
          {
            CreationTime = hueEvent.CreationTime,
            SendTime = DateTimeOffset.UtcNow,
            BridgeIp = bridgeIp,
            Id = data.Id,
            Type = data.Type,
            EventDetails = new EventDetails
            {
              Name = data.Metadata?.Name,
              IdV1 = data.IdV1,
              ExtensionData = JsonElementConverter.ConvertJsonElementDictionary(data.ExtensionData)
            }
          };

          await Clients.Group(bridgeId.ToString()).ReceiveEvent(eventData);
        }
      }
    }
  }
}
