using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace PixoVR.Apex
{
    public interface IPlatformErrorable
    {
        public abstract bool HasErrored();
    }

    public interface IFailure
    {
        // Empty so we can check all failure types against this
    }

    [Serializable]
    public class FailureResponse : IFailure, IPlatformErrorable
    {
        public string Error;
        public string HttpCode;
        public string Message;

        public bool HasErrored()
        {
            bool hasErrored = false;
            bool hasErrorSetup = !(Error == null && Message == null && HttpCode == null);

            if (hasErrorSetup == false)
            {
                if (Error != null)
                {
                    if (Error.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                    {
                        hasErrored = true;
                    }
                    else
                    {
                        hasErrored = !string.IsNullOrEmpty(Error);
                    }
                }
            }

            return hasErrored;
        }
    }

    [Serializable]
    public class JoinSessionResponse : FailureResponse
    {
        public JObject Data;
        public int SessionId;

        public void ParseData()
        {
            IEnumerable<JProperty> dataPropertyEnumerator = Data.Properties();
            foreach (JProperty property in dataPropertyEnumerator)
            {
                if (property.Name.Equals("SessionId", StringComparison.OrdinalIgnoreCase))
                {
                    SessionId = JsonConvert.DeserializeObject<int>(property.Value.ToString());
                }
            }
        }
    }

    [Serializable]
    public class GetUserModulesResponse : IFailure, IPlatformErrorable
    {
        public string Error;
        public string HttpCode;
        public string Message;
        public JObject Data;
        public List<UserModulesData> ParsedData;

        public bool HasErrored()
        {
            return (Error.Equals("true", StringComparison.CurrentCultureIgnoreCase));
        }

        public void ParseData()
        {
            ParsedData = new List<UserModulesData>();
            IEnumerable<JProperty> dataPropertyEnumerator = Data.Properties();
            UserModulesData currentUser;
            foreach (JProperty currentProperty in dataPropertyEnumerator)
            {
                currentUser = new UserModulesData();

                currentUser.UserId = currentProperty.Name;
                currentUser.AvailableModules = JsonConvert.DeserializeObject<List<int>>(
                    currentProperty.Value.ToString()
                );

                ParsedData.Add(currentUser);
            }
        }
    }

    [Serializable]
    public class UserModulesData
    {
        public string UserId;
        public List<int> AvailableModules;

        public UserModulesData()
        {
            AvailableModules = new List<int>();
        }
    }

    [Serializable]
    public class UserModulesRequestData
    {
        [JsonProperty(PropertyName = "userIds")]
        public List<int> UserIds = new List<int>();
    }

    [Serializable]
    public class LoginData
    {
        public string Login;
        public string Password;

        public LoginData(string username, string password)
        {
            Login = username;
            Password = password;
        }
    }

    [Serializable]
    public class LoginResponseContent : IPlatformErrorable
    {
        public int ID;
        public int OrgId;
        public string First;
        public string Last;
        public string Email;
        public string Token;
        public string Role;
        public Organization Org;
        public int MinimumPassingScore;

        public bool HasErrored()
        {
            return (Email == null || Token == null);
        }

        public bool IsPlatformSuperadmin()
        {
            var isSuperAdmin = !String.IsNullOrEmpty(Role) && Role.Equals("superadmin", StringComparison.CurrentCultureIgnoreCase);
            var isPlatformOrg = Org != null && !String.IsNullOrEmpty(Org.Type) && Org.Type.Equals("platform", StringComparison.CurrentCultureIgnoreCase);
            return isPlatformOrg && isSuperAdmin;
        }
    }

    [Serializable]
    public class UserLoginResponseContent : IPlatformErrorable
    {
        public LoginResponseContent User;

        public bool HasErrored()
        {
            return User == null;
        }
    }

    [Serializable]
    public class UserAccessResponseContent : IPlatformErrorable
    {
        public int UserId = -1;
        public int ModuleId = -1;
        public bool Access;
        public int? PassingScore;

        public bool HasErrored()
        {
            return (UserId == -1 || ModuleId == -1);
        }
    }

    [Serializable]
    public class ActiveUserInformation
    {
        public LoginResponseContent User = null;
        public UserAccessResponseContent ModuleUserInformation = null;
    }

    [Serializable]
    public class Organization
    {
        public int ID;
        public string Name;
        public string Status;
        public string DownloadRegion;
        public string Type;
        public string HubLogoLink;
        public string PrimaryColor;
        public string SecondaryColor;
    }

    [Serializable]
    public class GetUserResponseContent : IPlatformErrorable
    {
        public int ID;
        public string First;
        public string Last;
        public string Email;
        public string Username;

        public bool HasErrored()
        {
            return (Email == null);
        }
    }

    [Serializable]
    public class GeneratedAssistedLogin : IPlatformErrorable
    {
        public AssistedLoginCode AssistedLogin;

        public bool HasErrored()
        {
            return (AssistedLogin == null);
        }
    }

    public class AssistedLoginCode
    {
        public string AuthCode;
        public string Expires;
    }

    [Serializable]
    public class SessionData
    {
        public float Score;
        public float ScaledScore;
        public float MinimumScore;
        public float MaximumScore;
        public int Duration;
        public bool Complete;
        public bool Success;

        public SessionData() { }

        public SessionData(float score, float scaled, float min, float max, int duration, bool completed, bool success)
        {
            Score = score;
            ScaledScore = scaled;
            MinimumScore = min;
            MaximumScore = max;
            Duration = duration;
            Complete = completed;
            Success = success;
        }
    }

#if UNITY_6000_0_OR_NEWER
    public class OrgModule : ScriptableObject, INotifyBindablePropertyChanged
#else
    [Serializable]
    public class OrgModule
#endif
    {
        public int ID = -1;
        public string Name = "";
        public string Description = "";
        public string ShortDescription = "";
        public string LongDescription = "";
        public string Industry = "";
        public string Details = "";
        public string IconURL = "";
        public string AvailableLanguages = "";
        public string Distributor = "";
        public string UserGuideLink = "";
        public bool IsAuthenticatedLaunch = false;
        public bool EnableDebug = false;

        private Texture2D _thumbnail;

        [CreateProperty]
        public Texture2D Thumbnail
        {
            get { return _thumbnail; }
            set
            {
                _thumbnail = value;
#if UNITY_6000_0_OR_NEWER
                Notify();
#endif
            }
        }

        public List<OrgModuleDownload> Downloads = new List<OrgModuleDownload>();
        public int? PassingScore = null;
        public string Categories = "";
        public string externalId = "";
        public PlatformPlayer player = null;

#if UNITY_6000_0_OR_NEWER
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
#endif

        public OrgModule()
        {
            Downloads = new List<OrgModuleDownload>();
        }

        public OrgModule(JToken token)
        {
            Downloads = new List<OrgModuleDownload>();
            Parse(token);
        }

        public void Parse(JToken token)
        {
            ID = token.Value<int>("ID");
            Name = token.Value<string>("Name");
            Description = token.Value<string>("Description");
            ShortDescription = token.Value<string>("ShortDescription");
            LongDescription = token.Value<string>("LongDescription");
            Details = token.Value<string>("Details");
            IconURL = token.Value<string>("IconURL");
            PassingScore = token.Value<int?>("PassingScore");
            Categories = token.Value<string>("Categories");
            externalId = token.Value<string>("externalId");
            UserGuideLink = token.Value<string>("UserGuideLink");

            var PlayerToken = token.Value<JObject>("player");

            if (PlayerToken != null)
            {
                player = new PlatformPlayer(PlayerToken);
            }

            var DownloadTokens = token.Value<JArray>("Downloads");

            if (DownloadTokens != null)
            {
                foreach (JToken DownloadToken in DownloadTokens)
                {
                    var downloadData = ScriptableObject.CreateInstance<OrgModuleDownload>();
                    downloadData.Parse(DownloadToken);
                    Downloads.Add(downloadData);
                }
            }

            var availableLanguages = GetValue<JArray>(token, "availableLanguages");

            if (availableLanguages != null)
            {
                var availableLanguagesList = new List<string>();

                foreach (JToken languageToken in availableLanguages)
                {
                    string displayName = GetValue<string>(languageToken, "displayName");
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        availableLanguagesList.Add(displayName);
                    }
                }

                AvailableLanguages = string.Join(", ", availableLanguagesList);
            }

            var industry = GetValue<string>(token, "Industry");
            if (!string.IsNullOrEmpty(industry) && industry.Length > 0)
            {
                Industry = ModuleExtensions.CapitalizeIndustry(industry);
            }

            var distributor = GetValue<JToken>(token, "distributor");
            if (distributor != null)
            {
                Distributor = GetValue<string>(distributor, "name");
            }

            EnableDebug = token.Value<bool>("enableDebug");
            IsAuthenticatedLaunch = token.Value<bool>("isAuthenticatedLaunch");
        }

        private T GetValue<T>(JToken token, string propertyName, T defaultValue = default)
        {
            return token[propertyName] != null ? token.Value<T>(propertyName) : defaultValue;
        }

#if UNITY_6000_0_OR_NEWER
        void Notify([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
#endif
    }

    [Serializable]
    public class OrgModuleDownload : ScriptableObject
    {
        public int ID;
        public int VersionID;
        public string FileLocation;
        public string Version;
        public string Platform;
        public long DownloadSize;
        public string ApkName;
        public string URL;
        public string Status;
        public string externalId;

        public OrgModuleDownload(JToken token)
        {
            Parse(token);
        }

        public void Parse(JToken token)
        {
            ID = token.Value<int>("ID");
            VersionID = token.Value<int>("VersionID");
            DownloadSize = token.Value<long>("DownloadSize");
            externalId = token.Value<string>("externalId");
            FileLocation = token.Value<string>("FileLocation");
            Version = token.Value<string>("Version");
            Platform = token.Value<string>("Platform");
            ApkName = token.Value<string>("ApkName");
            URL = token.Value<string>("URL");
            Status = token.Value<string>("Status");
        }
    }

    #region Platform Models
    [Serializable]
    public class PlatformPlayer
    {
        public int id;
        public string name;
        public string description;
        public int distributorId;
        public string launchProtocol;
        public List<PlatformPlayerDownload> versions = new List<PlatformPlayerDownload>();

        public PlatformPlayer(int id)
        {
            this.id = id;
        }

        public PlatformPlayer(JObject tokenObject)
        {
            id = tokenObject.Value<int>("id");
            distributorId = tokenObject.Value<int>("distributorId");
            name = tokenObject.Value<string>("name");
            description = tokenObject.Value<string>("description");
            launchProtocol = tokenObject.Value<string>("launchProtocol");

            var versionTokens = tokenObject.Value<JArray>("versions");
            if (versionTokens == null)
            {
                versionTokens = new JArray();
            }

            foreach (JToken Version in versionTokens)
            {
                versions.Add(new PlatformPlayerDownload(Version));
            }
        }
    }

    [Serializable]
    public class PlatformPlayerDownload
    {
        public int id;
        public string version;
        public int modulePlayerId;
        public string status;
        public string URL;
        public string ApkName;
        public string platform;

        public PlatformPlayerDownload() { }

        public PlatformPlayerDownload(JToken token)
        {
            id = token.Value<int>("id");
            modulePlayerId = token.Value<int>("modulePlayerId");
            version = token.Value<string>("version");
            status = token.Value<string>("status");
            URL = token.Value<string>("URL");
            ApkName = token.Value<string>("ApkName");
            platform = token.Value<string>("platform");
        }
    }


    [Serializable]
    public class PlatformLoginResponse : IPlatformErrorable
    {
        public string Token { get; set; }
        public string Msg { get; set; }
        public User User { get; set; }

        public bool HasErrored()
        {
            return (User == null || string.IsNullOrEmpty(Token));
        }
    }

    [Serializable]
    public class User
    {
        public int Id { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Role { get; set; }
        public string[] Permissions { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public string ExternalId { get; set; }
        public DateTime PasswordExpDate { get; set; }
        public Organization Org { get; set; }
        public int OrgId { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string AuthToken { get; set; }
    }


    [Serializable]
    public class QuickIDAuthGetUsersResponse : IPlatformErrorable
    {
        public OrgProperties OrgProperties;
        public List<QuickIDUser> QuickIDUsers;

        public bool HasErrored()
        {
            return QuickIDUsers == null || OrgProperties == null;
        }
    }

    public class OrgProperties
    {
        public string PrimaryColor;
        public string SecondaryColor;
        public string HubLogoURL;
        public string OrgName;

        public OrgProperties() { }
        public OrgProperties(string primaryColor, string secondaryColor, string hubLogoURL, string orgName)
        {
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
            HubLogoURL = hubLogoURL;
            OrgName = orgName;
        }
    }

    public class QuickIDUser
    {
        public string FirstName;
        public string LastName;
        public string Username;
        public string Email;

        public QuickIDUser() { }

        public QuickIDUser(string firstName, string lastName, string username, string email)
        {
            FirstName = firstName;
            LastName = lastName;
            Username = username;
            Email = email;
        }
    }

    [Serializable]
    public class QuickIDLoginData
    {
        public string Username;
        public string SerialNumber;

        public QuickIDLoginData(string serialNumber, string username)
        {
            SerialNumber = serialNumber;
            Username = username;
        }
    }

    [Serializable]
    public class UserModulesResponse
    {
        public List<Module> modules;
    }

    [Serializable]
    public class Module
    {
        public string id;
        public string abbreviation;
        public string externalId;
        public string imageLink;
        public string developer;
        public string description;
        public string shortDesc;
        public string longDesc;
        public string industry;
        public string details;
        public string categories;
        public bool isAvailable;
        public bool isAuthenticatedLaunch;
        public List<ModuleLanguage> availableLanguages;
        public ModulePlayer modulePlayer;
        public List<ModuleVersion> versions;
    }

    [Serializable]
    public class ModuleLanguage
    {
        public string displayName;
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
            var orgModule = ScriptableObject.CreateInstance<OrgModule>();
#else
            var orgModule = new OrgModule();
#endif

            orgModule.Downloads ??= new List<OrgModuleDownload>();
            orgModule.ID = ParseID(module.id);
            orgModule.Name = module.description;
            orgModule.Description = module.description;
            orgModule.ShortDescription = module.shortDesc;
            orgModule.LongDescription = module.longDesc;
            orgModule.Industry = CapitalizeIndustry(module.industry) ?? string.Empty;
            orgModule.Details = module.details;
            orgModule.Categories = module.categories;
            orgModule.externalId = module.externalId;
            orgModule.IconURL = module.imageLink;
            orgModule.Distributor = module.developer;
            orgModule.IsAuthenticatedLaunch = module.isAuthenticatedLaunch;
            orgModule.AvailableLanguages = ToAvailableLanguages(module.availableLanguages);

            if (module.modulePlayer != null)
            {
                orgModule.player = new PlatformPlayer(ParseID(module.modulePlayer.id))
                {
                    name = module.modulePlayer.name,
                    launchProtocol = module.modulePlayer.launchProtocol
                };

                if (module.modulePlayer.versions != null)
                {
                    foreach (ModulePlayerVersion version in module.modulePlayer.versions)
                    {
                        orgModule.player.versions.AddRange(ToPlatformPlayerDownloads(version));
                    }
                }
            }

            if (module.versions != null)
            {
                foreach (ModuleVersion version in module.versions)
                {
                    orgModule.Downloads.AddRange(ToOrgModuleDownloads(orgModule.ID, version));
                }
            }

            return orgModule;
        }

        public static List<OrgModule> ToOrgModules(this List<Module> modules)
        {
            var orgModules = new List<OrgModule>();
            if (modules == null)
            {
                return orgModules;
            }

            foreach (Module module in modules)
            {
                var orgModule = module.ToOrgModule();
                if (orgModule != null)
                {
                    orgModules.Add(orgModule);
                }
            }

            return orgModules;
        }

        private static List<OrgModuleDownload> ToOrgModuleDownloads(int moduleID, ModuleVersion version)
        {
            var downloads = new List<OrgModuleDownload>();
            if (version?.platforms == null)
            {
                return downloads;
            }

            foreach (Platform platform in version.platforms)
            {
                if (platform == null)
                {
                    continue;
                }

                var download = ScriptableObject.CreateInstance<OrgModuleDownload>();
                download.ID = moduleID;
                download.VersionID = ParseID(version.id);
                download.Version = version.version;
                download.DownloadSize = version.fileSize;
                download.URL = version.fileLink;
                download.Platform = string.IsNullOrEmpty(platform.shortName) ? platform.name : platform.shortName;
                download.Status = version.lifecycle?.name;
                downloads.Add(download);
            }

            return downloads;
        }

        private static List<PlatformPlayerDownload> ToPlatformPlayerDownloads(ModulePlayerVersion version)
        {
            var downloads = new List<PlatformPlayerDownload>();
            if (version?.platforms == null)
            {
                return downloads;
            }

            foreach (Platform platform in version.platforms)
            {
                if (platform == null)
                {
                    continue;
                }

                downloads.Add(new PlatformPlayerDownload
                {
                    id = ParseID(version.id),
                    version = version.version,
                    status = version.status,
                    URL = version.fileLink,
                    platform = string.IsNullOrEmpty(platform.shortName) ? platform.name : platform.shortName
                });
            }

            return downloads;
        }

        private static string ToAvailableLanguages(List<ModuleLanguage> languages)
        {
            if (languages == null)
            {
                return string.Empty;
            }

            var displayNames = new List<string>();
            foreach (ModuleLanguage language in languages)
            {
                if (!string.IsNullOrEmpty(language?.displayName))
                {
                    displayNames.Add(language.displayName);
                }
            }

            return string.Join(", ", displayNames);
        }

        internal static string CapitalizeIndustry(string industry)
        {
            if (string.IsNullOrEmpty(industry))
            {
                return industry;
            }

            return char.ToUpper(industry[0]) + (industry.Length > 1 ? industry[1..] : string.Empty);
        }

        private static int ParseID(string id)
        {
            return int.TryParse(id, out int parsedID) ? parsedID : -1;
        }
    }

    [Serializable]
    public class ModulePlayer
    {
        public string id;
        public string name;
        public string launchProtocol;
        public List<ModulePlayerVersion> versions;
    }

    [Serializable]
    public class ModulePlayerVersion
    {
        public string id;
        public string version;
        public string status;
        public string fileLink;
        public long fileSize;
        public List<Platform> platforms;
    }

    [Serializable]
    public class ModuleVersion
    {
        public string id;
        public string version;
        public string fileLink;
        public long fileSize;
        public List<Control> controls;
        public List<Platform> platforms;
        public Lifecycle lifecycle;
    }

    [Serializable]
    public class Control
    {
        public string id;
        public string name;
    }

    [Serializable]
    public class Platform
    {
        public string id;
        public string name;
        public string shortName;
    }

    [Serializable]
    public class Lifecycle
    {
        public string id;
        public string name;
    }

    [Serializable]
    public class UserMetricsResponse : IFailure, IPlatformErrorable
    {
        public List<UserMetric> result;
        public PageInfo pageInfo;

        public bool HasErrored()
        {
            return (result == null || result.Count <= 0);
        }
    }


    [Serializable]
    public class PageInfo
    {
        public int totalCount;
        public int page;
        public int offset;
        public int pageSize;
        public int? previousPage;
        public int? nextPage;

        public int GetLastPageNumber()
        {
            var lastPage = 1;
            if (pageSize > 0)
            {
                lastPage = (int)Math.Ceiling((double)totalCount / pageSize);
            }
            return lastPage;
        }
    }

    public enum UserRoles
    {
        [Description("superadmin")]
        Superadmin = 0,
        [Description("admin")]
        Admin = 1,
        [Description("manager")]
        Manager = 2,
        [Description("developer")]
        Developer = 3,
        [Description("user")]
        User = 4,
        [Description("student")]
        Student = 5,
        [Description("trial")]
        Trial = 6
    }

    [Serializable]
    public class UserMetric
    {
        public int id;
        public string firstName;
        public string lastName;
        public string username;
        public string email;
        public string role;
        public Organization org;

        public DateTime createdAt;
        public int? orgUnitId;
        public OrgUnit orgUnit;
        public int? lastModuleId;
        public Module lastModule;
        public int sessionCount;
        public DateTime? lastActiveAt;
        public bool isInModule;
        public string passcode;

        // =============================
        // UI Toolkit bindable fields
        // =============================

        [SerializeField] private string displayName;
        [SerializeField] private string usernameEmail;
        [SerializeField] private string lastActiveDisplay;
        [SerializeField] private string lastSessionDisplay;
        [SerializeField] private string createdAtDisplay;

        // =============================
        // Read-only public accessors
        // =============================

        public string DisplayName => displayName;
        public string UsernameEmail => usernameEmail;
        public string LastActiveDisplay => lastActiveDisplay;
        public string LastSessionDisplay => lastSessionDisplay;
        public string CreatedAtDisplay => createdAtDisplay;


        // =============================
        // Call when data changes
        // =============================

        public void RefreshDisplayFields()
        {
            // Display name
            displayName = $"{firstName} {lastName}";

            // Username / email
            usernameEmail = string.IsNullOrWhiteSpace(email)
                ? username
                : $"{username} ({email})";

            // Last active
            lastActiveDisplay = lastActiveAt.GetLocalFormattedDateTime();

            // Last session
            if (isInModule)
            {
                if (lastModule == null)
                {
                    lastSessionDisplay = "In module...";
                }
                else if (lastModuleId == PixoPlatformModuleIDs.HUBAPP_MODULE_ID)
                {
                    lastSessionDisplay = "In Hub App";
                }
                else
                {
                    lastSessionDisplay =
                        $"In module {lastModule.description} ({lastModule.abbreviation}) - {lastActiveAt.GetLocalFormattedDateTime()}";
                }
            }
            else if (lastActiveAt != null)
            {
                lastSessionDisplay =
                    $"Session Completed - {lastActiveAt.GetLocalFormattedDateTime()}";
            }
            else
            {
                lastSessionDisplay = "No session";
            }


            // Created at
            createdAtDisplay = createdAt.GetLocalFormattedDateTime();
        }
    }

    [Serializable]
    public class OrgUnit
    {
        public int id;
        public string name;
        public string externalId;
    }

    [Serializable]
    public class Location
    {
        public string city;
        public string region;
        public string country;
    }

    [Serializable]
    public class Device
    {
        public int id;
        public string name;
        public string serial;
        public Location location;
        public int? batteryLevel;
        public bool online;
        public string model;
        public Module currentApp;
        public DeviceUser user;



        // =============================
        // UI Toolkit bindable fields
        // =============================

        [SerializeField] private string currentUserDisplay;
        [SerializeField] private string batteryLevelDisplay;
        [SerializeField] private string currentModuleDisplay;
        [SerializeField] private string locationDisplay;
        [SerializeField] private string currentUsernameEmailDisplay;


        public void RefreshDisplayFields()
        {
            // Display name
            if (!online)
            {
                currentUserDisplay = "Offline";
            }
            else
            {
                currentUserDisplay = "Available";
                if (user != null)
                {
                    currentUserDisplay = user.fullName;
                    currentUsernameEmailDisplay = String.IsNullOrWhiteSpace(user.email)
                        ? user.username
                        : $"{user.username} ({user.email})";

                }
            }

            if (online && currentApp != null)
            {

                if (currentApp.id == PixoPlatformModuleIDs.HUBAPP_MODULE_ID.ToString())
                {
                    currentModuleDisplay = "In Hub App";
                }
                else
                {
                    currentModuleDisplay =
                        $"In module - ({currentApp.abbreviation}) {currentApp.description}";
                }
            }

            batteryLevelDisplay = $"{(batteryLevel.HasValue ? batteryLevel.Value.ToString() + "%" : "N/A")}";

            locationDisplay = "Location Unknown";
            if (location != null)
            {
                locationDisplay = $"{location.city}, {location.region}, {location.country}";
            }
        }

    }


    [Serializable]
    public class DeviceUser
    {
        public string fullName;
        public string email;
        public string username;
    }

    [Serializable]
    public class OrgDevicesResponse : IFailure, IPlatformErrorable
    {
        public List<Device> result;
        public PageInfo pageInfo;

        public bool HasErrored()
        {
            return (result == null || result.Count <= 0);
        }
    }


    public class Session
    {
        public int id;
        public int userId;
        public int moduleId;
        public Module module;

        public float? rawScore;
        public float? maxScore;
        public float scaledScore;
        public string status;
        public string result;
        public DateTime startedAt;
        public DateTime? completedAt;


        // =============================
        // UI Toolkit bindable fields
        // =============================

        [SerializeField] private string sessionModuleDisplay;
        [SerializeField] private string sessionActiveStatusDisplay;


        public void RefreshDisplayFields()
        {
            var durationFormatted = GetDurationFormatted();
            sessionActiveStatusDisplay = String.Format("In Session For {0}", durationFormatted);
            if (isComplete())
            {
                sessionActiveStatusDisplay = String.Format("Completed Session - {0}", durationFormatted);
            }

            sessionModuleDisplay = module == null ? "Unknown Module" : String.Format("({0}) {1}", module.abbreviation, module.description);
            if (isComplete())
            {
                sessionModuleDisplay = String.Format(
                    "{0} - {1}",
                    sessionModuleDisplay,
                    completedAt.GetLocalFormattedDateTime()
                );
            }
        }

        public bool isComplete()
        {
            return completedAt.HasValue;
        }

        private string GetDurationFormatted()
        {
            var endTime = isComplete() ? completedAt.Value : DateTime.UtcNow;
            var timeSpan = endTime.Subtract(startedAt);

            var totalHours = (int)timeSpan.TotalHours;
            if (totalHours > 0)
            {
                return $"{totalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
            return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
    }

    public class SessionHistoryResponse : IFailure, IPlatformErrorable
    {
        public List<Session> result;
        public PageInfo pageInfo;
        public bool HasErrored()
        {
            return (result == null || result.Count <= 0);
        }
    }

    public class SessionFilters
    {
        public List<int> orgIDs;
        public List<int> userIDs;
    }
    #endregion


    public class FilterParams
    {
        public string searchText = "";
        public string sortField;
        public SortOrder sortOrder = SortOrder.Ascending;
        public enum SortOrder
        {
            Ascending,
            Descending
        }
    }



}
