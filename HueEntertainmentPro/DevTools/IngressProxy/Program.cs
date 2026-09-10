// A miniature replica of the Home Assistant ingress proxy, used for local
// development and verification of the dynamic <base href> fix.
//
// In production, HA serves the addon inside an iframe at
//   http://homeassistant.local:8123/api/hassio_ingress/<token>/
// and the supervisor strips that prefix before forwarding to the addon, so the
// addon always sees plain "/" paths.
//
// This proxy does the same thing locally:
//   http://localhost:5264/ingress/<token>/...  ==>  http://localhost:5159/...
// including the X-Ingress-Path header the supervisor sets. Open the prefixed
// URL in a browser and verify that gRPC, SignalR and static assets are all
// requested with the /ingress/<token> prefix (the client-side <base href>
// script in index.html derives that prefix from window.location.pathname).

using Microsoft.AspNetCore.Http.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Mirror the supervisor behaviour: stamp the ingress prefix into a request
// header so the upstream app can recover its public base path if needed
// (the Blazor index.html <base href> is derived from window.location, but the
// header keeps this replica faithful to HA).
var pathPrefix = PathString.FromUriComponent(
    builder.Configuration["Ingress:PathPrefix"] ?? "/ingress");

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments(pathPrefix, out var remaining))
    {
        var segments = remaining.ToUriComponent().Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 0)
        {
            context.Request.Headers["X-Ingress-Path"] =
                $"{pathPrefix.ToUriComponent()}/{segments[0]}";
        }
    }

    await next(context);
});

app.MapReverseProxy();

app.Run();
