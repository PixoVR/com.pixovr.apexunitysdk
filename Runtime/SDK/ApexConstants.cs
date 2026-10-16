namespace PixoVR.Apex
{
	/// <summary>Headset API hosts. The core has the same table; these feed the editor and APIEndpoint.</summary>
	public static class PlatformEndpoints
	{
		public const string kNorthAmericaProduction = "https://modules.apex.pixovr.com";
		public const string kNorthAmericaStaging = "https://modules.apex.stage.pixovr.com";
		public const string kNorthAmericaDevelopment = "https://modules.apex.dev.pixovr.com";
		public const string kSaudiProduction = "https://saudi.modules.apex.pixovr.com";
		public const string kLocalDevelopment = "http://localhost:8001";
	}

	/// <summary>Web app hosts, used by the launcher's web flows.</summary>
	public static class WebPlatformEndpoints
	{
		public const string kNorthAmericaProduction = "https://api.apex.pixovr.com";
		public const string kNorthAmericaStaging = "https://api.apex.stage.pixovr.com";
		public const string kNorthAmericaDevelopment = "https://api.apex.dev.pixovr.com";
		public const string kSaudiProduction = "https://api.apexsa.pixovr.com";
		public const string kLocalDevelopment = "http://localhost:8000";
	}

	/// <summary>Platform API hosts (GraphQL, token and Quick ID login, heartbeat).</summary>
	public static class APIPlatformEndpoints
	{
		public const string kNorthAmericaProduction = "https://apex.pixovr.com";
		public const string kNorthAmericaStaging = "https://apex.stage.pixovr.com";
		public const string kNorthAmericaDevelopment = "https://apex.dev.pixovr.com";
		public const string kSaudiProduction = "https://saudi.apex.pixovr.com";
		public const string kLocalDevelopment = "http://localhost:8000";
	}

	public static class PixoPlatformModuleIDs
	{
		public const int kHubAppModuleId = 13;
	}
}
