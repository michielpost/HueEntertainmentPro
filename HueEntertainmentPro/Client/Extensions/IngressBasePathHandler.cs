using System.Net.Http;

namespace HueEntertainmentPro.Client.Extensions;

// gRPC replaces the channel address's path with the service method path.
// Restore the ingress prefix before passing requests to the gRPC-Web handler.
public sealed class IngressBasePathHandler : DelegatingHandler
{
    private readonly string _basePath;

    public IngressBasePathHandler(string basePath)
    {
        _basePath = basePath.TrimEnd('/') + "/";
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_basePath.Length > 1 &&
            request.RequestUri is { IsAbsoluteUri: true } uri &&
            !uri.AbsolutePath.StartsWith(_basePath, StringComparison.Ordinal) &&
            uri.AbsolutePath != _basePath.TrimEnd('/'))
        {
            request.RequestUri = new UriBuilder(uri)
            {
                Path = _basePath + uri.AbsolutePath.TrimStart('/')
            }.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
