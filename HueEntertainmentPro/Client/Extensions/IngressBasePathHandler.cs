using System.Net.Http;

namespace HueEntertainmentPro.Client.Extensions;

/// <summary>
/// Re-applies the app's base path (e.g. a Home Assistant ingress prefix such as
/// "/api/hassio_ingress/&lt;token&gt;") to outgoing requests.
/// </summary>
/// <remarks>
/// gRPC-Web channels build request URLs by replacing the path of the channel
/// address with the service method path (UriBuilder.Path), so the ingress prefix
/// is lost and requests resolve against the site root. Other clients (assets,
/// SignalR) keep the prefix because they resolve relative URLs against the
/// <c>&lt;base href&gt;</c> from index.html. This handler puts the prefix back for
/// any URL that was resolved against the root while the app is hosted under a
/// non-root base path.
/// </remarks>
public sealed class IngressBasePathHandler : DelegatingHandler
{
    private readonly string _basePath;

    public IngressBasePathHandler(string basePath)
    {
        _basePath = basePath;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // _basePath is "/" when the app runs at the site root: nothing to do.
        // Otherwise prefix any URL that doesn't already include the base path.
        if (_basePath.Length > 1 &&
            request.RequestUri is { IsAbsoluteUri: true } uri &&
            !uri.PathAndQuery.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            var builder = new UriBuilder(uri);
            builder.Path = _basePath.TrimEnd('/') + "/" + uri.AbsolutePath.TrimStart('/');
            request.RequestUri = builder.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
