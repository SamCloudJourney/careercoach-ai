// labctl - probe runner for the .NET bounty lab.
//
//   dotnet run --project src/Lab.Probes -f net11.0 -- all
//   dotnet run --project src/Lab.Probes -f net10.0 -- http1 http2 --host 127.0.0.1
//
// Suites: http1 http2 auth signalr all.  Exit code 2 if any probe is FLAGged.

using Lab.Common;
using Lab.Probes;

var suites = new List<string>();
var host = "127.0.0.1";
int h1 = 5101, h2 = 5102, app = 5103;
var owner = false;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--host": host = args[++i]; break;
        case "--h1": h1 = int.Parse(args[++i]); break;
        case "--h2": h2 = int.Parse(args[++i]); break;
        case "--app": app = int.Parse(args[++i]); break;
        case "--i-own-this-target": owner = true; break;
        case "-h" or "--help":
            Console.WriteLine("labctl [http1|http2|auth|signalr|all]... [--host H] [--h1 P] [--h2 P] [--app P] [--i-own-this-target]");
            return 0;
        default: suites.Add(args[i].ToLowerInvariant()); break;
    }
}
if (suites.Count == 0 || suites.Contains("all")) suites = ["http1", "http2", "auth", "signalr"];

LabEnvironment.PrintBanner("labctl (probe client)");
TargetGuard.EnsureAllowed(new Uri($"http://{host}"), owner);

// Wait for the target so the matrix script can start both processes together.
using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
{
    for (var attempt = 0; ; attempt++)
    {
        try
        {
            var health = await http.GetStringAsync($"http://{host}:{app}/health");
            Console.WriteLine($"target  : {health}");
            break;
        }
        catch when (attempt < 40) { await Task.Delay(250); }
    }
}

var exit = 0;
foreach (var suite in suites)
{
    Console.WriteLine();
    Console.WriteLine($"=== {suite} ===");
    var report = new ProbeReport(suite);
    Task run = suite switch
    {
        "http1" => new Http1Suite(host, h1).RunAsync(report),
        "http2" => new Http2Suite(host, h2, app).RunAsync(report),
        "auth" => new AuthSuite(host, app).RunAsync(report),
        "signalr" => new SignalRSuite(host, app).RunAsync(report),
        _ => throw new ArgumentException($"unknown suite '{suite}'"),
    };
    await run;
    exit = Math.Max(exit, report.Summarize());
    Console.WriteLine($"  saved -> {report.Save()}");
}
return exit;
