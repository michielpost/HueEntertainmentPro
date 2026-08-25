using HueEntertainmentPro.Database;
using HueEntertainmentPro.Server.Extensions;
using HueEntertainmentPro.Server.Hubs;
using HueEntertainmentPro.Server.Services;
using HueEntertainmentPro.Services;
using HueLightDJ.Services;
using HueLightDJ.Services.Interfaces;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.AspNetCore.HttpOverrides;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<HueEntertainmentProDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddFluentUIComponents();
builder.Services.AddSignalR();

builder.Services.AddControllers();

builder.Services.AddGrpc();
builder.Services.AddResponseCompression(opts =>
{
  opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
      new[] { "application/octet-stream" });
});
builder.Services.AddCodeFirstGrpc();

builder.Services.AddSingleton<IHubService, HubService>();
builder.Services.AddScoped<BridgeService>();
builder.Services.AddHueLightDJServices();

// Add YARP to proxy to Hue Bridge
builder.Services.AddHueProxyService();

var app = builder.Build();

var sqlLiteBuilder = new SqliteConnectionStringBuilder(connectionString);
var dbPath = Path.GetDirectoryName(sqlLiteBuilder.DataSource);
if (dbPath != null && !Directory.Exists(dbPath))
{
  Directory.CreateDirectory(dbPath);
}

// Ensure database is created and apply migrations
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<HueEntertainmentProDbContext>();
  //db.Database.EnsureCreated();
  db.Database.Migrate(); // Creates the database if it does not exist and applies any pending migrations
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseWebAssemblyDebugging();
}
else
{
  app.UseExceptionHandler("/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

// Trust forwarded headers from the Home Assistant ingress proxy so the app
// correctly detects HTTPS. Without this, the HTTPS redirect below loops when
// the app is served through the HA ingress (which forwards over plain HTTP).
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    KnownIPNetworks =
    {
        System.Net.IPNetwork.Parse("10.0.0.0/8"),
        System.Net.IPNetwork.Parse("172.16.0.0/12"),
        System.Net.IPNetwork.Parse("192.168.0.0/16"),
        System.Net.IPNetwork.Parse("fc00::/7") // IPv6 unique-local (Docker networks with IPv6)
    }
});

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();


//Reverse Proxy to Hue Bridge, disabled in DEMO mode
if (app.Environment.EnvironmentName != "DEMO")
{
  app.MapReverseProxy();
}

app.UseRouting();

app.UseGrpcWeb();
app.MapGrpcService<BridgeDataService>().EnableGrpcWeb();
app.MapGrpcService<ProAreaDataService>().EnableGrpcWeb();
app.MapGrpcService<HueSetupService>().EnableGrpcWeb();
app.MapGrpcService<LightDJService>().EnableGrpcWeb();

app.MapHub<PreviewHub>("/previewhub");
app.MapHub<EventMonitorHub>("/eventmonitorhub");


app.MapRazorPages();
app.MapControllers();

// Serve the Blazor index.html with a <base href> matching the path the app is
// reached on. Home Assistant ingress serves the app under
// /api/hassio_ingress/<token>/ (sent via the X-Ingress-Path header) while the
// proxy strips that prefix before forwarding. Rewriting the base href keeps all
// relative asset and API URLs working both behind ingress and on direct access.
// The {**path} pattern (without a nonfile constraint) is required because the
// app has routes containing dots, e.g. /bridge/{*Ip} with an IP address.
app.MapFallback("{**path}", async context =>
{
    var indexFile = context.RequestServices
        .GetRequiredService<IWebHostEnvironment>()
        .WebRootFileProvider.GetFileInfo("index.html");

    if (!indexFile.Exists)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    string html;
    await using (var stream = indexFile.CreateReadStream())
    using (var reader = new StreamReader(stream))
    {
        html = await reader.ReadToEndAsync();
    }

    string? ingressPath = context.Request.Headers["X-Ingress-Path"];
    if (!string.IsNullOrEmpty(ingressPath) && ingressPath.StartsWith('/'))
    {
        var baseHref = System.Net.WebUtility.HtmlEncode(ingressPath.TrimEnd('/') + "/");
        html = html.Replace("<base href=\"/\" />", $"<base href=\"{baseHref}\" />");
    }

    // Never cache: the ingress token (and thus the base href) can change.
    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.Headers.CacheControl = "no-cache";
    await context.Response.WriteAsync(html);
});

app.Run();
