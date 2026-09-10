var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Home Assistant strips the public prefix and passes it to the app in this header.
var pathPrefix = new PathString("/ingress");

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
