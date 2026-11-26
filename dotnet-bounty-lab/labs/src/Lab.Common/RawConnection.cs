using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace Lab.Common;

/// <summary>
/// A byte-exact TCP connection. HttpClient normalises requests; parser bugs live in the
/// bytes it refuses to send, so the probes talk to the socket directly.
/// </summary>
public sealed class RawConnection : IAsyncDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;

    private RawConnection(TcpClient client)
    {
        _client = client;
        _client.NoDelay = true;
        _stream = client.GetStream();
    }

    public NetworkStream Stream => _stream;

    public static async Task<RawConnection> ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        var client = new TcpClient();
        await client.ConnectAsync(host, port, ct);
        return new RawConnection(client);
    }

    public Task SendAsync(string ascii, CancellationToken ct = default) =>
        SendAsync(Encoding.Latin1.GetBytes(ascii), ct);

    public async Task SendAsync(ReadOnlyMemory<byte> bytes, CancellationToken ct = default)
    {
        await _stream.WriteAsync(bytes, ct);
        await _stream.FlushAsync(ct);
    }

    /// <summary>Reads until the peer closes or goes quiet for <paramref name="idle"/>.</summary>
    public async Task<ReadResult> ReadUntilIdleAsync(TimeSpan idle, TimeSpan max, CancellationToken ct = default)
    {
        var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        var sw = Stopwatch.StartNew();
        var closed = false;
        while (sw.Elapsed < max)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(idle);
            int n;
            try
            {
                n = await _stream.ReadAsync(chunk, cts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                break; // idle
            }
            catch (IOException)
            {
                closed = true; // reset
                break;
            }
            if (n == 0) { closed = true; break; }
            buffer.Write(chunk, 0, n);
        }
        return new ReadResult(buffer.ToArray(), closed, sw.Elapsed);
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
        _client.Dispose();
    }
}

public sealed record ReadResult(byte[] Bytes, bool PeerClosed, TimeSpan Elapsed)
{
    public string Text => Encoding.Latin1.GetString(Bytes);

    /// <summary>Status codes of every HTTP/1.x response found in the stream (pipelining/desync detection).</summary>
    public IReadOnlyList<int> StatusCodes
    {
        get
        {
            var list = new List<int>();
            var text = Text;
            var idx = 0;
            while ((idx = text.IndexOf("HTTP/1.", idx, StringComparison.Ordinal)) >= 0)
            {
                // Only count status lines at a line start.
                if (idx == 0 || text[idx - 1] == '\n')
                {
                    var sp = text.IndexOf(' ', idx);
                    if (sp > 0 && sp + 4 <= text.Length && int.TryParse(text.AsSpan(sp + 1, 3), out var code))
                        list.Add(code);
                }
                idx += 7;
            }
            return list;
        }
    }
}
