using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
    public class OrgModule : ScriptableObject
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
                Industry = char.ToUpper(industry[0]) + (industry.Length > 1 ? industry[1..] : string.Empty);
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

        public int? GetLastPageNumber()
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
            lastActiveDisplay = lastActiveAt?.ToString("hh:mm tt");

            // Last session
            var dateFormat = lastActiveAt?.Date == DateTime.Now.Date
                ? "hh:mm tt"
                : "MM/dd/yyyy hh:mm tt";

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
                        $"In module {lastModule.description} ({lastModule.abbreviation}) - {lastActiveAt?.ToString(dateFormat)}";
                }
            }
            else if (lastActiveAt != null)
            {
                lastSessionDisplay =
                    $"Session Completed - {lastActiveAt?.ToString(dateFormat)}";
            }
            else
            {
                lastSessionDisplay = "No session";
            }


            // Created at
            createdAtDisplay = createdAt.ToString(dateFormat);
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
    public class Module
    {
        public int id;
        public string abbreviation;
        public string description;
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
                }
            }

            if (online && currentApp != null)
            {

                if (currentApp.id == PixoPlatformModuleIDs.HUBAPP_MODULE_ID)
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
        public string username;
        public string firstName;
        public string lastName;
        public string organization;

        public string module;
        public string orgUnit;
        public int eventCount;
        public float? rawScore;
        public float? maxScore;
        public float scaledScore;
        public string status;
        public string result;
        public DateTime createdAt;
        public DateTime startedAt;
        public DateTime? completedAt;

        /// <summary>
        /// Duration of the session in seconds
        /// </summary>
        public int duration;
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
