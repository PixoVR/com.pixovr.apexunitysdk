using PixoVR.Apex;
using PixoVR.Apex.XAPI;
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
        //contextExtension.Add(new Uri("https://apexurldemo.com/xapi/extension/extra_context_extension"), "This is a test!");
        contextExtension.Add("https://apexurldemo.com/xapi/extension/extra_context_extension", "This is a test!");
        contextExtension.AddSimple("demo_extension", "APEX");
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
        resultExtension.Add("https://apexurldemo.com/xapi/extension/score_average", "99.99");
        resultExtension.AddSimple("expected_score_prediction", "100");
        ApexSystem.CompleteSession(new SessionData(raw, scaled, min, max, duration, CompleteToggle.isOn, SuccessToggle.isOn), resultExtension: resultExtension);
    }

    public void SendSessionEvent()

    {
        /*
        Full Report
        *
        TinCan.Statement eventStatement = new TinCan.Statement();
        // Verb
        eventStatement.verb = new TinCan.Verb();
        eventStatement.verb.id = new Uri("https://pixovr.com/xapi/verbs/reported");
        eventStatement.verb.display = new TinCan.LanguageMap();
        eventStatement.verb.display.Add("en","Reported");
        eventStatement.verb.display.Add("es","Reportado");

        //Object
        //Note: Activity is one of four ObjectTypes, but the one that 99% of statements on
        //Apex should use.
        //Note the TinCan API calls Objects "Targets"
        TinCan.Activity eventActivity = new TinCan.Activity();
        eventActivity.id = "https://pixovr.com/xapi/currentmodule/exampleObject";
        eventStatement.target = eventActivity;

        //Result
        eventStatement.result = new TinCan.Result();
        eventStatement.result.completion = true; //did they complete the event
        eventStatement.result.success = true; //did they get a passing score or otherwise succeed at the event
        eventStatement.result.duration = TimeSpan.FromSeconds(15);//how long did they spend on the event
        eventStatement.result.response = "answer"; //how they answered a question, or otherwise responded
        eventStatement.result.score = new TinCan.Score();
        eventStatement.result.score.max = 100;
        eventStatement.result.score.min = 0;
        eventStatement.result.score.raw = 80;
        eventStatement.result.score.scaled = 0.8;

        //Context
        eventStatement.context = new TinCan.Context(); 
        Extension contextExtension = new Extension();
        contextExtension.Add("https://www.pixovr.com/xapi/extensions/iri_extension","value");
        contextExtension.AddSimple("simple_key", "value");
        eventStatement.context.extensions = new TinCan.Extensions(contextExtension.ToJObject());

        ApexSystem.SendSessionEvent(eventStatement);

        */

        /*
        Minimal Report
        */


        TinCan.Statement eventStatement = new TinCan.Statement();
        eventStatement.verb = new TinCan.Verb();
        eventStatement.verb.id = new Uri("https://pixovr.com/xapi/verbs/exampleVerb");

        //the target is equivalent to the object in the xAPI specificiation
        TinCan.Activity eventActivity = new TinCan.Activity();
        eventActivity.id = "https://pixovr.com/xapi/exampleProject/objects/exampleObject";
        eventStatement.target = eventActivity; 
        ApexSystem.SendSessionEvent(eventStatement);
        
    }
}
