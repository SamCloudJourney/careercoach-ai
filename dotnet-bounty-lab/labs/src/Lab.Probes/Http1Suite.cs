using System.Diagnostics;
using Lab.Common;

namespace Lab.Probes;

/// <summary>
/// HTTP/1.1 message-framing and parsing probes against Kestrel (RFC 9110 / RFC 9112).
///
/// Oracle: each probe that carries a smuggled request hides
///   GET /echo/SMUGGLED HTTP/1.1
/// inside what a strict parser treats as body bytes. If Kestrel's echo of that path comes
/// back on the same connection, Kestrel framed the message differently from a CL-trusting
/// intermediary - the precondition for request smuggling (cf. CVE-2025-55315).
/// </summary>
internal sealed class Http1Suite(string host, int port)
{
    private const string Smuggled = "GET /echo/SMUGGLED HTTP/1.1\r\nHost: lab\r\n\r\n";
    private const string SmuggledMarker = "\"path\":\"/echo/SMUGGLED\"";

    private sealed record Probe(
        string Id,
        string Title,
        string Request,
        Func<ReadResult, (Verdict, string)> Judge,
        string Expected,
        string? Reference = null);

    public async Task RunAsync(ProbeReport report)
    {
        foreach (var p in Probes())
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await using var c = await RawConnection.ConnectAsync(host, port);
                await c.SendAsync(p.Request);
                var r = await c.ReadUntilIdleAsync(TimeSpan.FromMilliseconds(700), TimeSpan.FromSeconds(6));
                var (verdict, observed) = p.Judge(r);
                report.Add(new ProbeResult("http1", p.Id, p.Title, verdict, observed, p.Expected,
                    sw.Elapsed.TotalMilliseconds, p.Reference));
            }
            catch (Exception ex)
            {
                report.Add(new ProbeResult("http1", p.Id, p.Title, Verdict.Error, ex.GetType().Name + ": " + ex.Message,
                    p.Expected, sw.Elapsed.TotalMilliseconds, p.Reference));
            }
        }
    }

    // ------------------------------------------------------------------ judges

    private static string Describe(ReadResult r) =>
        $"status=[{string.Join(",", r.StatusCodes)}] closed={r.PeerClosed} smuggled={r.Text.Contains(SmuggledMarker)} bytes={r.Bytes.Length}";

    /// <summary>Must reject with 4xx/5xx; anything reaching the app is a parser acceptance bug.</summary>
    private static Func<ReadResult, (Verdict, string)> MustReject(params int[] allowed) => r =>
    {
        var codes = r.StatusCodes;
        if (r.Text.Contains(SmuggledMarker)) return (Verdict.Flag, Describe(r));
        if (codes.Count == 0) return (r.PeerClosed ? Verdict.Pass : Verdict.Review, Describe(r)); // closed without answer is acceptable
        if (codes.All(c => c >= 400)) return (allowed.Length == 0 || codes.All(allowed.Contains) ? Verdict.Pass : Verdict.Review, Describe(r));
        return (Verdict.Flag, Describe(r));
    };

    /// <summary>Request is legal; expect normal 200 handling.</summary>
    private static Func<ReadResult, (Verdict, string)> MustAccept(int expectedResponses = 1) => r =>
    {
        var codes = r.StatusCodes;
        var ok = codes.Count == expectedResponses && codes.All(c => c == 200);
        return (ok ? Verdict.Pass : Verdict.Review, Describe(r));
    };

    /// <summary>
    /// RFC 9112 6.1: with both TE and CL the server MAY reject or process by TE, but MUST close the
    /// connection afterwards. A smuggled request being answered means the connection stayed open.
    /// </summary>
    private static (Verdict, string) TeAndClJudge(ReadResult r)
    {
        var codes = r.StatusCodes;
        if (r.Text.Contains(SmuggledMarker)) return (Verdict.Flag, Describe(r) + " (connection reused after TE+CL)");
        if (codes.Count >= 1 && codes[0] >= 400) return (Verdict.Pass, Describe(r));
        if (codes.Count == 1 && r.PeerClosed) return (Verdict.Pass, Describe(r));
        return (Verdict.Review, Describe(r));
    }

    private static (Verdict, string) EchoMustNotContain(ReadResult r, string forbidden)
    {
        var codes = r.StatusCodes;
        if (codes.Count > 0 && codes[0] == 200 && r.Text.Contains(forbidden, StringComparison.Ordinal))
            return (Verdict.Flag, Describe(r) + $" (app saw '{forbidden.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\0", "\\0")}')");
        return MustReject()(r);
    }

    // ------------------------------------------------------------------ probes

    private IEnumerable<Probe> Probes()
    {
        const string H = "Host: lab\r\n";
        string Post(string headers, string body) => $"POST /echo HTTP/1.1\r\n{H}{headers}\r\n{body}";
        var chunkedSmuggle = "0\r\n\r\n" + Smuggled;

        yield return new("h1.baseline", "Plain GET is answered", $"GET /echo HTTP/1.1\r\n{H}\r\n", MustAccept(), "200");
        yield return new("h1.pipelining", "Two pipelined GETs get two responses",
            $"GET /echo HTTP/1.1\r\n{H}\r\nGET /echo HTTP/1.1\r\n{H}\r\n", MustAccept(2), "200,200");

        // --- Transfer-Encoding / Content-Length ambiguity
        yield return new("h1.te-cl", "TE: chunked + Content-Length (CL.TE)",
            Post($"Content-Length: {chunkedSmuggle.Length}\r\nTransfer-Encoding: chunked\r\n", chunkedSmuggle),
            TeAndClJudge, "400, or 200 then connection close (RFC 9112 6.1 MUST close)", "RFC9112-6.1");
        yield return new("h1.cl-te-order", "Content-Length before TE, TE last wins?",
            Post($"Transfer-Encoding: chunked\r\nContent-Length: 4\r\n", chunkedSmuggle),
            TeAndClJudge, "400, or 200 then connection close", "RFC9112-6.1");

        foreach (var (id, te) in new[]
                 {
                     ("h1.te-xchunked", "Transfer-Encoding: xchunked"),
                     ("h1.te-space-before-colon", "Transfer-Encoding : chunked"),
                     ("h1.te-tab-value", "Transfer-Encoding:\tchunked"),
                     ("h1.te-chunked-identity", "Transfer-Encoding: chunked, identity"),
                     ("h1.te-identity", "Transfer-Encoding: identity"),
                     ("h1.te-dup", "Transfer-Encoding: chunked\r\nTransfer-Encoding: identity"),
                     ("h1.te-case", "Transfer-Encoding: CHUNKED"),
                     ("h1.te-vtab", "Transfer-Encoding:\vchunked"),
                     ("h1.te-quoted", "Transfer-Encoding: \"chunked\""),
                 })
        {
            yield return new(id, $"Obfuscated TE + CL [{te.Replace("\r\n", " | ").Replace("\t", "\\t").Replace("\v", "\\v")}]",
                Post($"Content-Length: {chunkedSmuggle.Length}\r\n{te}\r\n", chunkedSmuggle),
                TeAndClJudge, "400/501, or close after response; never answer the smuggled request", "RFC9112-6.1");
        }

        yield return new("h1.te-http10", "HTTP/1.0 with Transfer-Encoding: chunked",
            $"POST /echo HTTP/1.0\r\n{H}Transfer-Encoding: chunked\r\nContent-Length: {chunkedSmuggle.Length}\r\n\r\n{chunkedSmuggle}",
            TeAndClJudge, "400, or treat as faulty framing and close (RFC 9112 6.1)", "RFC9112-6.1");

        yield return new("h1.cl-dup-diff", "Two different Content-Length values",
            Post("Content-Length: 5\r\nContent-Length: 6\r\n", "hello!"), MustReject(), "400", "RFC9112-6.3");
        yield return new("h1.cl-list", "Content-Length: 5, 6 (list form)",
            Post("Content-Length: 5, 6\r\n", "hello!"), MustReject(), "400", "RFC9112-6.3");
        yield return new("h1.cl-plus", "Content-Length: +5",
            Post("Content-Length: +5\r\n", "hello"), MustReject(), "400", "RFC9110-8.6");
        yield return new("h1.cl-negative", "Content-Length: -1",
            Post("Content-Length: -1\r\n", ""), MustReject(), "400", "RFC9110-8.6");
        yield return new("h1.cl-overflow", "Content-Length: 2^64+1",
            Post("Content-Length: 18446744073709551617\r\n", ""), MustReject(), "400", "RFC9110-8.6");
        yield return new("h1.cl-hex", "Content-Length: 0x5",
            Post("Content-Length: 0x5\r\n", "hello"), MustReject(), "400", "RFC9110-8.6");

        // --- chunked body edge cases
        yield return new("h1.chunk-ext-bare-lf", "Chunk extension terminated by bare LF (CVE-2025-55315 class)",
            Post("Transfer-Encoding: chunked\r\n", "2;x\nxx\r\n0\r\n\r\n" + Smuggled),
            MustReject(), "400, smuggled request never answered", "CVE-2025-55315");
        yield return new("h1.chunk-ext-bare-lf-2", "Chunk extension with LF inside then chunk-size line",
            Post("Transfer-Encoding: chunked\r\n", "1;a=\"\n\"\r\nX\r\n0\r\n\r\n" + Smuggled),
            MustReject(), "400", "CVE-2025-55315");
        yield return new("h1.chunk-size-overflow", "Chunk size FFFFFFFFFFFFFFFF1 (64-bit overflow)",
            Post("Transfer-Encoding: chunked\r\n", "FFFFFFFFFFFFFFFF1\r\nX\r\n0\r\n\r\n"), MustReject(), "400");
        yield return new("h1.chunk-size-negative", "Chunk size -1",
            Post("Transfer-Encoding: chunked\r\n", "-1\r\nX\r\n0\r\n\r\n"), MustReject(), "400");
        yield return new("h1.chunk-size-space", "Chunk size with leading whitespace ' 5'",
            Post("Transfer-Encoding: chunked\r\n", " 5\r\nhello\r\n0\r\n\r\n"), MustReject(), "400");
        yield return new("h1.chunk-missing-crlf", "Chunk data not followed by CRLF",
            Post("Transfer-Encoding: chunked\r\n", "5\r\nhelloXX0\r\n\r\n"), MustReject(), "400");
        yield return new("h1.chunk-lf-only-data", "Chunk data terminated by bare LF, smuggle after",
            Post("Transfer-Encoding: chunked\r\n", "5\r\nhello\n0\r\n\r\n" + Smuggled), MustReject(), "400");
        yield return new("h1.chunk-trailer-cl", "Trailer section carries Content-Length / Host",
            Post("Transfer-Encoding: chunked\r\n", "5\r\nhello\r\n0\r\nContent-Length: 100\r\nHost: evil\r\n\r\n"),
            r => r.StatusCodes.FirstOrDefault() == 200 && r.Text.Contains("\"host\":\"evil\"")
                ? (Verdict.Flag, Describe(r) + " (trailer merged into headers)")
                : (r.StatusCodes.FirstOrDefault() is 200 or 400 ? Verdict.Pass : Verdict.Review, Describe(r)),
            "trailer fields never override framing/host (RFC 9110 6.5.1)", "RFC9110-6.5.1");

        // --- header syntax
        yield return new("h1.header-space-colon", "Whitespace between field name and colon",
            $"GET /echo HTTP/1.1\r\nHost : lab\r\n\r\n", MustReject(), "400 (RFC 9112 5.1 MUST reject)", "RFC9112-5.1");
        yield return new("h1.header-bare-cr", "Bare CR inside a header value",
            $"GET /echo HTTP/1.1\r\n{H}X-Lab: a\rb\r\n\r\n", r => EchoMustNotContain(r, "a\\rb"), "400, or CR replaced with SP", "RFC9112-2.2");
        yield return new("h1.header-nul", "NUL byte inside a header value",
            $"GET /echo HTTP/1.1\r\n{H}X-Lab: a\0b\r\n\r\n", r => EchoMustNotContain(r, "a\\u0000b"), "400", "RFC9110-5.5");
        yield return new("h1.header-obs-fold", "obs-fold continuation line",
            $"GET /echo HTTP/1.1\r\n{H}X-Lab: a\r\n b\r\n\r\n", MustReject(), "400 (or fold replaced with SP)", "RFC9112-5.2");
        yield return new("h1.header-name-utf8", "Non-token bytes in field name",
            $"GET /echo HTTP/1.1\r\n{H}X-L\u00e4b: v\r\n\r\n", MustReject(), "400", "RFC9110-5.1");
        yield return new("h1.bare-lf-headers", "Bare LF line terminators throughout",
            "GET /echo HTTP/1.1\nHost: lab\n\n",
            r => (r.StatusCodes.FirstOrDefault() is 200 or 400 ? Verdict.Pass : Verdict.Review, Describe(r)),
            "200 or 400 (RFC 9112 2.2 MAY accept LF); must be consistent with chunk parsing", "RFC9112-2.2");

        // --- host / target
        yield return new("h1.no-host", "HTTP/1.1 request without Host",
            "GET /echo HTTP/1.1\r\n\r\n", MustReject(), "400 (RFC 9112 3.2 MUST)", "RFC9112-3.2");
        yield return new("h1.dup-host", "Two Host headers",
            $"GET /echo HTTP/1.1\r\n{H}Host: other\r\n\r\n", MustReject(), "400 (RFC 9112 3.2 MUST)", "RFC9112-3.2");
        yield return new("h1.absolute-form-mismatch", "Absolute-form target with different Host",
            "GET http://lab/echo HTTP/1.1\r\nHost: other\r\n\r\n",
            r => r.StatusCodes.FirstOrDefault() == 200 && r.Text.Contains("\"host\":\"other\"")
                ? (Verdict.Flag, Describe(r) + " (app trusted Host over request-target authority)")
                : (Verdict.Pass, Describe(r)),
            "400, or Host replaced by target authority (RFC 9112 3.2.2)", "RFC9112-3.2.2");
        yield return new("h1.fragment-in-target", "Fragment in request-target",
            $"GET /echo#frag HTTP/1.1\r\n{H}\r\n", MustReject(), "400", "RFC9112-3.2");
        yield return new("h1.target-space", "Unencoded space in request-target",
            $"GET /echo /x HTTP/1.1\r\n{H}\r\n", MustReject(), "400", "RFC9112-3");
        yield return new("h1.version-garbage", "Invalid HTTP-version token",
            $"GET /echo HTTP/1.1x\r\n{H}\r\n", MustReject(), "400/505", "RFC9112-2.3");
        yield return new("h1.method-garbage", "Method with separator characters",
            $"GE(T /echo HTTP/1.1\r\n{H}\r\n", MustReject(), "400", "RFC9110-9.1");

        // --- limits (defaults: 32 KiB headers, 100 headers, 8 KiB request line)
        yield return new("h1.limit-header-size", "40 KiB header block",
            $"GET /echo HTTP/1.1\r\n{H}X-Big: {new string('a', 40 * 1024)}\r\n\r\n", MustReject(431, 400), "431");
        yield return new("h1.limit-header-count", "150 header fields",
            $"GET /echo HTTP/1.1\r\n{H}{string.Concat(Enumerable.Range(0, 150).Select(i => $"X-H{i}: v\r\n"))}\r\n",
            MustReject(431, 400), "431");
        yield return new("h1.limit-request-line", "10 KiB request target",
            $"GET /echo?{new string('a', 10 * 1024)} HTTP/1.1\r\n{H}\r\n", MustReject(414, 400), "414");

        // --- connection management
        yield return new("h1.upgrade-h2c", "Upgrade: h2c is ignored (no 101)",
            $"GET /echo HTTP/1.1\r\n{H}Connection: Upgrade, HTTP2-Settings\r\nUpgrade: h2c\r\nHTTP2-Settings: AAMAAABkAAQAoAAAAAIAAAAA\r\n\r\n",
            r => (r.StatusCodes.Contains(101) ? Verdict.Review : Verdict.Pass, Describe(r)), "200 without protocol switch");
        yield return new("h1.expect-100", "Expect: 100-continue with body",
            Post("Content-Length: 5\r\nExpect: 100-continue\r\n", "hello"),
            r => (r.StatusCodes.LastOrDefault() == 200 ? Verdict.Pass : Verdict.Review, Describe(r)), "100 then 200");
    }
}
