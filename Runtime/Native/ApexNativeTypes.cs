using System;
using System.Runtime.InteropServices;

namespace PixoVR.Apex.Native
{
	/// <summary>Why a platform call failed. Mirrors EApexError in the core's Apex.h.</summary>
	public enum EApexError
	{
		APEX_OK = 0,
		/// <summary>No HTTP response: DNS, TLS, timeout or offline.</summary>
		APEX_ERR_NETWORK = 1,
		/// <summary>An HTTP status outside 2xx with no platform error body.</summary>
		APEX_ERR_HTTP = 2,
		/// <summary>The response body was not what the platform should send.</summary>
		APEX_ERR_PARSE = 3,
		/// <summary>The platform reported an error; see the message.</summary>
		APEX_ERR_PLATFORM = 4,
		/// <summary>The call needs a logged-in user.</summary>
		APEX_ERR_NOT_LOGGED_IN = 5,
		APEX_ERR_CANCELLED = 6,
		APEX_ERR_INVALID_ARGUMENT = 7,
		/// <summary>For example, an event sent with no active session.</summary>
		APEX_ERR_INVALID_STATE = 8,
		/// <summary>The core has no built-in HTTP backend on this platform.</summary>
		APEX_ERR_NO_TRANSPORT = 9,
		APEX_ERR_INTERNAL = 10,
	}

	/// <summary>Mirrors EApexEnvironment in Apex.h.</summary>
	internal enum EApexEnvironment
	{
		APEX_ENV_NA_PRODUCTION = 0,
		APEX_ENV_NA_STAGING = 1,
		APEX_ENV_NA_DEVELOPMENT = 2,
		APEX_ENV_SA_PRODUCTION = 3,
		APEX_ENV_LOCAL = 4,
		APEX_ENV_CUSTOM = 5,
	}

	/// <summary>Mirrors EApexLogLevel in Apex.h.</summary>
	internal enum EApexLogLevel
	{
		APEX_LOG_DEBUG = 0,
		APEX_LOG_INFO = 1,
		APEX_LOG_WARNING = 2,
		APEX_LOG_ERROR = 3,
	}

	/// <summary>Mirrors EApexTelemetrySink in Apex.h.</summary>
	internal enum EApexTelemetrySink
	{
		APEX_TELEMETRY_OFF = 0,
		APEX_TELEMETRY_HTTP = 1,
		APEX_TELEMETRY_CALLBACK = 2,
	}

	// The structs below match Apex.h field for field. Strings are UTF-8 pointers, which the
	// wrapper marshals by hand because Mono marshals string as ANSI on Windows.

	[StructLayout(LayoutKind.Sequential)]
	internal struct ApexResultData
	{
		public ulong CallId;
		public int Error;
		public int HttpStatus;
		public IntPtr Message;
		public IntPtr Json;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct ApexConfigData
	{
		public uint StructSize;

		public int Environment;
		public IntPtr HeadsetApiUrl;
		public IntPtr PlatformApiUrl;
		public IntPtr EventPath;

		public int ModuleId;
		public IntPtr ModuleVersion;

		public IntPtr DeviceId;
		public IntPtr DeviceModel;
		public IntPtr Platform;
		public IntPtr ClientIp;

		public double HeartbeatIntervalSeconds;
		public uint RequestTimeoutMilliseconds;

		public IntPtr LogCallback;
		public IntPtr LogUserData;
		public int LogLevel;

		public IntPtr HttpSendCallback;
		public IntPtr HttpSendUserData;

		public int TelemetrySink;
		public IntPtr TelemetryUrl;
		public IntPtr TelemetryCallback;
		public IntPtr TelemetryUserData;
		public double TelemetryFlushIntervalSeconds;
		public double TelemetryPoseSamplesPerSecond;
		public int TelemetryMaximumRecordsPerPacket;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct ApexPoseData
	{
		public float PositionX;
		public float PositionY;
		public float PositionZ;
		public float RotationX;
		public float RotationY;
		public float RotationZ;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void ApexResultCallback(IntPtr userData, ref ApexResultData result);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void ApexLogCallback(IntPtr userData, int logLevel, IntPtr message);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void ApexTelemetryCallback(IntPtr userData, IntPtr packetJson);
}
