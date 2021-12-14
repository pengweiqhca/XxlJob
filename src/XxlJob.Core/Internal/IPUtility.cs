using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace XxlJob.Core.Internal;

/// <summary>
/// ip utility
/// </summary>
internal static  class IpUtility
{
    #region Private Members
    /// <summary>
    /// A类: 10.0.0.0-10.255.255.255
    /// </summary>
    private static long _ipABegin, _ipAEnd;
    /// <summary>
    /// B类: 172.16.0.0-172.31.255.255
    /// </summary>
    private static long _ipBBegin, _ipBEnd;
    /// <summary>
    /// C类: 192.168.0.0-192.168.255.255
    /// </summary>
    private static long _ipCBegin, _ipCEnd;
    #endregion

    #region Constructors
    /// <summary>
    /// static new
    /// </summary>
    static IpUtility()
    {
        _ipABegin = ConvertToNumber("10.0.0.0");
        _ipAEnd = ConvertToNumber("10.255.255.255");

        _ipBBegin = ConvertToNumber("172.16.0.0");
        _ipBEnd = ConvertToNumber("172.31.255.255");

        _ipCBegin = ConvertToNumber("192.168.0.0");
        _ipCEnd = ConvertToNumber("192.168.255.255");
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// ip address convert to long
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    private static long ConvertToNumber(string ipAddress)
    {
        return ConvertToNumber(IPAddress.Parse(ipAddress));
    }
    /// <summary>
    /// ip address convert to long
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    private static long ConvertToNumber(IPAddress ipAddress)
    {
        var bytes = ipAddress.GetAddressBytes();
        return bytes[0] * 256 * 256 * 256 + bytes[1] * 256 * 256 + bytes[2] * 256 + bytes[3];
    }
    /// <summary>
    /// true表示为内网IP
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    public static bool IsIntranet(string ipAddress)
    {
        return IsIntranet(ConvertToNumber(ipAddress));
    }
    /// <summary>
    /// true表示为内网IP
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    private static bool IsIntranet(IPAddress ipAddress)
    {
        return IsIntranet(ConvertToNumber(ipAddress));
    }
    /// <summary>
    /// true表示为内网IP
    /// </summary>
    /// <param name="longIp"></param>
    /// <returns></returns>
    private static bool IsIntranet(long longIp)
    {
        return ((longIp >= _ipABegin) && (longIp <= _ipAEnd) ||
                (longIp >= _ipBBegin) && (longIp <= _ipBEnd) ||
                (longIp >= _ipCBegin) && (longIp <= _ipCEnd));
    }

    /// <summary>
    /// 获取本机内网IP
    /// </summary>
    /// <returns></returns>
    public static IPAddress? GetLocalIntranetIp()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Select(p => p.GetIPProperties())
            .SelectMany(p =>
                p.UnicastAddresses
            ).FirstOrDefault(p => p.Address.AddressFamily == AddressFamily.InterNetwork
                                  && !IPAddress.IsLoopback(p.Address)
                                  && IsIntranet(p.Address))?.Address;
    }

    /// <summary>
    /// 获取本机内网IP列表
    /// </summary>
    /// <returns></returns>
    public static List<IPAddress> GetLocalIntranetIpList()
    {
        var infList =NetworkInterface.GetAllNetworkInterfaces()
            .Select(p => p.GetIPProperties())
            .SelectMany(p => p.UnicastAddresses)
            .Where(p =>
                p.Address.AddressFamily == AddressFamily.InterNetwork
                && !IPAddress.IsLoopback(p.Address)
                && IsIntranet(p.Address)
            );

        var result = new List<IPAddress>();
        foreach (var child in infList)
        {
            result.Add(child.Address);
        }

        return result;
    }
    #endregion
}