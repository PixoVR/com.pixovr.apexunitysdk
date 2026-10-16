namespace PixoVR.Apex.Analytics
{
	/// <summary>Who is playing what, passed to analytics providers with each event.</summary>
	public sealed class ApexAnalyticsContext
	{
		public int UserId { get; set; }
		public int OrgId { get; set; }
		public string UserEmail { get; set; }
		public int ModuleId { get; set; }
		public string ModuleName { get; set; }
		public string ModuleVersion { get; set; }
		public string ScenarioId { get; set; }
		/// <summary>The platform's session ID, or 0 outside a session.</summary>
		public long SessionId { get; set; }
		public string DeviceId { get; set; }
		public string DeviceModel { get; set; }
		public string DeviceSerial { get; set; }
		public string Platform { get; set; }
		public string SdkVersion { get; set; }
	}
}
