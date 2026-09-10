var builder = DistributedApplication.CreateBuilder(args);

var ingressToken = builder.AddParameter("ingress-token", "d758de61_huelightdj");

var server = builder.AddProject<Projects.HueEntertainmentPro_Server>("hueserver", launchProfileName: "http")
    .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Application");

builder.AddProject<Projects.IngressProxy>("ingressproxy", launchProfileName: "http")
    .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
    .WithEnvironment(
        "ReverseProxy__Clusters__hue-server__Destinations__d1__Address",
        server.GetEndpoint("http"))
    .WaitFor(server)
    .WithUrls(async context =>
    {
        var token = await ingressToken.Resource.GetValueAsync(context.CancellationToken);
        var url = context.Urls.Single(url => url.Endpoint?.EndpointName == "http");
        url.Url = $"{url.Url.TrimEnd('/')}/ingress/{Uri.EscapeDataString(token)}/";
        url.DisplayText = "Application (ingress)";
    });

await builder.Build().RunAsync();
