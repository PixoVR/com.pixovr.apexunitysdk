using System;

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
            return (Error == null || Message == null || HttpCode == null);
        }
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
    public class UserAccessResponseContent: IPlatformErrorable
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
}
