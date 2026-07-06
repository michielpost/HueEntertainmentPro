using Grpc.Net.Client.Web;
using HueEntertainmentPro.Client;
using HueEntertainmentPro.Client.Extensions;
using HueEntertainmentPro.Client.Services;
using HueEntertainmentPro.Shared.Interfaces;
using HueLightDJ.Blazor.Controls;
using HueLightDJ.Services.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddFluentUIComponents();

builder.Services.AddTransient<HueJsInterop>();
builder.Services.AddTransient<AudioBeatJsInterop>();
builder.Services.AddTransient<ThreejsPreviewJsInterop>();
builder.Services.AddSingleton<IHubService, SignalRClientHubService>();
builder.Services.AddSingleton<EventMonitorService>();
builder.Services.AddSingleton<ResourceExplorerService>();

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddHttpClient("ServerAPI",
  client =>
  {
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
  })
  .AddHttpMessageHandler(() => new GrpcWebHandler(GrpcWebMode.GrpcWeb));

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
.CreateClient("ServerAPI")
);

builder.Services.AddGrpcService<IBridgeDataService>();
builder.Services.AddGrpcService<IProAreaDataService>();
builder.Services.AddGrpcService<IHueSetupService>();
builder.Services.AddGrpcService<ILightDJService>();

await builder.Build().RunAsync();
