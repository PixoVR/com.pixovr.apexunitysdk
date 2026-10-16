using System;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

namespace PixoVR.Apex.Utils
{
	public enum EAddressFamily
	{
		IPV4,
		IPV6,
	}

	public static partial class ApexUtils
	{
		public const string kInvalidIp = "0.0.0.0";
		public const string kInvalidIpv6 = "::/0";
		public const string kHomeIp = "127.0.0.1";
		public const string kHomeIpv6 = "::1/128";

		public static string GetMacAddress()
		{
#if UNITY_WEBGL
			return kHomeIp;
#else
			try
			{
				PhysicalAddress physicalAddress = NetworkInterface.GetAllNetworkInterfaces()
					.FirstOrDefault(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up
						&& networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
					?.GetPhysicalAddress();
				if (physicalAddress == null)
				{
					return string.Empty;
				}

				return string.Join(":",
					physicalAddress.GetAddressBytes().Select(addressByte => addressByte.ToString("X2")));
			}
			catch (Exception exception)
			{
				Debug.LogError("[ApexUtils] " + exception.Message);
				return string.Empty;
			}
#endif
		}

		public static string GetLocalIP()
		{
#if UNITY_WEBGL
			return kHomeIp;
#else
			string localIp = GetIP(EAddressFamily.IPV4);
			if (localIp == kInvalidIp)
			{
				localIp = GetIP(EAddressFamily.IPV6);
			}

			return localIp;
#endif
		}

		private static string GetIP(EAddressFamily addressFamily)
		{
#if UNITY_WEBGL
			return kHomeIp;
#else
			// Null when IPv6 is requested but the OS does not support it.
			if (addressFamily == EAddressFamily.IPV6 && !Socket.OSSupportsIPv6)
			{
				return null;
			}

			string foundAddress = kInvalidIp;
			try
			{
				foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
				{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
					NetworkInterfaceType interfaceType = networkInterface.NetworkInterfaceType;
					bool isWirelessOrEthernet = interfaceType == NetworkInterfaceType.Wireless80211
						|| interfaceType == NetworkInterfaceType.Ethernet;
					if (!isWirelessOrEthernet || networkInterface.OperationalStatus != OperationalStatus.Up)
					{
						continue;
					}

#endif
					UnicastIPAddressInformationCollection unicastAddresses =
						networkInterface.GetIPProperties().UnicastAddresses;
					foreach (UnicastIPAddressInformation unicastAddress in unicastAddresses)
					{
						string addressText = unicastAddress.Address.ToString();
						bool isIpv4 = unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork;
						bool isIpv6 = unicastAddress.Address.AddressFamily == AddressFamily.InterNetworkV6;
						if (addressFamily == EAddressFamily.IPV4 && isIpv4 && addressText != kHomeIp)
						{
							foundAddress = addressText;
						}
						else if (addressFamily == EAddressFamily.IPV6 && isIpv6 && addressText != kHomeIpv6)
						{
							foundAddress = addressText;
						}
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogWarning("[ApexUtils] Could not read network interfaces: " + exception.Message);
			}

			return foundAddress;
#endif
		}
	}
}

public static class StringExtensions
{
	public static bool Contains(this string sourceText, string searchedText, StringComparison comparison)
	{
		return sourceText?.IndexOf(searchedText, comparison) >= 0;
	}
}

public static class DateTimeExtensions
{
	public static string GetLocalFormattedDateTime(this DateTime? dateTime)
	{
		DateTime? localTime = dateTime?.ToLocalTime();
		if (localTime == null)
		{
			return string.Empty;
		}

		return localTime.Value.Date == DateTime.Now.Date
			? localTime.Value.ToString("hh:mm tt", CultureInfo.InvariantCulture)
			: localTime.Value.ToString("MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture);
	}

	public static string GetLocalFormattedDateTime(this DateTime dateTime)
	{
		return ((DateTime?)dateTime).GetLocalFormattedDateTime();
	}
}
