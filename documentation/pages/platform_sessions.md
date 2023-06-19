\page platform_sessions Session & Data Tracking

To track the users progression through a Session and information, a module needs to send Session Events.
There are multiple kind of Session Events and serve specific purposes listed in the Session Event Types section.

The ApexSDK does help with sending session events to the PixoVR Platform, such as including information about the user, module and device.
However, all other information will need to be provided by the module through the function inputs. This data is formatted using the xAPI Standard.

To get a better understanding of the xAPI Standard, visit the <a href='https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#parttwo'>xAPI Spec</a>.

# Session Event Types

There are currently three types of Session Events.

## Session Join

Session Join is used to denote the start of a users session. Using this event will always start a new session, even if the prior session was not completed or ended. It should only be called once per module session.

\code{.cs}
public static bool JoinSession(string scenarioID, Extension contextExtension);
\endcode

*(Optional)* The `scenarioID` is up to the developer of the module to describe.
*(Optional)* `contextExtension` is packaged as part of the [Context](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#context) in the xAPI structure. The `contextExtension` is an xAPI [Extensions](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#miscext) object.

JoinSession returns `false` if there is no current user logged in. In all other cases, `true` will be returned.

There will be a message logged if JoinSession is called while there is already a session in progress.

## Session Complete

Session Complete is used to denote the completion and end of a users session. This will flag a completed session on the platform. This event also contains information on if the user completed the module, passed the module and scoring of the module. It should only be called once per module session.

\code{.cs}
public static bool CompleteSession(SessionData currentSessionData, Extension contextExtension, Extension resultExtension);
\endcode

`currentSessionData` contains scoring information as well as if the session was completed.
*(Optional)* `contextExtension` is packaged as part of the [Context](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#context) in the xAPI structure. The `contextExtension` is an xAPI [Extensions](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#miscext) object.
*(Optional)* `resultExtension` is packaged as part of the [Result](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#result) in the xAPI structure. The `resultExtension` is an xAPI [Extensions](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#miscext) object.

CompleteSession returns `false` if there is no current user logged in or session in progress. In all other cases, `true` will be returned.

## Session Event

Session Event is a catch-all event. It helps capture data or events that the module wants to track. These can be customized with any information that will fit within the <a href='https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#parttwo'>xAPI Spec</a>.

The Apex SDK helps developers capture these events with two functions, allowing delivery of a very simple session event or a fully customized session event.

Simple session event:
\code{.cs}
public static bool SendSimpleSessionEvent(string action, string targetObject, Extension contextExtension);
\endcode

The `action` is the name of the event that has occurred within the module. The `action` is the name of the [Verb](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#verb).
The `targetObject` is the name of the object or person that the `action` is taking place against or on. The `targetObject` is the name of the [Activity](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#activity) and used in the activity ID.
*(Optional)* `contextExtension` is packaged as part of the [Context](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#context) in the xAPI structure. The `contextExtension` is an xAPI [Extensions](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#miscext) object.

SendSimpleSessionEvent returns `false` if there `action` is null or an emptry string. In all other cases, `true` will be returned.

Custom session event:
\code{.cs}
public static bool SendSessionEvent(Statement eventStatement);
\endcode

The `eventStatement` is an xAPI [Statement](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#statement), which contains the whole structure within the required xAPI Spec.

SendSessionEvent will return `false` if there is no current user logged in or session in progress.
SendSessionEvent will return `false` if the `eventStatement` is null.
SendSessionEvent will return `false` if the `eventStatement` has a null [Verb](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#verb).
SendSessionEvent will return `false` if the `eventStatement` has a [Verb](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#verb) with no ID.
SendSessionEvent will return `false` if the `eventStatement` has a null [Target](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#object).
SendSessionEvent will return `true` in all other cases.

# Handling Authentication API Responses

All Session Event responses do not contain important information other than to indicate a failure to reach the server or if there was an error in the format of the data once it reached the server.
The approach in the Unity Apex SDK is done through specific Success and Fail unity events.

If you want more information on the data types in the events, check out the sections below.

\section HttpResponseMessage HttpResponseMessage
\section FailureResponse FailureResponse

## Session Join

ApexSystem::OnJoinSessionSuccess(HttpResponseMessage response) - This Delegate is called when the platform indicates that the session joined was sent successfully.
`response` contains the HTTP response.

For more information on what is in `responseMessage`, checkout the [HttpResponseMessage](@ref HttpResponseMessage) class.

ApexSystem::OnJoinSessionFailed(FailureResponse response)
`response` contains the error code and error message for why the user failed to join the session.

For more information on what is in `response`, checkout the [FailureResponse](@ref FailureResponse) class.

## Session Complete

ApexSystem::OnCompleteSessionSuccess(HttpResponseMessage response) - This Delegate is called when the platform indicates that the session completed was sent successfully.
`response` contains the HTTP response.

For more information on what is in `responseMessage`, checkout the [HttpResponseMessage](@ref HttpResponseMessage) class.

ApexSystem::OnCompleteSessionFailed(FailureResponse response)
`response` contains the error code and error message for why the user failed to complete the session.

For more information on what is in `response`, checkout the [FailureResponse](@ref FailureResponse) class.

## Session Event

ApexSystem::OnSendEventSuccess(HttpResponseMessage response) - This Delegate is called when the platform indicates that the session event was sent successfully.
`response` contains the HTTP response.

For more information on what is in `responseMessage`, checkout the [HttpResponseMessage](@ref HttpResponseMessage) class.

ApexSystem::OnSendEventFailed(FailureResponse response)
`response` contains the error code and error message for why the user failed to send the session event.

For more information on what is in `response`, checkout the [FailureResponse](@ref FailureResponse) class.

In the [Example Code](#example-code) section, you'll see C# examples on how to bind to the event delegates.

# Example Code

## Calling JoinSession

\code{.cs}
var contextExtension = new PixoVR.Apex.XAPI.Extension();
contextExtension.AddSimple("Session Start Time", $"{DateTime.UtcNow}");
contextExtension.AddSimple("UserName", $"Hello World");
contextExtension.AddSimple("Mode", $"Mode");
ApexSystem.JoinSession("Demo", contextExtension);
\endcode

## Calling CompleteSession

\code{.cs}
float raw = (float)System.Convert.ToDouble(RawScoreInput.text);
float scaled = (float)System.Convert.ToDouble(ScaledScoreInput.text);
float min = (float)System.Convert.ToDouble(MinScoreInput.text);
float max = (float)System.Convert.ToDouble(MaxScoreInput.text);
int duration = System.Convert.ToInt32(DurationInput.text);
bool moduleComplete = true;
bool userPassed = scaled > min;
ApexSystem.CompleteSession(new SessionData(raw, scaled, min, max, duration, moduleComplete, userPassed));
\endcode

## Calling SendSimpleSessionEvent

\code{.cs}
ApexSystem.SendSimpleSessionEvent("Demo Session Event", "User", null);
\endcode

## Binding to Session Event Success/Failed Events

\code{.cs}
void Start()
{
  ApexSystem.Instance.OnJoinSessionSuccess.AddListener(OnSessionJoinedSuccess);
  ApexSystem.Instance.OnJoinSessionFailed.AddListener(OnSessionJoinedFailed);
  ApexSystem.Instance.OnCompleteSessionSuccess.AddListener(OnSessionCompletedSuccess);
  ApexSystem.Instance.OnCompleteSessionFailed.AddListener(OnSessionCompletedFailed);
  ApexSystem.Instance.OnSendEventSuccess.AddListener(OnSendSessionEventSuccess);
  ApexSystem.Instance.OnSendEventFailed.AddListener(OnSendSessionEventFailed);
}

void OnSessionJoinedSuccess(HttpResponseMessage joinResponse)
{
  Debug.Log("Session joined successfully.");
}

void OnSessionJoinedFailed(FailureResponse failedLoginResponse)
{
  Debug.Log("Session was not joined.");
}

void OnSessionCompletedSuccess(HttpResponseMessage joinResponse)
{
  Debug.Log("Session completed successfully.");
}

void OnSessionCompletedFailed(FailureResponse failedLoginResponse)
{
  Debug.Log("Session was not completed.");
}

void OnSendSessionEventSuccess(HttpResponseMessage joinResponse)
{
  Debug.Log("Session event sent successfully.");
}

void OnSendSessionEventFailed(FailureResponse failedLoginResponse)
{
  Debug.Log("Session event failed to send.");
}
\endcode