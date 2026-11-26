using System.Buffers.Text;
using System.Formats.Asn1;
using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using NetHeaders = System.Net.Http.Headers;
using AspHeaders = Microsoft.Net.Http.Headers;

namespace Lab.Fuzz;

/// <summary>
/// Fuzz targets over in-scope parsers. Every target names the VMR source directory of the code
/// under test so a finding can be traced to source in src/vmr/net10.0 and src/vmr/net11.0.
/// </summary>
public static class Targets
{
    private const string Rt = "src/runtime/src/libraries";
    private const string Asp = "src/aspnetcore/src";

    private static byte[] B(string s) => Encoding.UTF8.GetBytes(s);
    private static string S(byte[] b) => Encoding.UTF8.GetString(b);

    public static readonly IReadOnlyList<FuzzTarget> All =
    [
        // ------------------------------------------------------------------ System.Text.Json
        new("json-document", "JsonDocument parse + WriteTo round-trip idempotence",
            $"{Rt}/System.Text.Json/src/System/Text/Json/Document", ["System.Text.Json.dll"],
            [typeof(JsonException), typeof(ArgumentException), typeof(InvalidOperationException)], // IOE: documented for lone surrogates / invalid UTF-8 on read-as-string
            [B("{\"a\":[1,2.5e10,true,null,\"\\u00e9\"],\"b\":{\"c\":\"d\"}}"), B("[[[[[]]]]]"), B("\"\\ud800\""), B("1e400")],
            ["{", "}", "[", "]", "\"", ":", ",", "\\u", "\\ud83d\\ude00", "1e308", "-0", "null", "true", "/*", "*/", "\uFEFF"],
            data =>
            {
                var opts = new JsonDocumentOptions { MaxDepth = 64, AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
                using var doc = JsonDocument.Parse(data, opts);
                var once = Serialize(doc.RootElement);
                using var again = JsonDocument.Parse(once);
                var twice = Serialize(again.RootElement);
                if (!once.AsSpan().SequenceEqual(twice))
                    throw new InvariantViolation("JsonDocument round-trip not idempotent");
            }),

        new("json-serializer", "JsonSerializer polymorphic + dictionary + extension-data deserialization",
            $"{Rt}/System.Text.Json/src/System/Text/Json/Serialization", ["System.Text.Json.dll"],
            [typeof(JsonException), typeof(NotSupportedException), typeof(InvalidOperationException), typeof(ArgumentException)],
            [B("{\"$type\":\"circle\",\"r\":1,\"tags\":{\"a\":1},\"x\":[1]}"), B("{\"$type\":\"square\",\"side\":2,\"extra\":{\"k\":\"v\"}}")],
            ["\"$type\"", "\"$id\"", "\"$ref\"", "\"$values\"", "\"circle\"", "\"square\"", "{", "}", "[", "]", "1e999"],
            data =>
            {
                var opts = new JsonSerializerOptions { MaxDepth = 64, ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve };
                var shape = JsonSerializer.Deserialize<Shape>(data, opts);
                if (shape is not null)
                {
                    var json = JsonSerializer.SerializeToUtf8Bytes(shape, opts);
                    JsonSerializer.Deserialize<Shape>(json, opts);
                }
                JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data);
            }),

        new("utf8-json-reader", "Utf8JsonReader segmented vs contiguous differential",
            $"{Rt}/System.Text.Json/src/System/Text/Json/Reader", ["System.Text.Json.dll"],
            [typeof(JsonException), typeof(InvalidOperationException)], // IOE: GetString on invalid UTF-8/UTF-16 (documented)
            [B("{\"a\":\"bcdefghij\",\"k\":[1,2,3]}"), B("[\"\\u0041\\u0042\"]")],
            ["\"", "\\", "\\u", "{", "}", "[", "]", ",", ":", " "],
            data =>
            {
                var a = ReadTokens(new Utf8JsonReader(data, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip }));
                // Same bytes split into 1..7-byte segments must produce identical tokens.
                var seq = Segments.Split(data, 1 + data.Length % 7);
                var b = ReadTokens(new Utf8JsonReader(seq, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip }));
                if (a != b) throw new InvariantViolation($"segmented reader differs: {Trunc(a)} vs {Trunc(b)}");
            }),

        // ------------------------------------------------------------------ System.Private.Xml
        new("xml-reader", "XmlReader with default (secure) settings; DTDs must be refused",
            $"{Rt}/System.Private.Xml/src/System/Xml/Core", ["System.Private.Xml.dll"],
            [typeof(XmlException)],
            [B("<?xml version=\"1.0\"?><a b=\"c\"><d>&amp;</d><![CDATA[x]]></a>"), B("<!DOCTYPE a [<!ENTITY e \"x\">]><a>&e;</a>")],
            ["<", ">", "</", "<!DOCTYPE", "<!ENTITY", "&", ";", "<![CDATA[", "]]>", "xmlns:", "<?", "?>", "&#x10FFFF;"],
            data =>
            {
                using var r = XmlReader.Create(new MemoryStream(data), new XmlReaderSettings { MaxCharactersInDocument = 1 << 20 });
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.DocumentType)
                        throw new InvariantViolation("DocumentType node surfaced although DtdProcessing defaults to Prohibit");
                    if (r.HasAttributes) while (r.MoveToNextAttribute()) _ = r.Value;
                }
            }),

        new("datacontract", "DataContractSerializer.ReadObject on a known graph",
            $"{Rt}/System.Private.DataContractSerialization", ["System.Private.DataContractSerialization.dll"],
            [typeof(SerializationException), typeof(XmlException), typeof(InvalidDataContractException)],
            [B("<Order xmlns=\"http://lab\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Id>1</Id><Lines><string>a</string></Lines></Order>")],
            ["<", ">", "i:type=", "z:Id=", "z:Ref=", "xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"", "</Order>"],
            data =>
            {
                var ser = new DataContractSerializer(typeof(Order));
                using var r = XmlDictionaryReader.CreateTextReader(data, new XmlDictionaryReaderQuotas { MaxDepth = 32, MaxStringContentLength = 1 << 16 });
                ser.ReadObject(r);
            }),

        // ------------------------------------------------------------------ System.Private.Uri
        new("uri", "Uri.TryCreate: no throw, AbsoluteUri idempotent, host has no delimiters",
            $"{Rt}/System.Private.Uri/src/System", ["System.Private.Uri.dll"],
            [],
            [B("http://user:pw@host:8080/a/b?c=d#e"), B("http://[::ffff:127.0.0.1]/"), B("file:///etc/passwd"), B("http://xn--nxasmq6b.com/"), B("http://a\\b@c/")],
            ["%", "%2e", "%00", "@", ":", "[", "]", "\\", "/", "?", "#", "xn--", "\u3002", "\uFF0E", "..", "%u002e"],
            data =>
            {
                var s = S(data);
                if (!Uri.TryCreate(s, UriKind.Absolute, out var u)) return;
                var abs = u.AbsoluteUri;
                if (!Uri.TryCreate(abs, UriKind.Absolute, out var u2))
                    throw new InvariantViolation($"AbsoluteUri not reparseable: {Trunc(abs)}");
                if (u2.AbsoluteUri != abs)
                    throw new InvariantViolation($"AbsoluteUri not idempotent: {Trunc(abs)} -> {Trunc(u2.AbsoluteUri)}");
                if (u.HostNameType is UriHostNameType.Dns or UriHostNameType.Basic &&
                    u.Host.IndexOfAny(['/', '\\', '@', '?', '#']) >= 0)
                    throw new InvariantViolation($"host contains delimiter: {Trunc(u.Host)}");
                _ = u.IdnHost; _ = u.Segments; _ = u.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
            }),

        // ------------------------------------------------------------------ System.Text.RegularExpressions
        new("regex", "Pattern parsing + backtracking vs NonBacktracking engine differential",
            $"{Rt}/System.Text.RegularExpressions/src", ["System.Text.RegularExpressions.dll"],
            [typeof(ArgumentException), typeof(RegexMatchTimeoutException), typeof(NotSupportedException)],
            [B("^(a|b)*c$"), B("[a-z]+@[a-z]+\\.com"), B("(?i)k"), B("\\p{L}+\\d{2,3}")],
            ["(", ")", "*", "+", "?", "{2,}", "[", "]", "^", "$", "|", "\\b", "\\w", "(?i)", "\\p{L}", "[^", "\\d", "."],
            data =>
            {
                var pattern = S(data);
                if (pattern.Length > 256) pattern = pattern[..256];
                var bt = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200));
                Regex nb;
                try { nb = new Regex(pattern, RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(200)); }
                catch (NotSupportedException) { return; } // backreferences / lookarounds
                foreach (var subject in RegexSubjects)
                {
                    bool a, b;
                    try { a = bt.IsMatch(subject); b = nb.IsMatch(subject); }
                    catch (RegexMatchTimeoutException) { return; }
                    if (a != b) throw new InvariantViolation($"engine differential on /{Trunc(pattern)}/ subject '{subject}': backtracking={a} nonbacktracking={b}");
                }
            }, SlowMs: 3000),

        // ------------------------------------------------------------------ System.Formats.Asn1
        new("asn1-der", "AsnReader recursive DER walk",
            $"{Rt}/System.Formats.Asn1/src/System/Formats/Asn1", ["System.Formats.Asn1.dll"],
            [typeof(AsnContentException), typeof(ArgumentException)],
            [[0x30, 0x0A, 0x02, 0x01, 0x05, 0x04, 0x03, 0x61, 0x62, 0x63, 0x05, 0x00], [0x06, 0x03, 0x2A, 0x86, 0x48], [0x17, 0x0D, .. B("250101000000Z")]],
            [],
            data => WalkAsn1(new AsnReader(data, AsnEncodingRules.DER), 0)),

        // ------------------------------------------------------------------ System.Formats.Tar / ZIP
        new("tar", "TarReader entry walk + extraction containment",
            $"{Rt}/System.Formats.Tar/src/System/Formats/Tar", ["System.Formats.Tar.dll"],
            [typeof(InvalidDataException), typeof(FormatException), typeof(EndOfStreamException), typeof(ArgumentException), typeof(IOException), typeof(UnauthorizedAccessException), typeof(InvalidOperationException)],
            [Archives.Tar()],
            ["../", "/etc/", "\0", "ustar", "././@LongLink"],
            data =>
            {
                using (var r = new TarReader(new MemoryStream(data)))
                {
                    TarEntry? e;
                    var n = 0;
                    while ((e = r.GetNextEntry()) is not null && n++ < 64) e.DataStream?.CopyTo(Stream.Null);
                }
                Archives.AssertContained(dest => TarFile.ExtractToDirectory(new MemoryStream(data), dest, overwriteFiles: false));
            }),

        new("zip", "ZipArchive read + ZipFile extraction containment",
            $"{Rt}/System.IO.Compression/src/System/IO/Compression", ["System.IO.Compression.dll", "System.IO.Compression.ZipFile.dll"],
            [typeof(InvalidDataException), typeof(NotSupportedException), typeof(ArgumentException), typeof(IOException), typeof(UnauthorizedAccessException)],
            [Archives.Zip()],
            ["PK\u0003\u0004", "PK\u0001\u0002", "PK\u0005\u0006", "../", "..\\"],
            data =>
            {
                using (var z = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read))
                    foreach (var e in z.Entries.Take(64)) { using var s = e.Open(); CopyCapped(s, 1 << 22); }
                Archives.AssertContained(dest =>
                {
                    using var z = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);
                    z.ExtractToDirectory(dest);
                });
            }),

        new("decompress", "GZip/Deflate/ZLib/Brotli decompression with output cap",
            $"{Rt}/System.IO.Compression/src/System/IO/Compression", ["System.IO.Compression.dll", "System.IO.Compression.Brotli.dll"],
            [typeof(InvalidDataException), typeof(InvalidOperationException)],
            [Archives.Gzip("hello hello hello"), Archives.Brotli("hello hello hello")],
            [],
            data =>
            {
                foreach (var make in new Func<Stream, Stream>[]
                         {
                             s => new GZipStream(s, CompressionMode.Decompress), s => new DeflateStream(s, CompressionMode.Decompress),
                             s => new ZLibStream(s, CompressionMode.Decompress), s => new BrotliStream(s, CompressionMode.Decompress),
                         })
                {
                    try { using var d = make(new MemoryStream(data)); CopyCapped(d, 1 << 24); }
                    catch (InvalidDataException) { }
                }
            }),

        // ------------------------------------------------------------------ System.Text.Encodings.Web (XSS)
        new("web-encoders", "HtmlEncoder / JavaScriptEncoder / UrlEncoder output safety invariants",
            $"{Rt}/System.Text.Encodings.Web/src", ["System.Text.Encodings.Web.dll"],
            [],
            [B("<script>alert('x')</script>"), B("\"><img src=x onerror=alert(1)>"), B("\ud83d\ude00 \u2028 \u0000")],
            ["<", ">", "\"", "'", "&", "\\", "`", "\u2028", "\u2029", "\ud800", "\udfff", "</script", "\u0000"],
            data =>
            {
                var s = S(data);
                var html = HtmlEncoder.Default.Encode(s);
                if (html.IndexOfAny(['<', '>', '"', '\'']) >= 0) throw new InvariantViolation($"HtmlEncoder leaked a metachar: {Trunc(html)}");
                for (var i = html.IndexOf('&'); i >= 0; i = html.IndexOf('&', i + 1))
                    if (!System.Text.RegularExpressions.Regex.IsMatch(html.AsSpan(i), "^&(#x?[0-9A-Fa-f]+|[a-z]+);"))
                        throw new InvariantViolation($"HtmlEncoder emitted bare '&': {Trunc(html)}");
                var js = JavaScriptEncoder.Default.Encode(s);
                if (js.IndexOfAny(['<', '>', '"', '\'', '&', '`', '\u2028', '\u2029']) >= 0) throw new InvariantViolation($"JavaScriptEncoder leaked a metachar: {Trunc(js)}");
                // UrlEncoder documents RFC 3986 sub-delims (!$()*,;@) as safe; what must never appear raw
                // are characters that end a URL component or an HTML attribute.
                var url = UrlEncoder.Default.Encode(s);
                for (var i = 0; i < url.Length; i++)
                {
                    var ch = url[i];
                    if (ch is '"' or '\'' or '<' or '>' or '&' or '?' or '#' or '/' or '\\' or ' ' or '+' or '=' or ':' or '`' || ch < 0x20 || ch > 0x7E)
                        throw new InvariantViolation($"UrlEncoder emitted unsafe char U+{(int)ch:X4}");
                    if (ch == '%' && (i + 2 >= url.Length || !char.IsAsciiHexDigit(url[i + 1]) || !char.IsAsciiHexDigit(url[i + 2])))
                        throw new InvariantViolation("UrlEncoder emitted bare '%'");
                }
            }),

        // ------------------------------------------------------------------ text / numbers / addresses
        new("utf8-chunked", "UTF-8 Decoder: chunked decode equals one-shot decode",
            $"{Rt}/System.Private.CoreLib/src/System/Text", [],
            [],
            [B("h\u00e9llo \ud83d\ude00 \u4e2d"), [0xF0, 0x9F, 0x98], [0xED, 0xA0, 0x80], [0xC0, 0xAF]],
            [],
            data =>
            {
                var oneShot = Encoding.UTF8.GetString(data);
                var dec = Encoding.UTF8.GetDecoder();
                var sb = new StringBuilder();
                var step = 1 + (data.Length == 0 ? 0 : data[0] % 5);
                var chars = new char[data.Length + 8];
                for (var i = 0; i < data.Length; i += step)
                {
                    var len = Math.Min(step, data.Length - i);
                    var n = dec.GetChars(data, i, len, chars, 0, flush: i + len >= data.Length);
                    sb.Append(chars, 0, n);
                }
                if (data.Length == 0) return;
                if (sb.ToString() != oneShot) throw new InvariantViolation($"chunked UTF-8 decode differs (step {step})");
            }),

        new("ipaddress", "IPAddress / IPNetwork parse -> format -> reparse stability",
            $"{Rt}/System.Net.Primitives/src/System/Net", ["System.Net.Primitives.dll"],
            [],
            [B("127.0.0.1"), B("::ffff:10.0.0.1"), B("fe80::1%eth0"), B("0x7f.1"), B("10.0.0.0/8"), B("2001:db8::/32")],
            [".", ":", "::", "%", "/", "0x", "ffff", "[", "]"],
            data =>
            {
                var s = S(data);
                if (IPAddress.TryParse(s, out var ip))
                {
                    var f = ip.ToString();
                    if (!IPAddress.TryParse(f, out var ip2) || !ip.Equals(ip2) || ip2.ToString() != f)
                        throw new InvariantViolation($"IPAddress round-trip unstable: {Trunc(s)} -> {f}");
                }
                if (IPNetwork.TryParse(s, out var net))
                {
                    var f = net.ToString();
                    if (!IPNetwork.TryParse(f, out var net2) || !net.Equals(net2))
                        throw new InvariantViolation($"IPNetwork round-trip unstable: {Trunc(s)} -> {f}");
                }
            }),

        new("bigint-parse", "BigInteger.Parse: algorithmic complexity guard",
            $"{Rt}/System.Runtime.Numerics/src/System/Numerics", ["System.Runtime.Numerics.dll"],
            [typeof(FormatException), typeof(OverflowException)],
            [B("123456789012345678901234567890"), B("-0"), B("1e10")],
            ["0", "9", "-", "+", "e", " "],
            data =>
            {
                var s = S(data);
                if (BigInteger.TryParse(s, out var v)) _ = v.ToString();
                _ = BigInteger.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out _);
            }, SlowMs: 1000),

        new("base64", "Convert vs Base64(UTF-8) decoder differential (no whitespace inputs)",
            $"{Rt}/System.Private.CoreLib/src/System/Buffers/Text", [],
            [],
            [B("aGVsbG8="), B("YQ=="), B("////"), B("A===")],
            ["=", "==", "+", "/", "-", "_"],
            data =>
            {
                if (data.Any(b => b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')) return;
                var s = Encoding.Latin1.GetString(data);
                var buf = new byte[data.Length];
                var a = Convert.TryFromBase64String(s, buf, out var na);
                var buf2 = new byte[data.Length];
                var st = Base64.DecodeFromUtf8(data, buf2, out _, out var nb);
                var b = st == System.Buffers.OperationStatus.Done;
                // Convert tolerates non-canonical trailing bits ("A+==") where Base64 is strict: documented.
                // Real disagreements: strict accepts what lenient rejects, or both accept with different bytes.
                if ((b && !a) || (a && b && !buf.AsSpan(0, na).SequenceEqual(buf2.AsSpan(0, nb))))
                    throw new InvariantViolation($"base64 differential: Convert={a} Base64={st}");
            }),

        new("net-http-headers", "System.Net.Http.Headers parse -> ToString -> reparse stability",
            $"{Rt}/System.Net.Http/src/System/Net/Http/Headers", ["System.Net.Http.dll"],
            [],
            [B("text/html; charset=utf-8"), B("Basic dXNlcjpwYXNz"), B("attachment; filename=\"a.txt\"; filename*=UTF-8''%e2%82%ac")],
            [";", "=", "\"", "\\", ",", "*=", "UTF-8''", "%"],
            data =>
            {
                var s = S(data);
                RoundTrip<NetHeaders.MediaTypeHeaderValue>(s, (string? i, out NetHeaders.MediaTypeHeaderValue? v) => NetHeaders.MediaTypeHeaderValue.TryParse(i, out v));
                RoundTrip<NetHeaders.AuthenticationHeaderValue>(s, (string? i, out NetHeaders.AuthenticationHeaderValue? v) => NetHeaders.AuthenticationHeaderValue.TryParse(i, out v));
                RoundTrip<NetHeaders.ContentDispositionHeaderValue>(s, (string? i, out NetHeaders.ContentDispositionHeaderValue? v) => NetHeaders.ContentDispositionHeaderValue.TryParse(i, out v));
                RoundTrip<NetHeaders.CacheControlHeaderValue>(s, (string? i, out NetHeaders.CacheControlHeaderValue? v) => NetHeaders.CacheControlHeaderValue.TryParse(i, out v));
            }),

        // ------------------------------------------------------------------ ASP.NET Core parsers
        new("aspnet-headers", "Microsoft.Net.Http.Headers (Set-Cookie, Content-Disposition, media types) stability",
            $"{Asp}/Http/Headers/src", ["Microsoft.Net.Http.Headers.dll"],
            [],
            [B("id=a3fWa; Expires=Wed, 21 Oct 2015 07:28:00 GMT; Secure; HttpOnly; SameSite=Lax"), B("form-data; name=\"f\"; filename=\"x.png\"")],
            [";", "=", "\"", "\\", "SameSite=", "Path=", "Domain=", "filename*=", "%"],
            data =>
            {
                var s = S(data);
                if (AspHeaders.SetCookieHeaderValue.TryParse(s, out var c))
                {
                    var f = c.ToString();
                    if (!AspHeaders.SetCookieHeaderValue.TryParse(f, out var c2) || c2.ToString() != f)
                        throw new InvariantViolation($"Set-Cookie round-trip unstable: {Trunc(f)}");
                }
                if (AspHeaders.ContentDispositionHeaderValue.TryParse(s, out var cd))
                {
                    var f = cd.ToString();
                    if (!AspHeaders.ContentDispositionHeaderValue.TryParse(f, out var cd2) || cd2.ToString() != f)
                        throw new InvariantViolation($"Content-Disposition round-trip unstable: {Trunc(f)}");
                    _ = AspHeaders.HeaderUtilities.RemoveQuotes(cd.FileName);
                }
                AspHeaders.MediaTypeHeaderValue.TryParseList([s], out _);
            }),

        new("aspnet-form", "FormReader (application/x-www-form-urlencoded) with default limits",
            $"{Asp}/Http/WebUtilities/src", ["Microsoft.AspNetCore.WebUtilities.dll"],
            [typeof(InvalidDataException)],
            [B("a=1&b=%20x&c=%E2%82%AC&a=2"), B("=&&==&%zz")],
            ["&", "=", "%", "%2", "+", "%00", "%u0041"],
            data =>
            {
                using var r = new FormReader(new MemoryStream(data), Encoding.UTF8);
                var form = r.ReadFormAsync().GetAwaiter().GetResult();
                foreach (var kv in form) _ = kv.Value.ToString();
            }),

        new("aspnet-multipart", "MultipartReader sections + Content-Disposition with default limits",
            $"{Asp}/Http/WebUtilities/src", ["Microsoft.AspNetCore.WebUtilities.dll", "Microsoft.Net.Http.Headers.dll"],
            [typeof(InvalidDataException), typeof(IOException)],
            [B("--b\r\nContent-Disposition: form-data; name=\"f\"; filename=\"a.txt\"\r\nContent-Type: text/plain\r\n\r\nhello\r\n--b--\r\n")],
            ["--b", "\r\n", "--b--", "Content-Disposition: ", "form-data; name=", "filename*=UTF-8''", "Content-Type: "],
            data =>
            {
                var reader = new MultipartReader("b", new MemoryStream(data)) { HeadersCountLimit = 16, HeadersLengthLimit = 16 * 1024, BodyLengthLimit = 1 << 20 };
                var n = 0;
                while (reader.ReadNextSectionAsync().GetAwaiter().GetResult() is { } section && n++ < 32)
                {
                    if (section.Headers is { } h && h.TryGetValue("Content-Disposition", out var cd))
                        AspHeaders.ContentDispositionHeaderValue.TryParse(cd.ToString(), out _);
                    section.Body.CopyTo(Stream.Null);
                }
            }),

        new("query-string", "QueryHelpers.ParseQuery / QueryString round-trip",
            $"{Asp}/Http/WebUtilities/src", ["Microsoft.AspNetCore.WebUtilities.dll"],
            [],
            [B("?a=1&b=2&a=3"), B("?%=%&&")],
            ["?", "&", "=", "%", "+", "#"],
            data =>
            {
                var q = QueryHelpers.ParseQuery(S(data));
                var rebuilt = QueryHelpers.AddQueryString("", q.SelectMany(kv => kv.Value.Select(v => new KeyValuePair<string, string?>(kv.Key, v))));
                var q2 = QueryHelpers.ParseQuery(rebuilt);
                if (q.Count != q2.Count) throw new InvariantViolation($"query round-trip changed key count {q.Count}->{q2.Count}");
                foreach (var (k, v) in q)
                    if (!q2.TryGetValue(k, out var v2) || !StringValues.Equals(v, v2))
                        throw new InvariantViolation($"query round-trip changed '{Trunc(k)}'");
            }),
    ];

    // ------------------------------------------------------------------ helpers

    private static readonly string[] RegexSubjects =
        ["", "a", "abc", "aaaaaaaaaaaaaaaaaaaab", "user@example.com", "K", "\u212A", "İ", "ı", "abc\nxyz", "12", "x1y22z333"];

    private static byte[] Serialize(JsonElement e)
    {
        var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { SkipValidation = false })) e.WriteTo(w);
        return ms.ToArray();
    }

    private static string ReadTokens(Utf8JsonReader r)
    {
        var sb = new StringBuilder();
        try
        {
            while (r.Read())
            {
                sb.Append((int)r.TokenType).Append(':');
                if (r.TokenType is JsonTokenType.String or JsonTokenType.PropertyName) sb.Append(r.GetString());
                else if (r.TokenType == JsonTokenType.Number) sb.Append(Encoding.UTF8.GetString(r.HasValueSequence ? System.Buffers.BuffersExtensions.ToArray(r.ValueSequence) : r.ValueSpan));
                sb.Append('|');
            }
        }
        catch (JsonException)
        {
            // Error *position* may legitimately differ by segment boundary; only token stream + failure matter.
            sb.Append("ERR");
        }
        catch (InvalidOperationException)
        {
            sb.Append("ERR-STRING");
        }
        return sb.ToString();
    }

    private static void WalkAsn1(AsnReader r, int depth)
    {
        if (depth > 32) return;
        while (r.HasData)
        {
            var tag = r.PeekTag();
            if (tag.IsConstructed)
            {
                var inner = tag.TagClass == TagClass.Universal && tag.TagValue == (int)UniversalTagNumber.SetOf
                    ? r.ReadSetOf(skipSortOrderValidation: false)
                    : r.ReadSequence(tag);
                WalkAsn1(inner, depth + 1);
                continue;
            }
            if (tag.TagClass != TagClass.Universal) { r.ReadEncodedValue(); continue; }
            switch ((UniversalTagNumber)tag.TagValue)
            {
                case UniversalTagNumber.Integer: _ = r.ReadInteger(); break;
                case UniversalTagNumber.ObjectIdentifier: _ = r.ReadObjectIdentifier(); break;
                case UniversalTagNumber.BitString: _ = r.ReadBitString(out _); break;
                case UniversalTagNumber.OctetString: _ = r.ReadOctetString(); break;
                case UniversalTagNumber.Boolean: _ = r.ReadBoolean(); break;
                case UniversalTagNumber.UtcTime: _ = r.ReadUtcTime(); break;
                case UniversalTagNumber.GeneralizedTime: _ = r.ReadGeneralizedTime(); break;
                case UniversalTagNumber.UTF8String: _ = r.ReadCharacterString(UniversalTagNumber.UTF8String); break;
                case UniversalTagNumber.PrintableString: _ = r.ReadCharacterString(UniversalTagNumber.PrintableString); break;
                case UniversalTagNumber.IA5String: _ = r.ReadCharacterString(UniversalTagNumber.IA5String); break;
                case UniversalTagNumber.BMPString: _ = r.ReadCharacterString(UniversalTagNumber.BMPString); break;
                case UniversalTagNumber.Null: r.ReadNull(); break;
                case UniversalTagNumber.Enumerated: _ = r.ReadEnumeratedBytes(); break;
                default: r.ReadEncodedValue(); break;
            }
        }
    }

    private delegate bool TryParse<T>(string? input, out T? value);

    private static void RoundTrip<T>(string s, TryParse<T> parse) where T : class
    {
        if (!parse(s, out var v) || v is null) return;
        var f = v.ToString()!;
        if (!parse(f, out var v2) || v2 is null) throw new InvariantViolation($"{typeof(T).Name}: formatted value not reparseable: {Trunc(f)}");
        if (v2.ToString() != f) throw new InvariantViolation($"{typeof(T).Name}: round-trip not idempotent: {Trunc(f)} -> {Trunc(v2.ToString()!)}");
    }

    private static void CopyCapped(Stream s, long cap)
    {
        var buf = new byte[81920];
        long total = 0;
        int n;
        while ((n = s.Read(buf)) > 0)
            if ((total += n) > cap) return;
    }

    public static string Trunc(string s) => s.Length > 120 ? s[..120] + "…" : s;

    // ------------------------------------------------------------------ models

    [System.Text.Json.Serialization.JsonPolymorphic]
    [System.Text.Json.Serialization.JsonDerivedType(typeof(Circle), "circle")]
    [System.Text.Json.Serialization.JsonDerivedType(typeof(Square), "square")]
    public abstract class Shape
    {
        public Dictionary<string, int>? Tags { get; set; }
        [System.Text.Json.Serialization.JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
    }
    public sealed class Circle : Shape { public double R { get; set; } }
    public sealed class Square : Shape { public double Side { get; set; } public List<Shape>? Children { get; set; } }

    [DataContract(Namespace = "http://lab")]
    public sealed class Order
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public List<string>? Lines { get; set; }
        [DataMember] public Dictionary<string, int>? Qty { get; set; }
    }
}
