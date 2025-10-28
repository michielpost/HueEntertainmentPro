using Grpc.Net.Client;
using Microsoft.AspNetCore.Components;
using ProtoBuf.Grpc.Client;

namespace HueEntertainmentPro.Client.Extensions
{
  public static class ServiceCollectionExtensions
  {
    public static void AddGrpcService<TService>(this IServiceCollection services) where TService : class
    {
      services.AddSingleton(services =>
      {
        var httpFactory = services.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpFactory.CreateClient("ServerAPI");
        var baseUri = services.GetRequiredService<NavigationManager>().BaseUri;
        var channel = GrpcChannel.ForAddress(baseUri, new GrpcChannelOptions { HttpClient = httpClient });
        return channel.CreateGrpcService<TService>();
      });
    }
  }
}
