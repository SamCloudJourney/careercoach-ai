using System.Net;
using System.Net.Sockets;

namespace Lab.Common;

/// <summary>
/// Probes in this lab send malformed and resource-heavy traffic. MSRC's Rules of Engagement
/// require testing only against systems you own, so probes refuse any non-loopback target
/// unless the operator passes --i-own-this-target explicitly.
/// </summary>
public static class TargetGuard
{
    public static void EnsureAllowed(Uri target, bool ownerOverride)
    {
        if (IsLoopback(target.Host)) return;
        if (ownerOverride)
        {
            Console.Error.WriteLine($"[guard] non-loopback target {target.Host} allowed by --i-own-this-target");
            return;
        }
        throw new InvalidOperationException(
            $"Refusing to probe non-loopback host '{target.Host}'. Test only instances you own " +
            "(MSRC Rules of Engagement). Pass --i-own-this-target to override for your own lab host.");
    }

    public static bool IsLoopback(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (IPAddress.TryParse(host.Trim('[', ']'), out var ip)) return IPAddress.IsLoopback(ip);
        try
        {
            return Dns.GetHostAddresses(host).All(IPAddress.IsLoopback);
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
