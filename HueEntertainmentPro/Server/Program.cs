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
app.MapFallbackToFile("{**path}", "index.html");

app.Run();
