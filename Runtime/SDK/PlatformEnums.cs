using System;

namespace PixoVR.Apex
{
	/// <summary>The display name and host of an environment.</summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class EndpointAttribute : Attribute
	{
		public EndpointAttribute(string display, string url)
		{
			Display = display;
			Url = url;
		}

		public string Display { get; }
		public string Url { get; }
	}

	/// <summary>The Apex environment the module talks to, by its Headset API host.</summary>
	public enum EPlatformServer
	{
		[Endpoint("North America - Production", PlatformEndpoints.kNorthAmericaProduction)]
		NA_PRODUCTION = 0,
		[Endpoint("North America - Staging", PlatformEndpoints.kNorthAmericaStaging)]
		NA_STAGE,
		[Endpoint("North America - Development", PlatformEndpoints.kNorthAmericaDevelopment)]
		NA_DEV,
		[Endpoint("Saudi - Production", PlatformEndpoints.kSaudiProduction)]
		SA_PRODUCTION,
		[Endpoint("Local Development", PlatformEndpoints.kLocalDevelopment)]
		LOCAL_DEV,
	}

	/// <summary>The same environments, by their web app host.</summary>
	public enum EWebPlatformServer
	{
		[Endpoint("North America - Production", WebPlatformEndpoints.kNorthAmericaProduction)]
		NA_PRODUCTION = 0,
		[Endpoint("North America - Staging", WebPlatformEndpoints.kNorthAmericaStaging)]
		NA_STAGE,
		[Endpoint("North America - Development", WebPlatformEndpoints.kNorthAmericaDevelopment)]
		NA_DEV,
		[Endpoint("Saudi - Production", WebPlatformEndpoints.kSaudiProduction)]
		SA_PRODUCTION,
		[Endpoint("Local Development", WebPlatformEndpoints.kLocalDevelopment)]
		LOCAL_DEV,
	}

	/// <summary>The same environments, by their Platform API host.</summary>
	public enum EAPIPlatformServer
	{
		[Endpoint("North America - Production", APIPlatformEndpoints.kNorthAmericaProduction)]
		NA_PRODUCTION = 0,
		[Endpoint("North America - Staging", APIPlatformEndpoints.kNorthAmericaStaging)]
		NA_STAGE,
		[Endpoint("North America - Development", APIPlatformEndpoints.kNorthAmericaDevelopment)]
		NA_DEV,
		[Endpoint("Saudi - Production", APIPlatformEndpoints.kSaudiProduction)]
		SA_PRODUCTION,
		[Endpoint("Local Development", APIPlatformEndpoints.kLocalDevelopment)]
		LOCAL_DEV,
	}

	public static class PlatformEnumExtensions
	{
		public static string ToDisplayString(this Enum enumValue)
		{
			EndpointAttribute endpoint = FindEndpoint(enumValue);
			return endpoint != null ? endpoint.Display : string.Empty;
		}

		public static string ToUrlString(this Enum enumValue)
		{
			EndpointAttribute endpoint = FindEndpoint(enumValue);
			return endpoint != null ? endpoint.Url : string.Empty;
		}

		private static EndpointAttribute FindEndpoint(Enum enumValue)
		{
			EndpointAttribute[] endpoints = (EndpointAttribute[])enumValue.GetType().GetField(enumValue.ToString())
				.GetCustomAttributes(typeof(EndpointAttribute), false);
			return endpoints.Length > 0 ? endpoints[0] : null;
		}
	}
}
