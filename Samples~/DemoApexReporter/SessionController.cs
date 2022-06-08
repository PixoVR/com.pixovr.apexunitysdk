using PixoVR.Apex;
using System;
using System.Net.Http;
using UnityEngine;
using UnityEngine.UI;

public class SessionController : MonoBehaviour
{
    public Button JoinSessionButton;
    public Button EndSessionButton;
    public Button SendEventButton;
    public InputField RawScoreInput;
    public InputField ScaledScoreInput;
    public InputField MinScoreInput;
    public InputField MaxScoreInput;
    public InputField DurationInput;
    public InputField EventNameInput;
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
        SendEventButton.interactable = false;
    }

    void OnLoginSuccess(LoginResponseContent loginResponse)
    {
        JoinSessionButton.interactable = true;
        EndSessionButton.interactable = false;
        SendEventButton.interactable = false;
    }

    void OnLoginFailed(FailureResponse failedLoginResponse)
    {
        JoinSessionButton.interactable = false;
        EndSessionButton.interactable = false;
        SendEventButton.interactable = false;
    }

    void OnSessionJoinedSuccess(HttpResponseMessage joinResponse)
    {
        Debug.Log("We joined a session!");
        EndSessionButton.interactable = true;
        SendEventButton.interactable = true;
    }

    void OnSessionJoinedFailed(FailureResponse failedLoginResponse)
    {
        EndSessionButton.interactable = false;
        SendEventButton.interactable = false;
    }

    void OnSessionCompletedSuccess(HttpResponseMessage joinResponse)
    {
        Debug.Log("We completed a session!");
        EndSessionButton.interactable = false;
        SendEventButton.interactable = false;
    }

    void OnSessionCompletedFailed(FailureResponse failedLoginResponse)
    {
    }

    public void JoinSession()
    {
        Extension contextExtension = new Extension();
        contextExtension.Add("https://apexurldemo.com/xapi/extension/extraContextExtension", "This is a test!");
        contextExtension.AddSimple("demoExtension", "APEX");
        ApexSystem.JoinSession(contextExtension: contextExtension);
    }

    public void CompleteSession()
    {
        float raw = (float)System.Convert.ToDouble(RawScoreInput.text);
        float scaled = (float)System.Convert.ToDouble(ScaledScoreInput.text);
        float min = (float)System.Convert.ToDouble(MinScoreInput.text);
        float max = (float)System.Convert.ToDouble(MaxScoreInput.text);
        int duration = System.Convert.ToInt32(DurationInput.text);

        Extension resultExtension = new Extension();
        resultExtension.Add("https://apexurldemo.com/xapi/extension/scoreAverage", "99.99");
        resultExtension.AddSimple("expectedScorePrediction", "100");
        ApexSystem.CompleteSession(new SessionData(raw, scaled, min, max, duration, CompleteToggle.isOn, SuccessToggle.isOn), resultExtension: resultExtension);
    }

    public void SendSessionEvent()
    {
        TinCan.Statement eventStatement = new TinCan.Statement();
        eventStatement.verb = new TinCan.Verb();
        string eventName = EventNameInput.text;
        string url = "https://pixovr.com/xapi/verbs/" + eventName;
        url = url.Replace(' ', '_').ToLower();
        eventStatement.verb.id = new Uri(url);
        eventStatement.verb.display = new TinCan.LanguageMap();
        eventStatement.verb.display.Add("en", eventName);

        TinCan.Activity activity = new TinCan.Activity();
        activity.id = string.Format("https://pixovr.com/xapi/objects/{0}/{1}", ApexSystem.ModuleID, eventName.Replace(" ", ""));
        eventStatement.target = activity;
        ApexSystem.SendSessionEvent(eventName, eventStatement);
    }
}
