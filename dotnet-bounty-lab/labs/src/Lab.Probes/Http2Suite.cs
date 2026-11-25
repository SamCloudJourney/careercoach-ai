using System.Diagnostics;
using Lab.Common;
using static Lab.Probes.H2;

namespace Lab.Probes;

/// <summary>
/// HTTP/2 conformance and resource-exhaustion probes against Kestrel (h2c prior knowledge).
/// "Malformed" requests (RFC 9113 8.1.1) must end in a stream error / 400, never reach the app.
/// Flood probes are regression checks for known classes (Rapid Reset, CONTINUATION flood);
/// a known, already-fixed issue is NOT reportable, but a variant that bypasses the fix is.
/// </summary>
internal sealed class Http2Suite(string host, int port, int appPort)
{
    /// <summary>Bytes of server memory growth after a flood that we treat as a DoS signal.</summary>
    private const long MemoryFlagBytes = 256L * 1024 * 1024;

    private async Task<(long ws, long gc)> ServerMemory()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var doc = System.Text.Json.JsonDocument.Parse(await http.GetStringAsync($"http://{host}:{appPort}/health?gc=true"));
        return (doc.RootElement.GetProperty("workingSetBytes").GetInt64(), doc.RootElement.GetProperty("gcHeapBytes").GetInt64());
    }

    private static string Mb(long b) => $"{b / (1024.0 * 1024):F1} MiB";

    private sealed record Outcome(List<Frame9> Frames, bool Closed, long BytesSent, TimeSpan Elapsed);

    public async Task RunAsync(ProbeReport report)
    {
        await Malformed(report, "h2.baseline", "Well-formed GET", Get(), expectReject: false);

        // --- RFC 9113 8.2 / 8.3 malformed header blocks
        await Malformed(report, "h2.uppercase-name", "Uppercase field name", [.. Get(), ("X-Upper", "1")]);
        await Malformed(report, "h2.conn-specific", "Connection-specific field (connection)", [.. Get(), ("connection", "keep-alive")]);
        await Malformed(report, "h2.transfer-encoding", "transfer-encoding: chunked", [.. Get(), ("transfer-encoding", "chunked")]);
        await Malformed(report, "h2.te-not-trailers", "te: gzip (only 'trailers' allowed)", [.. Get(), ("te", "gzip")]);
        await Malformed(report, "h2.pseudo-after-regular", "Pseudo-header after regular field",
            [(":method", "GET"), (":scheme", "http"), ("x-a", "1"), (":path", "/echo"), (":authority", "lab")]);
        await Malformed(report, "h2.dup-path", "Duplicate :path",
            [.. Get(), (":path", "/echo/second")]);
        await Malformed(report, "h2.missing-method", "Missing :method",
            [(":scheme", "http"), (":authority", "lab"), (":path", "/echo")]);
        await Malformed(report, "h2.empty-path", "Empty :path", Get(path: ""));
        await Malformed(report, "h2.relative-path", ":path without leading slash", Get(path: "echo"));
        await Malformed(report, "h2.unknown-pseudo", "Unknown pseudo-header :evil", [.. Get(), (":evil", "1")]);
        await Malformed(report, "h2.response-pseudo", "Response pseudo-header :status in request", [.. Get(), (":status", "200")]);
        await Malformed(report, "h2.value-crlf", "CRLF inside a field value", [.. Get(), ("x-lab", "a\r\nInjected: yes")],
            forbiddenInEcho: "Injected");
        await Malformed(report, "h2.value-nul", "NUL inside a field value", [.. Get(), ("x-lab", "a\0b")],
            forbiddenInEcho: "a\\u0000b");
        // RFC 9113 8.2.1 deviations with no framing impact on their own -> Review, not Flag.
        await Malformed(report, "h2.value-leading-space", "Leading whitespace in value", [.. Get(), ("x-lab", " padded")],
            acceptedVerdict: Verdict.Review);
        await Malformed(report, "h2.name-colon", "Colon inside regular field name", [.. Get(), ("x:lab", "1")],
            acceptedVerdict: Verdict.Review);
        await Malformed(report, "h2.name-space", "Space inside field name", [.. Get(), ("x lab", "1")],
            acceptedVerdict: Verdict.Review);
        await Malformed(report, "h2.keep-alive", "Connection-specific field (keep-alive)", [.. Get(), ("keep-alive", "timeout=5")]);
        await Malformed(report, "h2.proxy-connection", "Connection-specific field (proxy-connection)", [.. Get(), ("proxy-connection", "keep-alive")]);
        await Malformed(report, "h2.upgrade", "Connection-specific field (upgrade)", [.. Get(), ("upgrade", "websocket")]);
        await Malformed(report, "h2.authority-userinfo", ":authority with userinfo", Get(authority: "user@lab"));
        await ContentLengthMismatch(report);

        // --- connection-level protocol errors (expect GOAWAY)
        await ConnectionError(report, "h2.data-stream0", "DATA on stream 0",
            Frame(FrameType.Data, Flags.None, 0, [1, 2, 3]), "PROTOCOL_ERROR");
        await ConnectionError(report, "h2.even-stream", "Client opens even-numbered stream",
            Frame(FrameType.Headers, Flags.EndHeaders | Flags.EndStream, 2, Hpack(Get())), "PROTOCOL_ERROR");
        await ConnectionError(report, "h2.frame-too-large", "Frame larger than SETTINGS_MAX_FRAME_SIZE",
            Frame(FrameType.Headers, Flags.EndHeaders, 1, new byte[20_000]), "FRAME_SIZE_ERROR");
        await ConnectionError(report, "h2.window-overflow", "WINDOW_UPDATE overflows connection window",
            [.. WindowUpdate(0, 0x7FFFFFFF), .. WindowUpdate(0, 0x7FFFFFFF)], "FLOW_CONTROL_ERROR");
        await ConnectionError(report, "h2.window-zero", "WINDOW_UPDATE increment 0 on connection",
            WindowUpdate(0, 0), "PROTOCOL_ERROR");
        await ConnectionError(report, "h2.hpack-table-too-big", "HPACK dynamic table size update > SETTINGS",
            Frame(FrameType.Headers, Flags.EndHeaders | Flags.EndStream, 1, [.. HpackTableSizeUpdate(1 << 20), .. Hpack(Get())]),
            "COMPRESSION_ERROR");
        await ConnectionError(report, "h2.hpack-bad-index", "HPACK indexed field beyond tables",
            Frame(FrameType.Headers, Flags.EndHeaders | Flags.EndStream, 1, [0xFF, 0xFF, 0x7F]), "COMPRESSION_ERROR");
        await ConnectionError(report, "h2.settings-bad-len", "SETTINGS with length not multiple of 6",
            Frame(FrameType.Settings, Flags.None, 0, [0, 1, 0, 0]), "FRAME_SIZE_ERROR");
        await ConnectionError(report, "h2.continuation-orphan", "CONTINUATION without preceding HEADERS",
            Frame(FrameType.Continuation, Flags.EndHeaders, 1, Hpack(Get())), "PROTOCOL_ERROR");
        await ConnectionError(report, "h2.stream-id-regress", "Stream id lower than a previous one",
            [.. Frame(FrameType.Headers, Flags.EndHeaders | Flags.EndStream, 5, Hpack(Get())),
             .. Frame(FrameType.Headers, Flags.EndHeaders | Flags.EndStream, 3, Hpack(Get()))], "PROTOCOL_ERROR");

        // --- resource exhaustion regressions
        await RapidReset(report);
        await ContinuationFlood(report);
        await PingFlood(report);
        await SettingsFlood(report);
    }

    // ------------------------------------------------------------------ helpers

    private async Task<Outcome> Exchange(byte[] payload, TimeSpan? idle = null, (ushort, uint)[]? settings = null)
    {
        var sw = Stopwatch.StartNew();
        await using var c = await OpenAsync(host, port, settings ?? []);
        long sent = 0;
        try
        {
            await c.SendAsync(payload);
            sent = payload.Length;
        }
        catch (IOException)
        {
            // server closed while we were writing - that is an outcome, not a failure
        }
        var r = await c.ReadUntilIdleAsync(idle ?? TimeSpan.FromMilliseconds(600), TimeSpan.FromSeconds(8));
        return new Outcome(ParseFrames(r.Bytes, out _), r.PeerClosed, sent, sw.Elapsed);
    }

    private static string Summarize(Outcome o) =>
        string.Join(" ", o.Frames.Where(f => f.Type is not (FrameType.Settings or FrameType.WindowUpdate)).Take(8)) +
        (o.Closed ? " [closed]" : "");

    private async Task Malformed(ProbeReport report, string id, string title, (string, string)[] headers,
        bool expectReject = true, string? forbiddenInEcho = null, Verdict acceptedVerdict = Verdict.Flag)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var o = await Exchange(Frame(FrameType.Headers, Flags.EndHeaders | Flags.EndStream, 1, Hpack(headers)));
            var headersFrame = o.Frames.FirstOrDefault(f => f.Type == FrameType.Headers && f.Stream == 1);
            var rst = o.Frames.FirstOrDefault(f => f.Type == FrameType.RstStream && f.Stream == 1);
            var goaway = o.Frames.FirstOrDefault(f => f.Type == FrameType.GoAway);
            var body = string.Concat(o.Frames.Where(f => f.Type == FrameType.Data && f.Stream == 1)
                .Select(f => System.Text.Encoding.Latin1.GetString(f.Payload)));

            Verdict v;
            if (!expectReject)
                v = headersFrame?.Status == 200 ? Verdict.Pass : Verdict.Review;
            else if (forbiddenInEcho is not null && body.Contains(forbiddenInEcho, StringComparison.Ordinal))
                v = Verdict.Flag;
            else if (rst is not null || goaway is not null || headersFrame?.Status is >= 400)
                v = Verdict.Pass;
            else if (headersFrame?.Status == 200 && body.Contains("LAB-ECHO"))
                v = acceptedVerdict; // malformed request reached the application
            else
                v = Verdict.Review;

            report.Add(new ProbeResult("http2", id, title, v, Summarize(o),
                !expectReject ? ":status 200"
                : acceptedVerdict == Verdict.Flag ? "RST_STREAM(PROTOCOL_ERROR) / GOAWAY / 400 - never reaches the app"
                : "malformed per RFC 9113 8.2.1; impact needs an HTTP/2->HTTP/1.1 forwarding chain",
                sw.Elapsed.TotalMilliseconds, "RFC9113-8.1.1"));
        }
        catch (Exception ex)
        {
            report.Add(new ProbeResult("http2", id, title, Verdict.Error, ex.Message, "", sw.Elapsed.TotalMilliseconds));
        }
    }

    private async Task ContentLengthMismatch(ProbeReport report)
    {
        var sw = Stopwatch.StartNew();
        var headers = Frame(FrameType.Headers, Flags.EndHeaders, 1,
            Hpack((":method", "POST"), (":scheme", "http"), (":authority", "lab"), (":path", "/echo"), ("content-length", "3")));
        var data = Frame(FrameType.Data, Flags.EndStream, 1, "hello world"u8);
        var o = await Exchange([.. headers, .. data]);
        var status = o.Frames.FirstOrDefault(f => f.Type == FrameType.Headers)?.Status;
        var body = string.Concat(o.Frames.Where(f => f.Type == FrameType.Data).Select(f => System.Text.Encoding.Latin1.GetString(f.Payload)));
        var v = body.Contains("\"bodyLength\":11") ? Verdict.Flag
              : o.Frames.Any(f => f.Type is FrameType.RstStream or FrameType.GoAway) || status >= 400 ? Verdict.Pass
              : Verdict.Review;
        report.Add(new ProbeResult("http2", "h2.content-length-mismatch", "content-length: 3 but 11 DATA bytes", v,
            Summarize(o), "stream error PROTOCOL_ERROR (RFC 9113 8.1.1)", sw.Elapsed.TotalMilliseconds, "RFC9113-8.1.1"));
    }

    private async Task ConnectionError(ProbeReport report, string id, string title, byte[] payload, string expectedCode)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var o = await Exchange(payload);
            var goaway = o.Frames.FirstOrDefault(f => f.Type == FrameType.GoAway);
            var v = goaway is not null && ErrorName(goaway.ErrorCode) == expectedCode ? Verdict.Pass
                  : goaway is not null || o.Closed ? Verdict.Review
                  : o.Frames.Any(f => f.Status == 200) ? Verdict.Flag
                  : Verdict.Review;
            report.Add(new ProbeResult("http2", id, title, v, Summarize(o), $"GOAWAY({expectedCode})",
                sw.Elapsed.TotalMilliseconds, "RFC9113"));
        }
        catch (Exception ex)
        {
            report.Add(new ProbeResult("http2", id, title, Verdict.Error, ex.Message, "", sw.Elapsed.TotalMilliseconds));
        }
    }

    /// <summary>CVE-2023-44487: open-and-cancel streams faster than the server can account for them.</summary>
    private async Task RapidReset(ProbeReport report)
    {
        const int streams = 5_000;
        var sw = Stopwatch.StartNew();
        var block = Hpack(Get());
        var ms = new MemoryStream();
        for (uint s = 1, n = 0; n < streams; s += 2, n++)
        {
            ms.Write(Frame(FrameType.Headers, Flags.EndHeaders | Flags.EndStream, s, block));
            ms.Write(RstStream(s, 8 /* CANCEL */));
        }
        var before = await ServerMemory();
        var o = await Exchange(ms.ToArray(), TimeSpan.FromSeconds(1.5));
        var after = await ServerMemory();
        var growth = after.ws - before.ws;
        var goaway = o.Frames.FirstOrDefault(f => f.Type == FrameType.GoAway);
        var served = o.Frames.Count(f => f.Type == FrameType.Headers);
        // 10.x drops the connection; 11.x keeps it and refuses excess streams with
        // RST_STREAM(ENHANCE_YOUR_CALM). Both bound the work; the strategy is recorded.
        var calm = o.Frames.Count(f => f.Type == FrameType.RstStream && ErrorName(f.ErrorCode) == "ENHANCE_YOUR_CALM");
        var v = growth > MemoryFlagBytes ? Verdict.Flag
              : (goaway is not null || o.Closed || calm > 0) && served < streams ? Verdict.Pass
              : Verdict.Review;
        report.Add(new ProbeResult("http2", "h2.rapid-reset", $"{streams} HEADERS+RST_STREAM pairs", v,
            $"served={served} refused(ENHANCE_YOUR_CALM)={calm} goaway={goaway?.ToString() ?? "none"} closed={o.Closed} serverWS {Mb(before.ws)} -> {Mb(after.ws)}",
            "connection cut off before all streams are processed; bounded server memory",
            sw.Elapsed.TotalMilliseconds, "CVE-2023-44487"));
    }

    /// <summary>Endless CONTINUATION frames without END_HEADERS must be cut off near the header-size limit.</summary>
    private async Task ContinuationFlood(ProbeReport report)
    {
        var sw = Stopwatch.StartNew();
        await using var c = await OpenAsync(host, port);
        var first = Frame(FrameType.Headers, Flags.None, 1, Hpack(Get()));
        await c.SendAsync(first);
        var chunk = Frame(FrameType.Continuation, Flags.None, 1, Hpack(("x-flood", new string('a', 8000))));
        long sent = first.Length;
        var accepted = true;
        for (var i = 0; i < 2_000 && accepted; i++) // up to ~16 MB of header bytes
        {
            try { await c.SendAsync(chunk); sent += chunk.Length; }
            catch (IOException) { accepted = false; }
        }
        var r = await c.ReadUntilIdleAsync(TimeSpan.FromMilliseconds(800), TimeSpan.FromSeconds(5));
        var frames = ParseFrames(r.Bytes, out _);
        var goaway = frames.FirstOrDefault(f => f.Type == FrameType.GoAway);
        var v = (goaway is not null || !accepted) && sent < 4 * 1024 * 1024 ? Verdict.Pass
              : goaway is not null || !accepted ? Verdict.Review
              : Verdict.Flag;
        report.Add(new ProbeResult("http2", "h2.continuation-flood", "CONTINUATION flood without END_HEADERS", v,
            $"{string.Join(" ", frames.Where(f => f.Type == FrameType.GoAway))} accepted≈{sent / 1024} KiB before cut-off",
            "connection closed after ~MaxRequestHeadersTotalSize (32 KiB) + slack", sw.Elapsed.TotalMilliseconds,
            "CONTINUATION-flood (2024)"));
    }

    private async Task PingFlood(ProbeReport report)
    {
        var sw = Stopwatch.StartNew();
        var ms = new MemoryStream();
        for (ulong i = 0; i < 20_000; i++) ms.Write(Ping(i));
        var before = await ServerMemory();
        var o = await Exchange(ms.ToArray(), TimeSpan.FromSeconds(1));
        var after = await ServerMemory();
        var acks = o.Frames.Count(f => f.Type == FrameType.Ping);
        var goaway = o.Frames.FirstOrDefault(f => f.Type == FrameType.GoAway);
        report.Add(new ProbeResult("http2", "h2.ping-flood", "20k PINGs in one burst",
            after.ws - before.ws > MemoryFlagBytes ? Verdict.Flag : Verdict.Pass,
            $"acks={acks} {goaway} serverWS {Mb(before.ws)} -> {Mb(after.ws)}",
            "server memory stays bounded (CVE-2019-9512 class)", sw.Elapsed.TotalMilliseconds, "CVE-2019-9512"));
    }

    private async Task SettingsFlood(ProbeReport report)
    {
        var sw = Stopwatch.StartNew();
        var ms = new MemoryStream();
        for (var i = 0; i < 20_000; i++) ms.Write(Settings((0x4, 65535)));
        var before = await ServerMemory();
        var o = await Exchange(ms.ToArray(), TimeSpan.FromSeconds(1));
        var after = await ServerMemory();
        var acks = o.Frames.Count(f => f.Type == FrameType.Settings && f.Flags.HasFlag(Flags.Ack));
        var goaway = o.Frames.FirstOrDefault(f => f.Type == FrameType.GoAway);
        report.Add(new ProbeResult("http2", "h2.settings-flood", "20k SETTINGS frames in one burst",
            after.ws - before.ws > MemoryFlagBytes ? Verdict.Flag : Verdict.Pass,
            $"acks={acks} {goaway} serverWS {Mb(before.ws)} -> {Mb(after.ws)}",
            "server memory stays bounded (CVE-2019-9515 class)", sw.Elapsed.TotalMilliseconds, "CVE-2019-9515"));
    }
}
