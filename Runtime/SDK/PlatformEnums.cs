using System;

namespace PixoVR.Apex
{
    [System.AttributeUsage(System.AttributeTargets.Property |
                            System.AttributeTargets.Field)]
    public class EndpointAttribute : System.Attribute
    {
        public string Display;
        public string Url;

        public EndpointAttribute(string display, string url)
        {
            Display = display;
            Url = url;
        }
    }

    public enum PlatformServer : int
    {
        [Endpoint("North America - Production", PlatformEndpoints.NorthAmerica_ProductionEnvironment)]
        NA_PRODUCTION = 0,
        [Endpoint("North America - Staging", PlatformEndpoints.NorthAmerica_StagingEnvironment)]
        NA_STAGE,
        [Endpoint("North America - Development", PlatformEndpoints.NorthAmerica_DevelopmentEnvironment)]
        NA_DEV,
        [Endpoint("Saudi - Production", PlatformEndpoints.Saudi_ProductionEnvironment)]
        SA_PRODUCTION,
        [Endpoint("Local Development", PlatformEndpoints.Local_DevelopmentEnvironment)]
        LOCAL_DEV
    }

    // TODO: Move to new plugin
    public enum WebPlatformServer : int
    {
        [Endpoint("North America - Production", WebPlatformEndpoints.NorthAmerica_ProductionEnvironment)]
        NA_PRODUCTION = 0,
        [Endpoint("North America - Staging", WebPlatformEndpoints.NorthAmerica_StagingEnvironment)]
        NA_STAGE,
        [Endpoint("North America - Development", WebPlatformEndpoints.NorthAmerica_DevelopmentEnvironment)]
        NA_DEV,
        [Endpoint("Saudi - Production", WebPlatformEndpoints.Saudi_ProductionEnvironment)]
        SA_PRODUCTION,
        [Endpoint("Local Development", PlatformEndpoints.Local_DevelopmentEnvironment)]
        LOCAL_DEV
    }

    public enum APIPlatformServer : int
    {
        [Endpoint("North America - Production", APIPlatformEndpoints.NorthAmerica_ProductionEnvironment)]
        NA_PRODUCTION = 0,
        [Endpoint("North America - Staging", APIPlatformEndpoints.NorthAmerica_StagingEnvironment)]
        NA_STAGE,
        [Endpoint("North America - Development", APIPlatformEndpoints.NorthAmerica_DevelopmentEnvironment)]
        NA_DEV,
        [Endpoint("Saudi - Production", APIPlatformEndpoints.Saudi_ProductionEnvironment)]
        SA_PRODUCTION,
        [Endpoint("Local Development", APIPlatformEndpoints.Local_DevelopmentEnvironment)]
        LOCAL_DEV
    }

    public static class PPlatformEnumExtensions
    {
        public static string ToDisplayString(this Enum value)
        {
            EndpointAttribute[] attributes = (EndpointAttribute[])value
               .GetType()
               .GetField(value.ToString())
               .GetCustomAttributes(typeof(EndpointAttribute), false);
            return attributes.Length > 0 ? attributes[0].Display : string.Empty;
        }

        public static string ToUrlString(this Enum value)
        {
            EndpointAttribute[] attributes = (EndpointAttribute[])value
               .GetType()
               .GetField(value.ToString())
               .GetCustomAttributes(typeof(EndpointAttribute), false);
            return attributes.Length > 0 ? attributes[0].Url : string.Empty;
        }
    }
}
