using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace XxlJob.Core.Internal;

/// <summary>
/// ip utility
/// </summary>
internal static  class IpUtility
{
    public static string? GetHostIp()
    {
        string? hostIp = null;
        try
        {
            hostIp = NetworkInterface.GetAllNetworkInterfaces()
                .Where(network => network.OperationalStatus == OperationalStatus.Up)
                .Select(network => network.GetIPProperties())
                .OrderByDescending(properties => properties.GatewayAddresses.Count)
                .SelectMany(properties => properties.UnicastAddresses)
                .FirstOrDefault(address => !IPAddress.IsLoopback(address.Address) &&
                                           address.Address.AddressFamily == AddressFamily.InterNetwork)
                ?.Address.ToString();
        }
        catch
        {
            // ignored
        }

        if (hostIp is { } and not "127.0.0.1") return hostIp;

        try
        {
            hostIp = Array.Find(Dns.GetHostEntry(Dns.GetHostName()).AddressList,
                    address => !IPAddress.IsLoopback(address) &&
                               address.AddressFamily == AddressFamily.InterNetwork)
                ?.ToString();
        }
        catch
        {
            // ignored
        }

        return hostIp;
    }
}
