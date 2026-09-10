// Local replica of the Home Assistant ingress setup, orchestrated with .NET Aspire.
//
// Replaces the old run-ingress.ps1 script:
//   - HueEntertainmentPro.Server -> http://localhost:5159  (the "addon")
//   - IngressProxy (YARP)        -> http://localhost:5264  (the HA supervisor replica)
//
// The app is exposed under http://localhost:5264/ingress/<token>/ — exactly like
// HA's /api/hassio_ingress/<token>/: the prefix is stripped before forwarding to
// the server and the X-Ingress-Path header is set (see IngressProxy/Program.cs).
//
// Run with:
//   dotnet run --project HueEntertainmentPro.AppHost
// then open http://localhost:5264/ingress/d758de61_huelightdj/ in a browser and
// verify that gRPC, SignalR and static-asset requests all carry the
// /ingress/<token> prefix.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

// Mimics the dynamic HA ingress token (HA uses /api/hassio_ingress/<token>/).
// Can be overridden, e.g. via  Parameters__ingress-token=...  in user secrets.
var ingressToken = builder.AddParameter("ingress-token", "d758de61_huelightdj");

var server = builder.AddProject<Projects.HueEntertainmentPro_Server>("hueserver")
    .WithHttpEndpoint(port: 5159, name: "http", isProxied: false)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var ingressProxy = builder.AddProject<Projects.IngressProxy>("ingressproxy")
    .WithHttpEndpoint(port: 5264, name: "http", isProxied: false)
    // Point the YARP cluster at the server resource, like the HA supervisor
    // forwards to the addon. Passing the EndpointReference (not the .Url) lets
    // Aspire resolve it when the proxy starts, once the endpoint is allocated.
    // Overrides ReverseProxy:Clusters:hue-server:Destinations:d1:Address.
    .WithEnvironment(
        "ReverseProxy__Clusters__hue-server__Destinations__d1__Address",
        server.GetEndpoint("http"));

using var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("HueEntertainmentPro.AppHost");
var ingressUrl = $"http://localhost:5264/ingress/{ingressToken.Resource.Value}/";
logger.LogInformation(
    "Home Assistant ingress replica ready at {IngressUrl}", ingressUrl);
logger.LogInformation(
    "gRPC via ingress: {GrpcUrl}",
    $"{ingressUrl}HueLightDJ.Services.Interfaces.LightDJService/GetStatus");

await app.RunAsync();
