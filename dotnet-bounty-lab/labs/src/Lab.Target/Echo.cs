using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.Features;

namespace Lab.Target;

/// <summary>
/// Reflects exactly what Kestrel parsed. Probes compare this against the bytes they sent:
/// any mismatch in method, target, headers or body length is a parser differential, and a
/// second echo for one request is a desync.
/// </summary>
internal static class Echo
{
    public static async Task HandleAsync(HttpContext ctx)
    {
        var req = ctx.Request;
        var bodyHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long bodyLen = 0;
        var buffer = new byte[16 * 1024];
        int n;
        while ((n = await req.Body.ReadAsync(buffer, ctx.RequestAborted)) > 0)
        {
            bodyHash.AppendData(buffer, 0, n);
            bodyLen += n;
        }

        var rawTarget = ctx.Features.Get<IHttpRequestFeature>()?.RawTarget;
        await ctx.Response.WriteAsJsonAsync(new
        {
            marker = "LAB-ECHO",
            protocol = req.Protocol,
            method = req.Method,
            rawTarget,
            path = req.Path.Value,
            pathBase = req.PathBase.Value,
            query = req.QueryString.Value,
            host = req.Host.Value,
            headers = req.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray()),
            contentLength = req.ContentLength,
            bodyLength = bodyLen,
            bodySha256 = Convert.ToHexString(bodyHash.GetHashAndReset()),
            trailers = req.SupportsTrailers() && req.CheckTrailersAvailable()
                ? req.GetTrailer("x-lab-trailer").ToString()
                : null,
            connectionId = ctx.Connection.Id,
        }, ctx.RequestAborted);
    }
}
