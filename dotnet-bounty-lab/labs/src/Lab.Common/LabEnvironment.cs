using System.Reflection;
using System.Runtime.InteropServices;

namespace Lab.Common;

/// <summary>
/// Identifies exactly which runtime a lab is executing on and whether that runtime
/// is one the .NET bounty currently covers. Every repro should print this banner.
/// </summary>
public static class LabEnvironment
{
    // Keep in sync with scope/manifest.json (scripts/00-check-scope.sh warns on drift).
    public static readonly IReadOnlyDictionary<int, Version> InScopeRuntimes = new Dictionary<int, Version>
    {
        [10] = new(10, 0, 15),
        [11] = new(11, 0, 0),
    };

    public static Version RuntimeVersion => Environment.Version;

    public static string AspNetCoreVersion
    {
        get
        {
            // Resolve the shared framework version if ASP.NET Core is loaded in this process.
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Microsoft.AspNetCore");
            return asm?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                       ?.Split('+')[0] ?? "n/a";
        }
    }

    public static bool IsInScope(out string reason)
    {
        var v = RuntimeVersion;
        if (!InScopeRuntimes.TryGetValue(v.Major, out var latest))
        {
            reason = $".NET {v.Major} is not a supported, in-scope version";
            return false;
        }
        var actual = new Version(v.Major, v.Minor, v.Build);
        if (actual < latest)
        {
            reason = $"{actual} is older than latest patch {latest}; reproduce on the latest patch before reporting";
            return false;
        }
        reason = $"{actual} is the latest in-scope patch of .NET {v.Major}";
        return true;
    }

    public static void PrintBanner(string labName)
    {
        var inScope = IsInScope(out var reason);
        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"┌─ {labName}");
        Console.ForegroundColor = color;
        Console.WriteLine($"│ runtime   : {RuntimeInformation.FrameworkDescription} ({RuntimeVersion})");
        Console.WriteLine($"│ os / arch : {RuntimeInformation.OSDescription} / {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"│ rid       : {RuntimeInformation.RuntimeIdentifier}");
        Console.ForegroundColor = inScope ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine($"│ scope     : {(inScope ? "IN SCOPE" : "CHECK SCOPE")} - {reason}");
        Console.ForegroundColor = color;
        Console.WriteLine("└─");
    }
}
