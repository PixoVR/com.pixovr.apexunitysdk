using System;

namespace PixoVR.Apex
{
    public interface IApexErrorable
    {
        public abstract bool HasErrored();
    }

    public interface IFailure
    {
        // Empty so we can check all failure types against this
    }

    [Serializable]
    public class FailureResponse : IFailure, IApexErrorable
    {
        public string Error;
        public string Message;

        public bool HasErrored()
        {
            return (Error == null || Message == null);
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
    public class LoginResponseContent : IApexErrorable
    {
        public int ID;
        public int OrgId;
        public string First;
        public string Last;
        public string Email;
        public string Token;
        public Organization Org;

        public bool HasErrored()
        {
            return (Email == null || Token == null);
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
    public class GetUserResponseContent : IApexErrorable
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
