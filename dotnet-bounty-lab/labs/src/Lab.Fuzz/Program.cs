// labfuzz - parser fuzzing for in-scope .NET / ASP.NET Core components.
//
//   labfuzz list
//   labfuzz smoke <target|all> [--seconds N] [--seed S] [--corpus DIR]   dependency-free mutational fuzzing
//   labfuzz repro <target> <file>                                         replay one input with full stack
//   labfuzz libfuzzer <target>                                            SharpFuzz entry (run under libfuzzer-dotnet)
//
// Findings: labs/findings/fuzz/<runtime>/<target>/<kind>-<hash>.{bin,txt}
//   kind = crash (memory-safety-class exception) | contract (undocumented exception type) | invariant (oracle violated)
//          | slow (algorithmic blow-up) | hang (>10 s) | died (process killed: stack overflow, AV, FailFast)

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Lab.Common;
using Lab.Fuzz;
using Microsoft.Win32.SafeHandles;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("labfuzz list | smoke <target|all> [--seconds N] [--seed S] [--corpus DIR] | repro <target> <file> | libfuzzer <target>");
    return 0;
}

FuzzTarget Find(string name) => Targets.All.FirstOrDefault(t => t.Name == name)
    ?? throw new ArgumentException($"unknown target '{name}'. Run 'labfuzz list'.");

switch (args[0])
{
    case "list":
        foreach (var t in Targets.All)
            Console.WriteLine($"{t.Name,-18} {t.Description}\n{"",-18} source: {t.SourcePath}");
        return 0;

    case "assemblies":
        // Used by scripts/07-libfuzzer.sh to decide which shipped assemblies to instrument.
        Console.WriteLine(string.Join('\n', Find(args[1]).InstrumentAssemblies));
        return 0;

    case "repro":
    {
        var t = Find(args[1]);
        var data = File.ReadAllBytes(args[2]);
        LabEnvironment.PrintBanner($"labfuzz repro {t.Name}");
        var sw = Stopwatch.StartNew();
        try
        {
            t.Execute(data);
            Console.WriteLine($"no exception ({sw.ElapsedMilliseconds} ms)");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{(t.IsExpected(ex) ? "expected" : "UNEXPECTED")} after {sw.ElapsedMilliseconds} ms:\n{ex}");
            return t.IsExpected(ex) ? 0 : 1;
        }
    }

    case "libfuzzer":
    {
        // Under libfuzzer-dotnet: expected rejections are swallowed, everything else propagates
        // and libFuzzer records the input as a crash.
        var t = Find(args[1]);
        SharpFuzz.Fuzzer.LibFuzzer.Run(span =>
        {
            try { t.Execute(span.ToArray()); }
            catch (Exception ex) when (t.IsExpected(ex)) { }
        });
        return 0;
    }

    case "smoke":
    {
        var seconds = 30;
        var seed = Environment.TickCount;
        string? corpus = null;
        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seconds": seconds = int.Parse(args[++i]); break;
                case "--seed": seed = int.Parse(args[++i]); break;
                case "--corpus": corpus = args[++i]; break;
            }
        }
        LabEnvironment.PrintBanner("labfuzz smoke");
        var targets = args.Length > 1 && args[1] != "all" ? [Find(args[1])] : Targets.All.ToArray();
        var perTarget = Math.Max(1, seconds / targets.Length);
        var total = 0;
        foreach (var t in targets) total += new SmokeFuzzer(t, seed, corpus).Run(TimeSpan.FromSeconds(perTarget));
        Console.WriteLine($"\n{total} unique finding(s). Output: {SmokeFuzzer.FindingsRoot}");
        return total > 0 ? 2 : 0;
    }

    default:
        Console.Error.WriteLine($"unknown command '{args[0]}'");
        return 1;
}

internal sealed class SmokeFuzzer(FuzzTarget target, int seed, string? corpusDir)
{
    public static readonly string FindingsRoot =
        Path.Combine(ProbeReport.FindLabsRoot(), "findings", "fuzz", $"net{Environment.Version.Major}-{Environment.Version}");

    private readonly Random _rng = new(seed);
    private readonly HashSet<string> _seen = new();
    private readonly string _dir = Path.Combine(FindingsRoot, target.Name);
    private static readonly byte[] Interesting = [0x00, 0x01, 0x7F, 0x80, 0xFF, 0xFE, (byte)'\n', (byte)'\r', (byte)'"', (byte)'\\', (byte)'<', (byte)'%', (byte)'&', (byte)'/', (byte)'.', 0xC0, 0xED, 0xF4];

    public int Run(TimeSpan budget)
    {
        Directory.CreateDirectory(_dir);
        var inflightPath = Path.Combine(_dir, ".inflight");
        var findings = RecoverAbnormalExit(inflightPath);

        var pool = new List<byte[]>(target.Seeds);
        if (corpusDir is not null && Directory.Exists(Path.Combine(corpusDir, target.Name)))
            pool.AddRange(Directory.EnumerateFiles(Path.Combine(corpusDir, target.Name)).Select(File.ReadAllBytes));
        var dict = target.Dictionary.Select(Encoding.UTF8.GetBytes).ToArray();

        using var inflight = File.OpenHandle(inflightPath, FileMode.Create, FileAccess.ReadWrite);
        long execs = 0;
        var started = Stopwatch.StartNew();
        var current = Stopwatch.StartNew();
        byte[] input = [];

        // Watchdog: a single input running > 10 s is a hang; persist it and exit (we cannot abort managed code safely).
        using var watchdog = new Timer(_ =>
        {
            if (current.ElapsedMilliseconds > 10_000)
            {
                Save("hang", input, $"input exceeded 10 s on {target.Name}");
                Console.Error.WriteLine($"\n[{target.Name}] HANG saved; exiting");
                Environment.Exit(3);
            }
        }, null, 1000, 1000);

        Console.Write($"  {target.Name,-18} ");
        while (started.Elapsed < budget)
        {
            input = Mutate(pool[_rng.Next(pool.Count)], pool, dict);
            // Persist before executing so a process-killing bug (stack overflow, AV) is recoverable.
            RandomAccess.SetLength(inflight, 0);
            RandomAccess.Write(inflight, input, 0);
            current.Restart();
            try
            {
                target.Execute(input);
                if (current.ElapsedMilliseconds > target.SlowMs)
                    findings += Save("slow", input, $"{current.ElapsedMilliseconds} ms for {input.Length} bytes (threshold {target.SlowMs} ms)");
                else if (pool.Count < 4096 && _rng.Next(8) == 0)
                    pool.Add(input); // keep accepted inputs as future seeds
            }
            catch (Exception ex) when (target.IsExpected(ex))
            {
                if (current.ElapsedMilliseconds > target.SlowMs)
                    findings += Save("slow", input, $"{current.ElapsedMilliseconds} ms before {ex.GetType().Name}");
            }
            catch (Exception ex)
            {
                findings += Save(Classify(ex), input, ex.ToString(), Signature(ex));
            }
            execs++;
        }
        RandomAccess.SetLength(inflight, 0);
        Console.WriteLine($"{execs,9:N0} execs  {execs / Math.Max(1, started.Elapsed.TotalSeconds),8:N0}/s  findings {findings}");
        return findings;
    }

    private int RecoverAbnormalExit(string inflightPath)
    {
        if (!File.Exists(inflightPath) || new FileInfo(inflightPath).Length == 0) return 0;
        Save("died", File.ReadAllBytes(inflightPath), "previous run terminated while executing this input (stack overflow / access violation / FailFast?)");
        return 1;
    }

    private int Save(string kind, byte[] input, string detail, string? signature = null)
    {
        var key = kind + ":" + (signature ?? Convert.ToHexString(SHA256.HashData(input))[..16]);
        if (!_seen.Add(key)) return 0;
        var hash = Convert.ToHexString(SHA256.HashData(input))[..12].ToLowerInvariant();
        var basePath = Path.Combine(_dir, $"{kind}-{hash}");
        File.WriteAllBytes(basePath + ".bin", input);
        File.WriteAllText(basePath + ".txt",
            $"target   : {target.Name}\nsource   : {target.SourcePath}\nruntime  : {Environment.Version}\nkind     : {kind}\n" +
            $"repro    : labfuzz repro {target.Name} {basePath}.bin\n\n{detail}\n");
        Console.Write($"[{kind}] ");
        return 1;
    }

    /// <summary>
    /// crash     = memory-safety-class or runtime-integrity failure (most interesting)
    /// contract  = a managed exception type the API does not document for bad input (robustness / possible DoS)
    /// invariant = an oracle (round-trip, differential, safety property) was violated
    /// </summary>
    private static string Classify(Exception ex) => ex switch
    {
        InvariantViolation => "invariant",
        NullReferenceException or IndexOutOfRangeException or ArgumentOutOfRangeException or InvalidCastException
            or AccessViolationException or OutOfMemoryException or InsufficientExecutionStackException
            or ArrayTypeMismatchException or DivideByZeroException => "crash",
        // OverflowException comes from *checked* arithmetic: memory-safe, but an undocumented type.
        _ => "contract",
    };

    /// <summary>Exception type + first frame inside the framework - dedups thousands of hits of one bug.</summary>
    private static string Signature(Exception ex)
    {
        var frame = new StackTrace(ex, false).GetFrames()
            .Select(f => f.GetMethod())
            .FirstOrDefault(m => m?.DeclaringType?.Namespace is { } ns && !ns.StartsWith("Lab.", StringComparison.Ordinal));
        return ex is InvariantViolation
            ? "inv:" + new string(ex.Message.TakeWhile(c => c != ':').ToArray())
            : $"{ex.GetType().FullName}@{frame?.DeclaringType?.FullName}.{frame?.Name}";
    }

    private byte[] Mutate(byte[] src, List<byte[]> pool, byte[][] dict)
    {
        var buf = new List<byte>(src);
        var rounds = 1 + _rng.Next(4);
        for (var r = 0; r < rounds; r++)
        {
            var pos = buf.Count == 0 ? 0 : _rng.Next(buf.Count);
            switch (_rng.Next(9))
            {
                case 0 when buf.Count > 0: buf[pos] ^= (byte)(1 << _rng.Next(8)); break;
                case 1 when buf.Count > 0: buf[pos] = Interesting[_rng.Next(Interesting.Length)]; break;
                case 2: buf.Insert(pos, (byte)_rng.Next(256)); break;
                case 3 when buf.Count > 1: buf.RemoveRange(pos, Math.Min(buf.Count - pos, 1 + _rng.Next(8))); break;
                case 4 when dict.Length > 0: buf.InsertRange(pos, dict[_rng.Next(dict.Length)]); break;
                case 5 when buf.Count > 1: // duplicate a slice (grows nesting / repetition)
                {
                    var len = Math.Min(buf.Count - pos, 1 + _rng.Next(32));
                    var slice = buf.GetRange(pos, len);
                    for (var k = _rng.Next(1, 16); k > 0; k--) buf.InsertRange(pos, slice);
                    break;
                }
                case 6: // splice with another pool entry
                {
                    var other = pool[_rng.Next(pool.Count)];
                    if (other.Length == 0) break;
                    var start = _rng.Next(other.Length);
                    buf.InsertRange(pos, other.Skip(start).Take(1 + _rng.Next(Math.Min(64, other.Length - start))));
                    break;
                }
                case 7 when buf.Count > 0: buf.RemoveRange(pos, buf.Count - pos); break; // truncate
                default:
                    if (buf.Count > 0) buf[pos] = (byte)(buf[pos] + _rng.Next(-3, 4));
                    break;
            }
        }
        if (buf.Count > 64 * 1024) buf.RemoveRange(64 * 1024, buf.Count - 64 * 1024);
        return buf.ToArray();
    }
}
