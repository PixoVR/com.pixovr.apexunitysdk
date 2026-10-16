using System;
using Newtonsoft.Json;

namespace PixoVR.Apex
{
	/// <summary>The contents of pixoconfig.cnf, which can select the environment on Android devices.</summary>
	[Serializable]
	public class ConfigurationTypes
	{
		/// <summary>For example "NA Production", "NA Stage", "NA Dev" or "SA Production".</summary>
		[JsonProperty("platform")]
		public string Platform { get; set; }

		[JsonProperty("configTarget")]
		public string ConfigTarget { get; set; }
	}
}
