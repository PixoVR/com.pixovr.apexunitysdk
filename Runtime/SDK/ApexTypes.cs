using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Networking;
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
                if (Error != null && Error.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                {
                    hasErrored = true;
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
                currentUser.AvailableModules = JsonConvert.DeserializeObject<List<int>>(currentProperty.Value.ToString());

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
        public Organization Org;
        public int MinimumPassingScore;

        public bool HasErrored()
        {
            return (Email == null || Token == null);
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

    public class OrgModule : ScriptableObject, INotifyBindablePropertyChanged
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

        private Texture2D _thumbnail;
        [CreateProperty]
        public Texture2D Thumbnail
        {
            get
            {
                return _thumbnail;
            }
            set
            {
                _thumbnail = value;
                Notify();
            }
        }

        public List<OrgModuleDownload> Downloads = new List<OrgModuleDownload>();
        public int? PassingScore = null;
        public string Categories = "";
        public string externalId = "";
        public PlatformPlayer player = null;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;


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

				var availableLanguages = token.Value<JArray>("availableLanguages");

				if (availableLanguages != null)
				{
					var availableLanguagesList = new List<string>();

					foreach (JToken languageToken in availableLanguages)
					{
						availableLanguagesList.Add(languageToken.Value<string>("displayName"));
					}

					AvailableLanguages = string.Join(", ", availableLanguagesList);
				}

            var industry = token.Value<string>("Industry");
				if (industry != null)
				{
					Industry = industry.Substring(0, 1).ToUpper() + industry.Substring(1);
				}

				var distributor = token.Value<JObject>("distributor");
				if (distributor != null)
				{
					Distributor = distributor.Value<string>("name");
				}
        }

        void Notify([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }

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

    [Serializable]
    public class PlatformPlayer
    {
        public int id;
        public string name;
        public string description;
        public int distributorId;
        public List<PlatformPlayerDownload> versions = new List<PlatformPlayerDownload>();

        public PlatformPlayer(JObject tokenObject)
        {
            id = tokenObject.Value<int>("id");
            distributorId = tokenObject.Value<int>("distributorId");
            name = tokenObject.Value<string>("name");
            description = tokenObject.Value<string>("description");

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
}
