using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Properties;
using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;
#endif

// The models below are what ApexSystem hands to callbacks. Each is built from the JSON the
// core returns (apex-sdk-core spec/schema), with FromJson methods that tolerate missing and
// null values the way the platform sends them.
namespace PixoVR.Apex
{
	/// <summary>A failed call, in the shape the 1.x callbacks used. See <see cref="ApexResponse"/>.</summary>
	[Serializable]
	public class FailureResponse
	{
		public string Error { get; set; }
		public string HttpCode { get; set; }
		public string Message { get; set; }

		public bool HasErrored()
		{
			if (string.IsNullOrEmpty(Error))
			{
				return false;
			}

			return !Error.Equals("false", StringComparison.OrdinalIgnoreCase);
		}

		internal static FailureResponse FromMessage(string message)
		{
			FailureResponse failureResponse = new FailureResponse();
			failureResponse.Error = "true";
			failureResponse.HttpCode = string.Empty;
			failureResponse.Message = message;
			return failureResponse;
		}
	}

	/// <summary>The platform's reply to joining a session.</summary>
	[Serializable]
	public class JoinSessionResponse
	{
		/// <summary>The platform's session ID, which the heartbeat and telemetry use.</summary>
		public long SessionId { get; set; }

		/// <summary>The session's UUID, sent with every event of the session.</summary>
		public Guid SessionUuid { get; set; }
	}

	/// <summary>The modules each requested user can access.</summary>
	[Serializable]
	public class GetUserModulesResponse
	{
		public List<UserModulesData> ParsedData { get; set; } = new List<UserModulesData>();

		internal static GetUserModulesResponse FromJson(JObject payload)
		{
			GetUserModulesResponse userModulesResponse = new GetUserModulesResponse();
			foreach (JObject userEntry in JsonReading.GetObjects(payload, "users"))
			{
				UserModulesData userModules = new UserModulesData();
				userModules.UserId = JsonReading.GetInteger(userEntry, "userId") ?? -1;
				foreach (JToken moduleIdToken in JsonReading.GetArray(userEntry, "moduleIds"))
				{
					int? moduleId = JsonReading.ToInteger(moduleIdToken);
					if (moduleId.HasValue)
					{
						userModules.AvailableModules.Add(moduleId.Value);
					}
				}

				userModulesResponse.ParsedData.Add(userModules);
			}

			return userModulesResponse;
		}
	}

	[Serializable]
	public class UserModulesData
	{
		public int UserId { get; set; } = -1;
		public List<int> AvailableModules { get; set; } = new List<int>();
	}

	[Serializable]
	public class LoginData
	{
		public LoginData(string username, string password)
		{
			Login = username;
			Password = password;
		}

		public string Login { get; set; }
		public string Password { get; set; }
	}

	/// <summary>The logged-in user (spec/schema/user.json).</summary>
	[Serializable]
	public class LoginResponseContent
	{
		public int ID { get; set; }
		public int OrgId { get; set; }
		public string First { get; set; }
		public string Last { get; set; }
		/// <summary>Only Quick ID logins return it.</summary>
		public string Username { get; set; }
		public string Email { get; set; }
		/// <summary>Bearer token for later calls, and for handing the login on to the next app.</summary>
		public string Token { get; set; }
		public string Role { get; set; }
		public Organization Org { get; set; }
		public int MinimumPassingScore { get; set; }

		public bool HasErrored()
		{
			return string.IsNullOrEmpty(Token);
		}

		public bool IsPlatformSuperadmin()
		{
			bool isSuperAdmin = !string.IsNullOrEmpty(Role)
				&& Role.Equals("superadmin", StringComparison.OrdinalIgnoreCase);
			bool isPlatformOrg = Org != null && !string.IsNullOrEmpty(Org.Type)
				&& Org.Type.Equals("platform", StringComparison.OrdinalIgnoreCase);
			return isPlatformOrg && isSuperAdmin;
		}

		internal static LoginResponseContent FromJson(JObject payload)
		{
			LoginResponseContent user = new LoginResponseContent();
			user.ID = JsonReading.GetInteger(payload, "id") ?? 0;
			user.OrgId = JsonReading.GetInteger(payload, "orgId") ?? 0;
			user.First = JsonReading.GetString(payload, "firstName");
			user.Last = JsonReading.GetString(payload, "lastName");
			user.Username = JsonReading.GetString(payload, "username");
			user.Email = JsonReading.GetString(payload, "email");
			user.Role = JsonReading.GetString(payload, "role");
			user.Token = JsonReading.GetString(payload, "token");
			user.MinimumPassingScore = (int)Math.Round(JsonReading.GetNumber(payload, "minimumPassingScore") ?? 0.0);
			JObject organizationObject = JsonReading.GetObject(payload, "org");
			user.Org = organizationObject == null ? null : Organization.FromJson(organizationObject);
			return user;
		}
	}

	/// <summary>Whether a user may run a module on this device.</summary>
	[Serializable]
	public class UserAccessResponseContent
	{
		public int UserId { get; set; } = -1;
		public int ModuleId { get; set; } = -1;
		public bool Access { get; set; }
		public int? PassingScore { get; set; }

		public bool HasErrored()
		{
			return UserId == -1 || ModuleId == -1;
		}

		internal static UserAccessResponseContent FromJson(JObject payload)
		{
			UserAccessResponseContent userAccess = new UserAccessResponseContent();
			userAccess.UserId = JsonReading.GetInteger(payload, "userId") ?? -1;
			userAccess.ModuleId = JsonReading.GetInteger(payload, "moduleId") ?? -1;
			userAccess.Access = JsonReading.GetBoolean(payload, "access") ?? false;
			double? passingScore = JsonReading.GetNumber(payload, "passingScore");
			userAccess.PassingScore = passingScore.HasValue ? (int?)Math.Round(passingScore.Value) : null;
			return userAccess;
		}
	}

	[Serializable]
	public class ActiveUserInformation
	{
		public LoginResponseContent User { get; set; }
		public UserAccessResponseContent ModuleUserInformation { get; set; }
	}

	[Serializable]
	public class Organization
	{
		public int ID { get; set; }
		public string Name { get; set; }
		public string Status { get; set; }
		public string DownloadRegion { get; set; }
		public string Type { get; set; }
		public string HubLogoLink { get; set; }
		public string PrimaryColor { get; set; }
		public string SecondaryColor { get; set; }

		internal static Organization FromJson(JObject payload)
		{
			Organization organization = new Organization();
			organization.ID = JsonReading.GetInteger(payload, "id") ?? 0;
			organization.Name = JsonReading.GetString(payload, "name");
			organization.Status = JsonReading.GetString(payload, "status");
			organization.DownloadRegion = JsonReading.GetString(payload, "downloadRegion");
			organization.Type = JsonReading.GetString(payload, "type");
			organization.HubLogoLink = JsonReading.GetString(payload, "hubLogoLink");
			organization.PrimaryColor = JsonReading.GetString(payload, "primaryColor");
			organization.SecondaryColor = JsonReading.GetString(payload, "secondaryColor");
			return organization;
		}
	}

	/// <summary>A one-time login code, for signing in on another device.</summary>
	[Serializable]
	public class GeneratedAssistedLogin
	{
		public AssistedLoginCode AssistedLogin { get; set; }

		public bool HasErrored()
		{
			return AssistedLogin == null;
		}

		internal static GeneratedAssistedLogin FromJson(JObject payload)
		{
			GeneratedAssistedLogin assistedLogin = new GeneratedAssistedLogin();
			assistedLogin.AssistedLogin = new AssistedLoginCode();
			assistedLogin.AssistedLogin.AuthCode = JsonReading.GetString(payload, "code");
			assistedLogin.AssistedLogin.Expires = JsonReading.GetString(payload, "expiresAt");
			return assistedLogin;
		}
	}

	[Serializable]
	public class AssistedLoginCode
	{
		public string AuthCode { get; set; }
		public string Expires { get; set; }
	}

	/// <summary>A session's result, sent when the session completes.</summary>
	[Serializable]
	public class SessionData
	{
		public SessionData()
		{
		}

		public SessionData(float score, float scaledScore, float minimumScore, float maximumScore, int durationSeconds,
			bool complete, bool success)
		{
			Score = score;
			ScaledScore = scaledScore;
			MinimumScore = minimumScore;
			MaximumScore = maximumScore;
			Duration = durationSeconds;
			Complete = complete;
			Success = success;
		}

		public float Score { get; set; }
		/// <summary>0 to 100. Left at 0, it is computed from Score and MaximumScore.</summary>
		public float ScaledScore { get; set; }
		public float MinimumScore { get; set; }
		public float MaximumScore { get; set; }
		/// <summary>Session length in seconds.</summary>
		public int Duration { get; set; }
		public bool Complete { get; set; }
		public bool Success { get; set; }
	}

#if UNITY_6000_0_OR_NEWER
	public class OrgModule : ScriptableObject, INotifyBindablePropertyChanged
#else
	[Serializable]
	public class OrgModule
#endif
	{
		private Texture2D m_thumbnail;

#if UNITY_6000_0_OR_NEWER
		public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged; // StyleCheck: external name
#endif

		[CreateProperty]
		public int ID { get; set; } = -1;
		[CreateProperty]
		public string Name { get; set; } = string.Empty;
		[CreateProperty]
		public string Description { get; set; } = string.Empty;
		[CreateProperty]
		public string ShortDescription { get; set; } = string.Empty;
		[CreateProperty]
		public string LongDescription { get; set; } = string.Empty;
		[CreateProperty]
		public string Industry { get; set; } = string.Empty;
		[CreateProperty]
		public string Details { get; set; } = string.Empty;
		[CreateProperty]
		public string IconURL { get; set; } = string.Empty;
		[CreateProperty]
		public string AvailableLanguages { get; set; } = string.Empty;
		[CreateProperty]
		public string Distributor { get; set; } = string.Empty;
		[CreateProperty]
		public string UserGuideLink { get; set; } = string.Empty;
		[CreateProperty]
		public bool IsAuthenticatedLaunch { get; set; }
		[CreateProperty]
		public bool EnableDebug { get; set; }
		[CreateProperty]
		public List<OrgModuleDownload> Downloads { get; set; } = new List<OrgModuleDownload>();
		[CreateProperty]
		public int? PassingScore { get; set; }
		[CreateProperty]
		public string Categories { get; set; } = string.Empty;
		[CreateProperty]
		public string ExternalId { get; set; } = string.Empty;
		[CreateProperty]
		public PlatformPlayer Player { get; set; }

		[CreateProperty]
		public Texture2D Thumbnail
		{
			get
			{
				return m_thumbnail;
			}
			set
			{
				m_thumbnail = value;
#if UNITY_6000_0_OR_NEWER
				Notify();
#endif
			}
		}

#if UNITY_6000_0_OR_NEWER
		private void Notify([CallerMemberName] string propertyName = "")
		{
			propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propertyName));
		}
#endif
	}

	/// <summary>One downloadable build of a module for one platform.</summary>
	[Serializable]
	public class OrgModuleDownload : ScriptableObject
	{
		public int ID { get; set; }
		public int VersionID { get; set; }
		public string FileLocation { get; set; }
		public string Version { get; set; }
		public string Platform { get; set; }
		public long DownloadSize { get; set; }
		/// <summary>Android package name of this build.</summary>
		public string ApkName { get; set; }
		public string URL { get; set; }
		/// <summary>The lifecycle name of a module version; the status of a player version.</summary>
		public string Status { get; set; }
		public string ExternalId { get; set; }
		/// <summary>The lifecycle Status maps to, for module versions.</summary>
		public EModuleLifecycle Lifecycle { get; set; } = EModuleLifecycle.OTHER;
	}

	/// <summary>The player app a module runs in, with its builds.</summary>
	[Serializable]
	public class PlatformPlayer
	{
		public PlatformPlayer(int playerId)
		{
			Id = playerId;
		}

		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public int DistributorId { get; set; }
		public string LaunchProtocol { get; set; }
		public List<PlatformPlayerDownload> Versions { get; set; } = new List<PlatformPlayerDownload>();
	}

	[Serializable]
	public class PlatformPlayerDownload
	{
		public int Id { get; set; }
		public string Version { get; set; }
		public int ModulePlayerId { get; set; }
		public string Status { get; set; }
		public string URL { get; set; }
		/// <summary>Android package name of this build.</summary>
		public string ApkName { get; set; }
		public string Platform { get; set; }
	}

	/// <summary>Users enrolled for Quick ID on a device, and their organization's branding.</summary>
	[Serializable]
	public class QuickIDAuthGetUsersResponse
	{
		public OrgProperties OrgProperties { get; set; }
		public List<QuickIDUser> QuickIDUsers { get; set; } = new List<QuickIDUser>();

		public bool HasErrored()
		{
			return QuickIDUsers == null || OrgProperties == null;
		}

		internal static QuickIDAuthGetUsersResponse FromJson(JObject payload)
		{
			QuickIDAuthGetUsersResponse quickIdResponse = new QuickIDAuthGetUsersResponse();
			JObject organizationObject = JsonReading.GetObject(payload, "org") ?? new JObject();
			quickIdResponse.OrgProperties = new OrgProperties(JsonReading.GetString(organizationObject, "primaryColor"),
				JsonReading.GetString(organizationObject, "secondaryColor"),
				JsonReading.GetString(organizationObject, "hubLogoUrl"),
				JsonReading.GetString(organizationObject, "name"));
			foreach (JObject userObject in JsonReading.GetObjects(payload, "users"))
			{
				quickIdResponse.QuickIDUsers.Add(new QuickIDUser(JsonReading.GetString(userObject, "firstName"),
					JsonReading.GetString(userObject, "lastName"), JsonReading.GetString(userObject, "username"),
					JsonReading.GetString(userObject, "email")));
			}

			return quickIdResponse;
		}
	}

	[Serializable]
	public class OrgProperties
	{
		public OrgProperties()
		{
		}

		public OrgProperties(string primaryColor, string secondaryColor, string hubLogoUrl, string orgName)
		{
			PrimaryColor = primaryColor;
			SecondaryColor = secondaryColor;
			HubLogoURL = hubLogoUrl;
			OrgName = orgName;
		}

		public string PrimaryColor { get; set; }
		public string SecondaryColor { get; set; }
		public string HubLogoURL { get; set; }
		public string OrgName { get; set; }
	}

	[Serializable]
	public class QuickIDUser
	{
		public QuickIDUser()
		{
		}

		public QuickIDUser(string firstName, string lastName, string username, string email)
		{
			FirstName = firstName;
			LastName = lastName;
			Username = username;
			Email = email;
		}

		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
	}

	/// <summary>The modules available to the current user (spec/schema/module_list.json).</summary>
	[Serializable]
	public class UserModulesResponse
	{
		public List<Module> Modules { get; set; } = new List<Module>();

		internal static UserModulesResponse FromJson(string payloadJson)
		{
			UserModulesResponse modulesResponse = JsonConvert.DeserializeObject<UserModulesResponse>(payloadJson);
			if (modulesResponse == null)
			{
				return new UserModulesResponse();
			}

			modulesResponse.Modules ??= new List<Module>();
			return modulesResponse;
		}
	}

	/// <summary>One page of the current user's modules (spec/schema/module_page.json).</summary>
	[Serializable]
	public class ModulePageResponse
	{
		/// <summary>At most PageSize modules; empty past the last page.</summary>
		public List<Module> Modules { get; set; } = new List<Module>();
		public int PageIndex { get; set; }
		public int PageSize { get; set; }
		public int PageCount { get; set; }
		/// <summary>Modules matching the filters, across every page.</summary>
		public int TotalCount { get; set; }

		internal static ModulePageResponse FromJson(string payloadJson)
		{
			ModulePageResponse pageResponse = JsonConvert.DeserializeObject<ModulePageResponse>(payloadJson);
			if (pageResponse == null)
			{
				return new ModulePageResponse();
			}

			pageResponse.Modules ??= new List<Module>();
			return pageResponse;
		}
	}

	/// <summary>
	/// Module version lifecycles, as flags for <see cref="ApexSystem.GetModulesPage"/>. Mirrors EApexLifecycle
	/// in Apex.h. A lifecycle named anything other than Released, Development, QA, UAT or Archived, or no
	/// lifecycle, is OTHER. Archived versions are never returned.
	/// </summary>
	[Flags]
	public enum EModuleLifecycle
	{
		RELEASED = 1,
		DEVELOPMENT = 2,
		QA = 4,
		UAT = 8,
		OTHER = 16,
		/// <summary>Every lifecycle the SDK returns: all but Archived.</summary>
		ALL = RELEASED | DEVELOPMENT | QA | UAT | OTHER,
	}

	/// <summary>
	/// Order of the modules <see cref="ApexSystem.GetModulesPage"/> pages through. Mirrors EApexModuleSort.
	/// </summary>
	public enum EModuleSortOrder
	{
		/// <summary>The order the platform returns.</summary>
		PLATFORM = 0,
		/// <summary>Name A-Z, ignoring case.</summary>
		NAME = 1,
		/// <summary>Module ID, lowest first.</summary>
		ID = 2,
		/// <summary>Newest matching version (CreatedAt) first.</summary>
		NEWEST_VERSION = 3,
	}

	/// <summary>A module version's lifecycle.</summary>
	[Serializable]
	public class ModuleLifecycle
	{
		public int? Id { get; set; }
		/// <summary>As the platform names it; null when the version has no lifecycle.</summary>
		public string Name { get; set; }
		/// <summary>The lifecycle the name maps to; a single flag.</summary>
		public EModuleLifecycle Stage { get; set; } = EModuleLifecycle.OTHER;
	}

	/// <summary>A module as the core returns it, with versions already filtered to one platform.</summary>
	[Serializable]
	public class Module
	{
		/// <summary>-1 when the platform sent an ID that is not a number.</summary>
		public int Id { get; set; } = -1;
		public string Name { get; set; }
		public string Abbreviation { get; set; }
		public string ExternalId { get; set; }
		public string IconUrl { get; set; }
		public string Distributor { get; set; }
		public string ShortDescription { get; set; }
		public string LongDescription { get; set; }
		/// <summary>First letter capitalized.</summary>
		public string Industry { get; set; }
		public string Details { get; set; }
		public string Categories { get; set; }
		public bool IsAvailable { get; set; }
		public bool IsAuthenticatedLaunch { get; set; }
		public List<string> Languages { get; set; } = new List<string>();
		public ModulePlayer Player { get; set; }
		public List<ModuleVersion> Versions { get; set; } = new List<ModuleVersion>();
	}

	[Serializable]
	public class ModulePlayer
	{
		public int? Id { get; set; }
		public string Name { get; set; }
		public string LaunchProtocol { get; set; }
		public List<ModulePlayerVersion> Versions { get; set; } = new List<ModulePlayerVersion>();
	}

	[Serializable]
	public class ModulePlayerVersion
	{
		public int? Id { get; set; }
		public string Version { get; set; }
		public string Status { get; set; }
		/// <summary>Android package name of this player version's build.</summary>
		public string Package { get; set; }
		public List<string> Platforms { get; set; } = new List<string>();
	}

	[Serializable]
	public class ModuleVersion
	{
		public int? Id { get; set; }
		public string Version { get; set; }
		/// <summary>Android package name of this version's build.</summary>
		public string Package { get; set; }
		public string CreatedAt { get; set; }
		/// <summary>Names of the controls this version supports.</summary>
		public List<string> Controls { get; set; } = new List<string>();
		/// <summary>Short name, or name when the short name is empty.</summary>
		public List<string> Platforms { get; set; } = new List<string>();
		public ModuleLifecycle Lifecycle { get; set; } = new ModuleLifecycle();
	}

	public static class ModuleExtensions
	{
		public static OrgModule ToOrgModule(this Module module)
		{
			if (module == null)
			{
				return null;
			}

#if UNITY_6000_0_OR_NEWER
			OrgModule orgModule = ScriptableObject.CreateInstance<OrgModule>();
#else
			OrgModule orgModule = new OrgModule();
#endif
			orgModule.ID = module.Id;
			orgModule.Name = module.Name ?? string.Empty;
			orgModule.Description = module.Name ?? string.Empty;
			orgModule.ShortDescription = module.ShortDescription;
			orgModule.LongDescription = module.LongDescription;
			orgModule.Industry = CapitalizeIndustry(module.Industry) ?? string.Empty;
			orgModule.Details = module.Details;
			orgModule.Categories = module.Categories;
			orgModule.ExternalId = module.ExternalId;
			orgModule.IconURL = module.IconUrl;
			orgModule.Distributor = module.Distributor;
			orgModule.IsAuthenticatedLaunch = module.IsAuthenticatedLaunch;
			orgModule.AvailableLanguages = module.Languages == null
				? string.Empty
				: string.Join(", ", module.Languages);
			orgModule.Downloads = new List<OrgModuleDownload>();

			if (module.Player != null)
			{
				orgModule.Player = new PlatformPlayer(module.Player.Id ?? -1);
				orgModule.Player.Name = module.Player.Name;
				orgModule.Player.LaunchProtocol = module.Player.LaunchProtocol;
				foreach (ModulePlayerVersion playerVersion in module.Player.Versions ?? new List<ModulePlayerVersion>())
				{
					orgModule.Player.Versions.AddRange(ToPlatformPlayerDownloads(playerVersion));
				}
			}

			foreach (ModuleVersion moduleVersion in module.Versions ?? new List<ModuleVersion>())
			{
				orgModule.Downloads.AddRange(ToOrgModuleDownloads(orgModule.ID, moduleVersion));
			}

			return orgModule;
		}

		public static List<OrgModule> ToOrgModules(this List<Module> modules)
		{
			List<OrgModule> orgModules = new List<OrgModule>();
			if (modules == null)
			{
				return orgModules;
			}

			foreach (Module module in modules)
			{
				OrgModule orgModule = module.ToOrgModule();
				if (orgModule != null)
				{
					orgModules.Add(orgModule);
				}
			}

			return orgModules;
		}

		private static List<OrgModuleDownload> ToOrgModuleDownloads(int moduleId, ModuleVersion moduleVersion)
		{
			List<OrgModuleDownload> downloads = new List<OrgModuleDownload>();
			if (moduleVersion?.Platforms == null)
			{
				return downloads;
			}

			foreach (string platformName in moduleVersion.Platforms)
			{
				if (string.IsNullOrEmpty(platformName))
				{
					continue;
				}

				OrgModuleDownload download = ScriptableObject.CreateInstance<OrgModuleDownload>();
				download.ID = moduleId;
				download.VersionID = moduleVersion.Id ?? -1;
				download.Version = moduleVersion.Version;
				download.ApkName = moduleVersion.Package;
				download.Platform = platformName;
				download.Status = moduleVersion.Lifecycle?.Name;
				download.Lifecycle = moduleVersion.Lifecycle?.Stage ?? EModuleLifecycle.OTHER;
				downloads.Add(download);
			}

			return downloads;
		}

		private static List<PlatformPlayerDownload> ToPlatformPlayerDownloads(ModulePlayerVersion playerVersion)
		{
			List<PlatformPlayerDownload> downloads = new List<PlatformPlayerDownload>();
			if (playerVersion?.Platforms == null)
			{
				return downloads;
			}

			foreach (string platformName in playerVersion.Platforms)
			{
				if (string.IsNullOrEmpty(platformName))
				{
					continue;
				}

				PlatformPlayerDownload download = new PlatformPlayerDownload();
				download.Id = playerVersion.Id ?? -1;
				download.Version = playerVersion.Version;
				download.Status = playerVersion.Status;
				download.ApkName = playerVersion.Package;
				download.Platform = platformName;
				downloads.Add(download);
			}

			return downloads;
		}

		internal static string CapitalizeIndustry(string industry)
		{
			if (string.IsNullOrEmpty(industry))
			{
				return industry;
			}

			return char.ToUpper(industry[0], CultureInfo.InvariantCulture) + industry.Substring(1);
		}
	}

	/// <summary>Lenient readers for the core's JSON payloads: wrong types read as missing.</summary>
	internal static class JsonReading
	{
		public static JObject ParseObject(string jsonText)
		{
			if (string.IsNullOrEmpty(jsonText))
			{
				return new JObject();
			}

			try
			{
				return JObject.Parse(jsonText);
			}
			catch (JsonException)
			{
				return new JObject();
			}
		}

		public static string GetString(JObject container, string memberName)
		{
			JToken memberToken = container?[memberName];
			return memberToken != null && memberToken.Type == JTokenType.String ? memberToken.Value<string>() : null;
		}

		public static int? GetInteger(JObject container, string memberName)
		{
			return ToInteger(container?[memberName]);
		}

		public static int? ToInteger(JToken memberToken)
		{
			if (memberToken == null)
			{
				return null;
			}

			switch (memberToken.Type)
			{
				case JTokenType.Integer:
					return memberToken.Value<int>();
				case JTokenType.Float:
					return (int)Math.Round(memberToken.Value<double>());
				case JTokenType.String:
					bool parsed = int.TryParse(memberToken.Value<string>(), NumberStyles.Integer,
						CultureInfo.InvariantCulture, out int parsedValue);
					return parsed ? parsedValue : (int?)null;
				default:
					return null;
			}
		}

		public static double? GetNumber(JObject container, string memberName)
		{
			JToken memberToken = container?[memberName];
			if (memberToken == null)
			{
				return null;
			}

			bool isNumber = memberToken.Type == JTokenType.Integer || memberToken.Type == JTokenType.Float;
			return isNumber ? memberToken.Value<double>() : (double?)null;
		}

		public static bool? GetBoolean(JObject container, string memberName)
		{
			JToken memberToken = container?[memberName];
			bool isBoolean = memberToken != null && memberToken.Type == JTokenType.Boolean;
			return isBoolean ? memberToken.Value<bool>() : (bool?)null;
		}

		public static JObject GetObject(JObject container, string memberName)
		{
			return container?[memberName] as JObject;
		}

		public static JArray GetArray(JObject container, string memberName)
		{
			return container?[memberName] as JArray ?? new JArray();
		}

		public static IEnumerable<JObject> GetObjects(JObject container, string memberName)
		{
			foreach (JToken elementToken in GetArray(container, memberName))
			{
				if (elementToken is JObject elementObject)
				{
					yield return elementObject;
				}
			}
		}
	}
}
