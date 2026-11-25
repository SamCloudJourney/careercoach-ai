using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lab.Common;

public enum Verdict
{
    /// <summary>Behaved as a hardened server/library should.</summary>
    Pass,
    /// <summary>Unusual but not obviously a vulnerability; worth a human look.</summary>
    Review,
    /// <summary>Behaviour consistent with a vulnerability class (desync, bypass, crash, blow-up).</summary>
    Flag,
    /// <summary>The probe itself could not run.</summary>
    Error,
}

public sealed record ProbeResult(
    string Suite,
    string Id,
    string Title,
    Verdict Verdict,
    string Observed,
    string Expected,
    double ElapsedMs,
    string? Reference = null);

/// <summary>Prints a coloured table and appends JSON lines to findings/ for later diffing between runtimes.</summary>
public sealed class ProbeReport
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly List<ProbeResult> _results = new();
    private readonly string _suite;

    public ProbeReport(string suite) => _suite = suite;

    public IReadOnlyList<ProbeResult> Results => _results;

    public void Add(ProbeResult r)
    {
        _results.Add(r);
        var (tag, color) = r.Verdict switch
        {
            Verdict.Pass => ("PASS  ", ConsoleColor.Green),
            Verdict.Review => ("REVIEW", ConsoleColor.Yellow),
            Verdict.Flag => ("FLAG  ", ConsoleColor.Red),
            _ => ("ERROR ", ConsoleColor.Magenta),
        };
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write($"  [{tag}] ");
        Console.ForegroundColor = old;
        Console.WriteLine($"{r.Id,-28} {r.Title}");
        if (r.Verdict != Verdict.Pass)
        {
            Console.WriteLine($"           observed: {Trim(r.Observed)}");
            Console.WriteLine($"           expected: {r.Expected}");
        }
    }

    public string Save(string? directory = null)
    {
        directory ??= Path.Combine(FindLabsRoot(), "findings");
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory,
            $"{_suite}-net{Environment.Version.Major}-{Environment.Version}-{DateTime.UtcNow:yyyyMMddTHHmmssZ}.jsonl");
        using var w = new StreamWriter(file);
        foreach (var r in _results)
        {
            w.WriteLine(JsonSerializer.Serialize(new
            {
                runtime = Environment.Version.ToString(),
                r.Suite, r.Id, r.Title, r.Verdict, r.Observed, r.Expected, r.ElapsedMs, r.Reference,
            }, JsonOpts));
        }
        return file;
    }

    public int Summarize()
    {
        var groups = _results.GroupBy(r => r.Verdict).ToDictionary(g => g.Key, g => g.Count());
        Console.WriteLine();
        Console.WriteLine($"  {_suite}: {_results.Count} probes | " +
                          $"pass {groups.GetValueOrDefault(Verdict.Pass)} | " +
                          $"review {groups.GetValueOrDefault(Verdict.Review)} | " +
                          $"flag {groups.GetValueOrDefault(Verdict.Flag)} | " +
                          $"error {groups.GetValueOrDefault(Verdict.Error)}");
        return groups.GetValueOrDefault(Verdict.Flag) > 0 ? 2 : 0;
    }

    private static string Trim(string s)
    {
        s = s.Replace("\r", "\\r").Replace("\n", "\\n");
        return s.Length > 220 ? s[..220] + "…" : s;
    }

    public static string FindLabsRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
            dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
