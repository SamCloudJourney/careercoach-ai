using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lab.Target;

/// <summary>SignalR hub with one anonymous and one admin-only method (authorization bypass target).</summary>
public sealed class LabHub : Hub
{
    public string Echo(string message) => message;

    public int Sum(int[] values) => values.Sum();

    [Authorize(Roles = "admin")]
    public string AdminSecret() => "HUB-ADMIN-SECRET";

    public async IAsyncEnumerable<int> Counter(int count, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        for (var i = 0; i < Math.Min(count, 1000); i++)
        {
            yield return i;
            await Task.Delay(1, ct);
        }
    }
}
