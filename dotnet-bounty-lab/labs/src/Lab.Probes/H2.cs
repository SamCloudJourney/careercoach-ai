using System.Buffers.Binary;
using System.Text;
using Lab.Common;

namespace Lab.Probes;

/// <summary>Minimal, byte-exact HTTP/2 client codec (RFC 9113) + HPACK literal encoder (RFC 7541).</summary>
internal static class H2
{
    public static readonly byte[] Preface = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"u8.ToArray();

    public enum FrameType : byte
    {
        Data = 0, Headers = 1, Priority = 2, RstStream = 3, Settings = 4, PushPromise = 5,
        Ping = 6, GoAway = 7, WindowUpdate = 8, Continuation = 9,
    }

    [Flags]
    public enum Flags : byte { None = 0, EndStream = 0x1, Ack = 0x1, EndHeaders = 0x4, Padded = 0x8, Priority = 0x20 }

    public static readonly string[] ErrorCodes =
    [
        "NO_ERROR", "PROTOCOL_ERROR", "INTERNAL_ERROR", "FLOW_CONTROL_ERROR", "SETTINGS_TIMEOUT", "STREAM_CLOSED",
        "FRAME_SIZE_ERROR", "REFUSED_STREAM", "CANCEL", "COMPRESSION_ERROR", "CONNECT_ERROR", "ENHANCE_YOUR_CALM",
        "INADEQUATE_SECURITY", "HTTP_1_1_REQUIRED",
    ];

    public static string ErrorName(uint code) => code < ErrorCodes.Length ? ErrorCodes[code] : $"0x{code:x}";

    public static byte[] Frame(FrameType type, Flags flags, uint streamId, ReadOnlySpan<byte> payload)
    {
        var f = new byte[9 + payload.Length];
        f[0] = (byte)(payload.Length >> 16);
        f[1] = (byte)(payload.Length >> 8);
        f[2] = (byte)payload.Length;
        f[3] = (byte)type;
        f[4] = (byte)flags;
        BinaryPrimitives.WriteUInt32BigEndian(f.AsSpan(5), streamId & 0x7FFFFFFF);
        payload.CopyTo(f.AsSpan(9));
        return f;
    }

    public static byte[] Settings(params (ushort id, uint value)[] settings)
    {
        var p = new byte[settings.Length * 6];
        for (var i = 0; i < settings.Length; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(p.AsSpan(i * 6), settings[i].id);
            BinaryPrimitives.WriteUInt32BigEndian(p.AsSpan(i * 6 + 2), settings[i].value);
        }
        return Frame(FrameType.Settings, Flags.None, 0, p);
    }

    public static byte[] SettingsAck() => Frame(FrameType.Settings, Flags.Ack, 0, []);

    public static byte[] RstStream(uint stream, uint code)
    {
        var p = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(p, code);
        return Frame(FrameType.RstStream, Flags.None, stream, p);
    }

    public static byte[] WindowUpdate(uint stream, uint increment)
    {
        var p = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(p, increment);
        return Frame(FrameType.WindowUpdate, Flags.None, stream, p);
    }

    public static byte[] Ping(ulong data, bool ack = false)
    {
        var p = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(p, data);
        return Frame(FrameType.Ping, ack ? Flags.Ack : Flags.None, 0, p);
    }

    // ------------------------------------------------------------------ HPACK (encode only, no Huffman)

    /// <summary>Encodes headers as "literal without indexing, new name" so every byte we choose reaches the decoder.</summary>
    public static byte[] Hpack(params (string name, string value)[] headers)
    {
        var ms = new MemoryStream();
        foreach (var (name, value) in headers)
        {
            ms.WriteByte(0x00);
            WriteString(ms, name);
            WriteString(ms, value);
        }
        return ms.ToArray();
    }

    public static byte[] HpackTableSizeUpdate(int size)
    {
        var ms = new MemoryStream();
        WriteInt(ms, size, 5, 0x20);
        return ms.ToArray();
    }

    private static void WriteString(Stream s, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        WriteInt(s, bytes.Length, 7, 0x00); // H=0, raw octets
        s.Write(bytes);
    }

    public static void WriteInt(Stream s, long value, int prefixBits, byte firstByteMask)
    {
        var max = (1 << prefixBits) - 1;
        if (value < max)
        {
            s.WriteByte((byte)(firstByteMask | value));
            return;
        }
        s.WriteByte((byte)(firstByteMask | max));
        value -= max;
        while (value >= 128)
        {
            s.WriteByte((byte)(value % 128 + 128));
            value /= 128;
        }
        s.WriteByte((byte)value);
    }

    public static (string, string)[] Get(string path = "/echo", string authority = "lab") =>
        [(":method", "GET"), (":scheme", "http"), (":authority", authority), (":path", path)];

    // ------------------------------------------------------------------ frame reader

    public sealed record Frame9(FrameType Type, Flags Flags, uint Stream, byte[] Payload)
    {
        public uint ErrorCode => Type switch
        {
            FrameType.RstStream => BinaryPrimitives.ReadUInt32BigEndian(Payload),
            FrameType.GoAway => BinaryPrimitives.ReadUInt32BigEndian(Payload.AsSpan(4)),
            _ => 0,
        };

        /// <summary>
        /// Best-effort :status extraction. Kestrel emits :status first, either as a static-table
        /// index (0x88-0x8e) or as a literal whose name is static index 8.
        /// </summary>
        public int? Status
        {
            get
            {
                if (Type != FrameType.Headers || Payload.Length == 0) return null;
                var p = Payload.AsSpan();
                if (Flags.HasFlag(Flags.Padded)) p = p[1..^Payload[0]];
                if (Flags.HasFlag(Flags.Priority)) p = p[5..];
                if (p.Length == 0) return null;
                int[] staticStatus = [200, 204, 206, 304, 400, 404, 500];
                if (p[0] is >= 0x88 and <= 0x8e) return staticStatus[p[0] - 0x88];
                // literal with indexed name 8 (:status) under any of the literal representations
                if (p[0] is 0x48 or 0x08 or 0x18 && p.Length >= 5 && p[1] == 3)
                    return int.TryParse(Encoding.ASCII.GetString(p.Slice(2, 3)), out var s) ? s : null;
                return null;
            }
        }

        public override string ToString() => Type switch
        {
            FrameType.RstStream => $"RST_STREAM(s{Stream},{ErrorName(ErrorCode)})",
            FrameType.GoAway => $"GOAWAY({ErrorName(ErrorCode)})",
            FrameType.Headers => $"HEADERS(s{Stream},:status={Status?.ToString() ?? "?"})",
            _ => $"{Type}(s{Stream},{Payload.Length}b)",
        };
    }

    public static List<Frame9> ParseFrames(byte[] bytes, out bool truncated)
    {
        var frames = new List<Frame9>();
        var i = 0;
        truncated = false;
        while (i + 9 <= bytes.Length)
        {
            var len = (bytes[i] << 16) | (bytes[i + 1] << 8) | bytes[i + 2];
            if (i + 9 + len > bytes.Length) { truncated = true; break; }
            frames.Add(new Frame9((FrameType)bytes[i + 3], (Flags)bytes[i + 4],
                BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(i + 5)) & 0x7FFFFFFF,
                bytes.AsSpan(i + 9, len).ToArray()));
            i += 9 + len;
        }
        return frames;
    }

    /// <summary>Opens an h2c prior-knowledge connection and completes the SETTINGS exchange.</summary>
    public static async Task<RawConnection> OpenAsync(string host, int port, params (ushort, uint)[] settings)
    {
        var c = await RawConnection.ConnectAsync(host, port);
        var hello = new MemoryStream();
        hello.Write(Preface);
        hello.Write(Settings(settings));
        hello.Write(SettingsAck());
        await c.SendAsync(hello.ToArray());
        return c;
    }
}
