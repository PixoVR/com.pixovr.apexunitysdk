using UnityEngine.Events;
using System.Net.Http;

namespace PixoVR.Apex.Events
{
    [System.Serializable]
    public class OnHttpResponseEvent : UnityEvent<HttpResponseMessage> { };
    [System.Serializable]
    public class OnApexFailureEvent : UnityEvent<FailureResponse> { };
    [System.Serializable]
    public class OnLoginSuccessEvent : UnityEvent<LoginResponseContent> { };
    [System.Serializable]
    public class OnGetUserSuccessEvent : UnityEvent<GetUserResponseContent> { };
}
