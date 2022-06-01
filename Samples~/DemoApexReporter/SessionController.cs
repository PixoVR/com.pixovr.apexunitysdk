using PixoVR.Apex;
using System.Net.Http;
using UnityEngine;
using UnityEngine.UI;

public class SessionController : MonoBehaviour
{
    public Button JoinSessionButton;
    public Button EndSessionButton;
    public InputField RawScoreInput;
    public InputField ScaledScoreInput;
    public InputField MinScoreInput;
    public InputField MaxScoreInput;
    public InputField DurationInput;
    public Toggle CompleteToggle;
    public Toggle SuccessToggle;

    void Start()
    {
        ApexSystem.Instance.OnLoginSuccess.AddListener(OnLoginSuccess);
        ApexSystem.Instance.OnLoginFailed.AddListener(OnLoginFailed);
        ApexSystem.Instance.OnJoinSessionSuccess.AddListener(OnSessionJoinedSuccess);
        ApexSystem.Instance.OnJoinSessionFailed.AddListener(OnSessionJoinedFailed);
        ApexSystem.Instance.OnCompleteSessionSuccess.AddListener(OnSessionCompletedSuccess);
        ApexSystem.Instance.OnCompleteSessionFailed.AddListener(OnSessionCompletedFailed);
        JoinSessionButton.interactable = false;
        EndSessionButton.interactable = false;
    }

    void OnLoginSuccess(LoginResponseContent loginResponse)
    {
        JoinSessionButton.interactable = true;
        EndSessionButton.interactable = false;
    }

    void OnLoginFailed(FailureResponse failedLoginResponse)
    {
        JoinSessionButton.interactable = false;
        EndSessionButton.interactable = false;
    }

    void OnSessionJoinedSuccess(HttpResponseMessage joinResponse)
    {
        Debug.Log("We joined a session!");
        EndSessionButton.interactable = true;

    }

    void OnSessionJoinedFailed(FailureResponse failedLoginResponse)
    {
        EndSessionButton.interactable = false;
    }

    void OnSessionCompletedSuccess(HttpResponseMessage joinResponse)
    {
        Debug.Log("We completed a session!");
        EndSessionButton.interactable = false;
    }

    void OnSessionCompletedFailed(FailureResponse failedLoginResponse)
    {
    }

    public void JoinSession()
    {
        ApexSystem.JoinSession();
    }

    public void CompleteSession()
    {
        float raw = (float)System.Convert.ToDouble(RawScoreInput.text);
        float scaled = (float)System.Convert.ToDouble(ScaledScoreInput.text);
        float min = (float)System.Convert.ToDouble(MinScoreInput.text);
        float max = (float)System.Convert.ToDouble(MaxScoreInput.text);
        int duration = System.Convert.ToInt32(DurationInput.text);
        ApexSystem.CompleteSession(new SessionData(raw, scaled, min, max, duration, CompleteToggle.isOn, SuccessToggle.isOn));
    }
}
