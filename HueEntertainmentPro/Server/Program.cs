using HueEntertainmentPro.Database;
using HueEntertainmentPro.Server.Hubs;
using HueEntertainmentPro.Server.Services;
using HueEntertainmentPro.Services;
using HueLightDJ.Services;
using HueLightDJ.Services.Interfaces;
using Microsoft.AspNetCore.ResponseCompression;
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

builder.Services.AddGrpc();
builder.Services.AddResponseCompression(opts =>
{
  opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
      new[] { "application/octet-stream" });
});
builder.Services.AddCodeFirstGrpc();

builder.Services.AddSingleton<IHubService, HubService>();
builder.Services.AddHueLightDJServices();

var app = builder.Build();

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

app.UseRouting();

app.UseGrpcWeb();
app.MapGrpcService<BridgeDataService>().EnableGrpcWeb();
app.MapGrpcService<ProAreaDataService>().EnableGrpcWeb();
app.MapGrpcService<HueSetupService>().EnableGrpcWeb();
app.MapGrpcService<LightDJService>().EnableGrpcWeb();

app.MapHub<PreviewHub>("/previewhub");


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
