using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WardogsTacticalRadio.Networking;

public sealed class HostConnectivityReport
{
    public string LocalAddress { get; init; } = "Unknown";
    public string GatewayAddress { get; init; } = "Unknown";
    public string PublicAddress { get; init; } = "Unknown";
    public bool FirewallRuleReady { get; init; }
    public bool AutomaticPortMappingReady { get; init; }
    public string PortMappingMessage { get; init; } = "Not attempted";
}

public static class HostConnectivityService
{
    private const string FirewallRuleName = "WARDOGS Tactical Radio TCP";

    public static async Task<HostConnectivityReport> PrepareAsync(int port)
    {
        var local = RadioSessionService.GetBestLanAddress();
        var gateway = GetDefaultGateway() ?? "Unknown";

        var firewallReady = await EnsureFirewallRuleAsync(port);
        var (mapped, mappingMessage) = await TryCreateUpnpMappingAsync(port, local);
        var publicAddress = await TryGetPublicAddressAsync();

        return new HostConnectivityReport
        {
            LocalAddress = local,
            GatewayAddress = gateway,
            PublicAddress = publicAddress,
            FirewallRuleReady = firewallReady,
            AutomaticPortMappingReady = mapped,
            PortMappingMessage = mappingMessage
        };
    }

    public static string? GetDefaultGateway()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().GatewayAddresses)
                .Select(g => g.Address)
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.Any.Equals(a))
                ?.ToString();
        }
        catch { return null; }
    }

    public static void OpenRouterPage()
    {
        var gateway = GetDefaultGateway();
        if (string.IsNullOrWhiteSpace(gateway))
            throw new InvalidOperationException("No IPv4 default gateway was detected.");

        Process.Start(new ProcessStartInfo($"http://{gateway}") { UseShellExecute = true });
    }

    private static async Task<bool> EnsureFirewallRuleAsync(int port)
    {
        try
        {
            var check = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall show rule name=\"{FirewallRuleName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(check))
            {
                if (process is not null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0) return true;
                }
            }

            var add = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall add rule name=\"{FirewallRuleName}\" dir=in action=allow protocol=TCP localport={port} profile=private",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var elevated = Process.Start(add);
            if (elevated is null) return false;
            await elevated.WaitForExitAsync();
            return elevated.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(bool Success, string Message)> TryCreateUpnpMappingAsync(int port, string localAddress)
    {
        return await Task.Run(() =>
        {
            try
            {
#pragma warning disable CA1416
                var natType = Type.GetTypeFromProgID("HNetCfg.NATUPnP");
#pragma warning restore CA1416
                if (natType is null) return (false, "UPnP service not available on this PC/router.");

                dynamic? nat = Activator.CreateInstance(natType);
                dynamic? mappings = nat?.StaticPortMappingCollection;
                if (mappings is null) return (false, "Router did not expose UPnP port mapping.");

                try
                {
                    dynamic? existing = mappings.Item(port, "TCP");
                    if (existing is not null) mappings.Remove(port, "TCP");
                }
                catch { }

                mappings.Add(port, "TCP", port, localAddress, true, "WARDOGS Tactical Radio");
                return (true, $"TCP {port} mapped automatically with UPnP.");
            }
            catch (Exception ex)
            {
                return (false, $"Automatic mapping unavailable: {ex.Message}");
            }
        });
    }

    private static async Task<string> TryGetPublicAddressAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            var value = (await client.GetStringAsync("https://api.ipify.org")).Trim();
            return IPAddress.TryParse(value, out _) ? value : "Unknown";
        }
        catch { return "Unknown"; }
    }
}
