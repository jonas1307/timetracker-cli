using System.Net;
using System.Text;

namespace Timetracker.Tests;

/// <summary>
/// Captures the outgoing request and returns a canned response, so HttpService methods can
/// be exercised without any real network access.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _body;

    public HttpRequestMessage LastRequest { get; private set; }
    public string LastRequestBody { get; private set; }

    public StubHttpMessageHandler(HttpStatusCode status, string body = "")
    {
        _status = status;
        _body = body;
    }

    /// <summary>The request URI with query parameters URL-decoded, for readable assertions.</summary>
    public string DecodedQuery => Uri.UnescapeDataString(LastRequest?.RequestUri?.Query ?? string.Empty);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        if (request.Content != null)
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(_status)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json"),
        };
    }
}
