using System.Diagnostics;
using System.Net;
using System.Text;
using Lab.Common;

namespace Lab.Probes;

/// <summary>
/// Authorization-bypass, token-validation, antiforgery and static-file probes against the
/// app listener. The target grants nobody the admin role, so any response containing an
/// "ADMIN-SECRET" marker is a Security Feature Bypass / EoP candidate in ASP.NET Core itself.
/// </summary>
internal sealed class AuthSuite(string host, int port)
{
    private readonly Uri _base = new($"http://{host}:{port}");

    public async Task RunAsync(ProbeReport report)
    {
        using var anon = NewClient(out _);
        using var user = NewClient(out var userCookies);
        var login = await user.PostAsync("/login?user=alice", null);
        Add(report, "auth.baseline-login", "Non-admin cookie login works",
            login.StatusCode == HttpStatusCode.OK ? Verdict.Pass : Verdict.Error, $"{(int)login.StatusCode}", "200", 0);

        await PathVariants(report, anon, "anon");
        await PathVariants(report, user, "user");
        await RawPathVariants(report);
        await Jwt(report, anon);
        await Antiforgery(report);
        await StaticFiles(report, anon);
    }

    private HttpClient NewClient(out CookieContainer jar)
    {
        jar = new CookieContainer();
        var handler = new SocketsHttpHandler { CookieContainer = jar, AllowAutoRedirect = false, UseProxy = false };
        return new HttpClient(handler) { BaseAddress = _base, Timeout = TimeSpan.FromSeconds(10) };
    }

    private static void Add(ProbeReport r, string id, string title, Verdict v, string observed, string expected, double ms, string? reference = null) =>
        r.Add(new ProbeResult("auth", id, title, v, observed, expected, ms, reference));

    // ------------------------------------------------------------------ routing / normalisation

    private static readonly string[] AdminPathVariants =
    [
        "/admin", "/admin/", "/admin/x", "/ADMIN", "/Admin/x", "/%61dmin", "/%41DMIN", "/admin%2fx", "/admin%2Fx",
        "/./admin", "/x/../admin", "/x/..%2fadmin", "/%2e/admin", "/%2e%2e/admin",
        "/admin;x", "/admin%3bx", "/admin%00", "/admin%20", "/admin.", "/admin%5cx", "/admin%c0%afx",
        "/%EF%BD%81dmin", "/a%CC%81dmin", "/admin?x=1", "/admin#x", "/admin/%2e%2e/admin",
    ];

    private static async Task PathVariants(ProbeReport report, HttpClient client, string who)
    {
        foreach (var path in AdminPathVariants)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, path);
                using var resp = await client.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                var leaked = body.Contains("ADMIN-SECRET", StringComparison.Ordinal);
                Add(report, $"auth.path.{who}.{Slug(path)}", $"{who} GET {path}",
                    leaked ? Verdict.Flag : Verdict.Pass, $"{(int)resp.StatusCode} {Trim(body)}",
                    "401/403/404/400 - never the admin body", sw.Elapsed.TotalMilliseconds, "CWE-863");
            }
            catch (Exception ex)
            {
                Add(report, $"auth.path.{who}.{Slug(path)}", $"{who} GET {path}", Verdict.Error, ex.Message, "", sw.Elapsed.TotalMilliseconds);
            }
        }
    }

    /// <summary>Request-targets HttpClient would normalise away: backslashes, raw dot-segments, absolute-form.</summary>
    private async Task RawPathVariants(ProbeReport report)
    {
        string[] targets =
        [
            "//admin", "///admin/x", "/admin\\x", "/x/..\\admin", "/.%2e/admin", "/x/%2e%2e/admin", "http://lab/admin", "/admin/./", "*",
        ];
        foreach (var t in targets)
        {
            var sw = Stopwatch.StartNew();
            await using var c = await RawConnection.ConnectAsync(host, port);
            await c.SendAsync($"GET {t} HTTP/1.1\r\nHost: lab\r\nConnection: close\r\n\r\n");
            var r = await c.ReadUntilIdleAsync(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(5));
            var leaked = r.Text.Contains("ADMIN-SECRET", StringComparison.Ordinal);
            Add(report, $"auth.rawpath.{Slug(t)}", $"raw GET {t}", leaked ? Verdict.Flag : Verdict.Pass,
                $"status=[{string.Join(",", r.StatusCodes)}]", "never the admin body", sw.Elapsed.TotalMilliseconds, "CWE-863");
        }
    }

    // ------------------------------------------------------------------ JWT bearer

    private async Task Jwt(ProbeReport report, HttpClient client)
    {
        var token = await (await client.PostAsync("/api/token?user=alice", null)).Content.ReadAsStringAsync();
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            Add(report, "jwt.issue", "Lab token issued", Verdict.Error, token, "header.payload.signature", 0);
            return;
        }
        string B64(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var header = Encoding.UTF8.GetString(FromB64Url(parts[0]));
        var payload = Encoding.UTF8.GetString(FromB64Url(parts[1]));
        var adminPayload = payload.Replace("\"role\":\"user\"", "\"role\":\"admin\"");

        async Task Check(string id, string title, string? authHeader, string path, bool shouldSucceed, string reference = "CWE-347")
        {
            var sw = Stopwatch.StartNew();
            using var req = new HttpRequestMessage(HttpMethod.Get, path);
            if (authHeader is not null) req.Headers.TryAddWithoutValidation("Authorization", authHeader);
            HttpResponseMessage resp;
            try
            {
                resp = await client.SendAsync(req);
            }
            catch (HttpRequestException ex)
            {
                // Kestrel refusing the request at the transport level (e.g. 431 + close) is a rejection.
                Add(report, id, title, shouldSucceed ? Verdict.Error : Verdict.Pass, $"transport rejected: {ex.InnerException?.Message ?? ex.Message}",
                    shouldSucceed ? "200" : "401/403", sw.Elapsed.TotalMilliseconds, reference);
                return;
            }
            using var _ = resp;
            var body = await resp.Content.ReadAsStringAsync();
            var ok = resp.StatusCode == HttpStatusCode.OK;
            var v = ok == shouldSucceed ? Verdict.Pass : ok ? Verdict.Flag : Verdict.Review;
            Add(report, id, title, v, $"{(int)resp.StatusCode} {Trim(body)}", shouldSucceed ? "200" : "401/403", sw.Elapsed.TotalMilliseconds, reference);
        }

        await Check("jwt.baseline", "Valid user token on /api/me", $"Bearer {token}", "/api/me", true);
        await Check("jwt.baseline-admin-denied", "Valid user token denied on /api/admin", $"Bearer {token}", "/api/admin", false);
        await Check("jwt.no-token", "No token", null, "/api/me", false);
        await Check("jwt.alg-none", "alg=none, signature stripped",
            $"Bearer {B64("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{B64(adminPayload)}.", "/api/admin", false);
        await Check("jwt.alg-none-case", "alg=NoNe",
            $"Bearer {B64("{\"alg\":\"NoNe\",\"typ\":\"JWT\"}")}.{B64(adminPayload)}.", "/api/admin", false);
        await Check("jwt.role-tamper", "role=admin with original signature",
            $"Bearer {parts[0]}.{B64(adminPayload)}.{parts[2]}", "/api/admin", false);
        await Check("jwt.dup-claim", "Duplicate role claim appended (re-signed? no - original sig)",
            $"Bearer {parts[0]}.{B64(payload.TrimEnd('}') + ",\"role\":\"admin\"}")}.{parts[2]}", "/api/admin", false);
        await Check("jwt.sig-truncated", "Signature truncated by one char",
            $"Bearer {parts[0]}.{parts[1]}.{parts[2][..^1]}", "/api/me", false);
        await Check("jwt.sig-empty", "Empty signature, HS256 header",
            $"Bearer {parts[0]}.{parts[1]}.", "/api/me", false);
        await Check("jwt.kid-inject", "kid pointing elsewhere, original sig",
            $"Bearer {B64(header.Replace("\"kid\":\"lab-key-1\"", "\"kid\":\"../../dev/null\""))}.{parts[1]}.{parts[2]}", "/api/me", false);
        await Check("jwt.jwk-embedded", "Embedded jwk header (key confusion)",
            $"Bearer {B64("{\"alg\":\"HS256\",\"typ\":\"JWT\",\"jwk\":{\"kty\":\"oct\",\"k\":\"AAAA\"}}")}.{B64(adminPayload)}.AAAA", "/api/admin", false);
        await Check("jwt.crit", "Unknown crit header with valid sig",
            $"Bearer {B64(header.TrimEnd('}') + ",\"crit\":[\"x-lab\"],\"x-lab\":1}")}.{parts[1]}.{parts[2]}", "/api/me", false);
        await Check("jwt.jwe-shape", "Five-part JWE-shaped token",
            $"Bearer {parts[0]}.{parts[1]}.{parts[2]}.AAAA.AAAA", "/api/me", false);
        await Check("jwt.scheme-case", "lowercase 'bearer' scheme (valid token)", $"bearer {token}", "/api/me", true, "RFC9110-11.1");
        await Check("jwt.double-header", "Two tokens in one header", $"Bearer {token}, Bearer {token}", "/api/me", false);
        await Check("jwt.huge", "1 MiB token (DoS guard)", $"Bearer {new string('A', 1 << 20)}.x.y", "/api/me", false, "CVE-2024-21319-class");
    }

    // ------------------------------------------------------------------ antiforgery

    private async Task Antiforgery(ProbeReport report)
    {
        using var a = NewClient(out _);
        using var b = NewClient(out _);
        var tokA = System.Text.Json.JsonDocument.Parse(await a.GetStringAsync("/form")).RootElement;
        var tokB = System.Text.Json.JsonDocument.Parse(await b.GetStringAsync("/form")).RootElement;
        var field = tokA.GetProperty("field").GetString()!;

        async Task Post(string id, string title, HttpClient c, HttpContent content, bool shouldAccept, Action<HttpRequestMessage>? tweak = null)
        {
            var sw = Stopwatch.StartNew();
            using var req = new HttpRequestMessage(HttpMethod.Post, "/form") { Content = content };
            tweak?.Invoke(req);
            using var resp = await c.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            var accepted = body.StartsWith("ACCEPTED:", StringComparison.Ordinal);
            var v = accepted == shouldAccept ? Verdict.Pass : accepted ? Verdict.Flag : Verdict.Review;
            Add(report, id, title, v, $"{(int)resp.StatusCode} {Trim(body)}", shouldAccept ? "accepted" : "rejected (400)",
                sw.Elapsed.TotalMilliseconds, "CWE-352");
        }

        FormUrlEncodedContent Form(string? token) => new(token is null
            ? [new("comment", "x")]
            : [new("comment", "x"), new(field, token)]);

        await Post("csrf.valid", "Own session token", a, Form(tokA.GetProperty("token").GetString()), true);
        await Post("csrf.missing", "No token", a, Form(null), false);
        await Post("csrf.cross-session", "Token from another session", a, Form(tokB.GetProperty("token").GetString()), false);
        await Post("csrf.garbage", "Garbage token", a, Form("CfDJ8AAAA"), false);
        await Post("csrf.multipart-no-token", "multipart/form-data without token", a,
            new MultipartFormDataContent { { new StringContent("x"), "comment" } }, false);
        await Post("csrf.text-plain", "text/plain body shaped like a form", a,
            new StringContent("comment=x", Encoding.UTF8, "text/plain"), false);
        await Post("csrf.ct-params", "Content-Type with odd parameters", a,
            new StringContent("comment=x", Encoding.UTF8, "application/x-www-form-urlencoded"), false,
            r => r.Content!.Headers.ContentType!.Parameters.Add(new("boundary", "x")));
    }

    // ------------------------------------------------------------------ static files

    private static async Task StaticFiles(ProbeReport report, HttpClient client)
    {
        string[] paths =
        [
            "/index.html", "/%69ndex.html", "/INDEX.HTML", "/index.html%00", "/..%2fProgram.cs", "/%2e%2e/Program.cs",
            "/..%5cProgram.cs", "/%2e%2e%2fappsettings.json", "/../appsettings.json", "/wwwroot/../appsettings.json",
            "/index.html::$DATA", "/index.html.", "/index.html%20",
        ];
        foreach (var p in paths)
        {
            var sw = Stopwatch.StartNew();
            using var resp = await client.GetAsync(p);
            var body = await resp.Content.ReadAsStringAsync();
            var leak = body.Contains("LAB-APPSETTINGS-MARKER") || body.Contains("Lab.Target - a deliberately");
            var v = leak ? Verdict.Flag : Verdict.Pass;
            Add(report, $"static.{Slug(p)}", $"GET {p}", v, $"{(int)resp.StatusCode} len={body.Length}",
                "only files under wwwroot are served", sw.Elapsed.TotalMilliseconds, "CWE-22");
        }
    }

    // ------------------------------------------------------------------ util

    private static byte[] FromB64Url(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }

    private static string Slug(string s) =>
        new string(s.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_').ToArray()).Trim('_') is { Length: > 0 } x ? x : "root";

    private static string Trim(string s) => s.Length > 80 ? s[..80] + "…" : s;
}
