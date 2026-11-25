using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Lab.Common;

namespace Lab.Probes;

/// <summary>
/// Raw SignalR JSON-protocol probes (no client library, so malformed frames are possible).
/// Hub method [Authorize] must hold for every invocation shape and casing.
/// </summary>
internal sealed class SignalRSuite(string host, int port)
{
    private const char RS = '\u001e';

    public async Task RunAsync(ProbeReport report)
    {
        await Invoke(report, "signalr.baseline", "Anonymous Echo works",
            "{\"type\":1,\"invocationId\":\"1\",\"target\":\"Echo\",\"arguments\":[\"hi\"]}", r => r.Contains("\"result\":\"hi\""), true);
        await Invoke(report, "signalr.admin-anon", "Anonymous call to [Authorize(Roles=admin)] method",
            "{\"type\":1,\"invocationId\":\"1\",\"target\":\"AdminSecret\",\"arguments\":[]}", r => r.Contains("HUB-ADMIN-SECRET"), false);
        await Invoke(report, "signalr.admin-case", "Method name casing variant (adminsecret)",
            "{\"type\":1,\"invocationId\":\"1\",\"target\":\"adminsecret\",\"arguments\":[]}", r => r.Contains("HUB-ADMIN-SECRET"), false);
        await Invoke(report, "signalr.admin-stream", "Admin method via StreamInvocation (type 4)",
            "{\"type\":4,\"invocationId\":\"1\",\"target\":\"AdminSecret\",\"arguments\":[]}", r => r.Contains("HUB-ADMIN-SECRET"), false);
        await Invoke(report, "signalr.admin-noid", "Admin method as fire-and-forget (no invocationId)",
            "{\"type\":1,\"target\":\"AdminSecret\",\"arguments\":[]}", r => r.Contains("HUB-ADMIN-SECRET"), false);
        await Invoke(report, "signalr.dup-target", "Duplicate 'target' property (JSON parser differential)",
            "{\"type\":1,\"invocationId\":\"1\",\"target\":\"Echo\",\"target\":\"AdminSecret\",\"arguments\":[\"x\"]}",
            r => r.Contains("HUB-ADMIN-SECRET"), false);
        await Invoke(report, "signalr.type-confusion", "Argument type confusion (object where int[] expected)",
            "{\"type\":1,\"invocationId\":\"1\",\"target\":\"Sum\",\"arguments\":[{\"length\":1}]}", r => r.Contains("\"error\""), true);
        await Invoke(report, "signalr.oversize", "Message larger than MaximumReceiveMessageSize (32 KiB)",
            "{\"type\":1,\"invocationId\":\"1\",\"target\":\"Echo\",\"arguments\":[\"" + new string('a', 64 * 1024) + "\"]}",
            r => r.Contains("\"result\""), false);
        await Invoke(report, "signalr.deep-json", "Deeply nested JSON arguments (depth 10k)",
            "{\"type\":1,\"invocationId\":\"1\",\"target\":\"Echo\",\"arguments\":[" + new string('[', 10_000) + new string(']', 10_000) + "]}",
            r => r.Contains("\"result\""), false);
    }

    private async Task Invoke(ProbeReport report, string id, string title, string message, Func<string, bool> success, bool shouldSucceed)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://{host}:{port}") };
            var neg = await http.PostAsync("/hub/negotiate?negotiateVersion=1", null);
            var token = JsonDocument.Parse(await neg.Content.ReadAsStringAsync()).RootElement.GetProperty("connectionToken").GetString();

            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri($"ws://{host}:{port}/hub?id={token}"), CancellationToken.None);
            await Send(ws, "{\"protocol\":\"json\",\"version\":1}" + RS);
            var handshake = await Receive(ws, TimeSpan.FromSeconds(3));
            await Send(ws, message + RS);
            var reply = await Receive(ws, TimeSpan.FromSeconds(2));
            var ok = success(reply);
            var v = ok == shouldSucceed ? Verdict.Pass : ok ? Verdict.Flag : Verdict.Review;
            report.Add(new ProbeResult("signalr", id, title, v,
                $"handshake={handshake.Trim(RS)} reply={Trim(reply)} state={ws.State}",
                shouldSucceed ? "invocation succeeds" : "error / close - never the protected result",
                sw.Elapsed.TotalMilliseconds, "CWE-862"));
        }
        catch (Exception ex)
        {
            report.Add(new ProbeResult("signalr", id, title, shouldSucceed ? Verdict.Error : Verdict.Pass,
                $"{ex.GetType().Name}: {ex.Message}", "", sw.Elapsed.TotalMilliseconds));
        }
    }

    private static Task Send(ClientWebSocket ws, string text) =>
        ws.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);

    private static async Task<string> Receive(ClientWebSocket ws, TimeSpan timeout)
    {
        var sb = new StringBuilder();
        var buf = new byte[64 * 1024];
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var r = await ws.ReceiveAsync(buf, cts.Token);
                if (r.MessageType == WebSocketMessageType.Close) { sb.Append($"[close:{ws.CloseStatus} {ws.CloseStatusDescription}]"); break; }
                sb.Append(Encoding.UTF8.GetString(buf, 0, r.Count));
                // stop once we have a completion / close message
                var s = sb.ToString();
                if (r.EndOfMessage && (s.Contains("\"type\":3") || s.Contains("\"type\":7") || s.EndsWith("{}" + RS) && s.Length <= 3)) break;
            }
        }
        catch (OperationCanceledException) { sb.Append("[timeout]"); }
        catch (WebSocketException ex) { sb.Append($"[ws:{ex.WebSocketErrorCode}]"); }
        return sb.ToString();
    }

    private static string Trim(string s) => (s.Length > 140 ? s[..140] + "…" : s).Replace(RS, '␞');
}
