using System.Net;
using System.Text;
using Hl7Hackathon.Fhir.Common;

namespace Hl7Hackathon.UnitTests.Stubs;

/// <summary>Records every request (with its body read up front) and answers with the queued responses in order.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    public List<RecordedRequest> Requests { get; } = [];

    public StubHttpMessageHandler RespondWith(HttpStatusCode statusCode, string body)
    {
        _responses.Enqueue(() => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, FhirMediaTypes.Json)
        });

        return this;
    }

    public StubHttpMessageHandler Throw(Exception exception)
    {
        _responses.Enqueue(() => throw exception);

        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, request.Headers.ToString(), request.Content?.Headers.ContentType?.MediaType, body));

        return _responses.Dequeue()();
    }

    internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string Headers, string? ContentType, string? Body);
}
