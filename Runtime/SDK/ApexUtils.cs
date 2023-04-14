
using Newtonsoft.Json.Linq;
using PixoVR.Apex.XAPI;
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TinCan;
using TinCan.Json;

namespace PixoVR.Apex.Utils
{
    public class ApexUtils
    {
        public static string INVALID_IP = "0.0.0.0";
        public static string INVALID_IPV6 = "::/0";
        public static string HOME_IP = "127.0.0.1";
        public static string HOME_IPV6 = "::1/128";

        public static string GetLocalIP()
        {
            string currentIp = GetIP(ADDRESSFAM.IPv4);
            if(currentIp == INVALID_IP)
            {
                currentIp = GetIP(ADDRESSFAM.IPv6);
            }

            return currentIp;
        }

        private static string GetIP(ADDRESSFAM Addfam)
        {
            //Return null if ADDRESSFAM is Ipv6 but Os does not support it
            if (Addfam == ADDRESSFAM.IPv6 && !Socket.OSSupportsIPv6)
            {
                return null;
            }

            string output = INVALID_IP;

            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                NetworkInterfaceType _type1 = NetworkInterfaceType.Wireless80211;
                NetworkInterfaceType _type2 = NetworkInterfaceType.Ethernet;

                if ((item.NetworkInterfaceType == _type1 || item.NetworkInterfaceType == _type2) && item.OperationalStatus == OperationalStatus.Up)
#endif
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        //IPv4
                        if (Addfam == ADDRESSFAM.IPv4)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                if (ip.Address.ToString() != HOME_IP)
                                    output = ip.Address.ToString();
                            }
                        }

                        //IPv6
                        else if (Addfam == ADDRESSFAM.IPv6)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                if (ip.Address.ToString() != HOME_IPV6)
                                    output = ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            return output;
        }
        public enum ADDRESSFAM
        {
            IPv4, IPv6
        }

        public static JObject ConvertStringToJObject(string json)
        {
            StringOfJSON stringOfJson = new StringOfJSON(json);
            return stringOfJson.toJObject();
        }
    }
}

public static class StringExtensions
{
    public static bool Contains(this string source, string value, StringComparison comp)
    {
        return source?.IndexOf(value, comp) >= 0;
    }
}
