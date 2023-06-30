\page platform_sessions Session And Data Reporting

# Functions

There are 4 functions used for manipulating session and data reporting:

 - [ApexSystem::JoinSession()](@ref PixoVR::Apex::ApexSystem::JoinSession()) is used to indicate the start of a session. It also includes information about the module and user.
 - [ApexSystem::CompleteSession()](@ref PixoVR::Apex::ApexSystem::CompleteSession()) is used to indicate the end of a session. It also includes information about the user's session, such as length, completion status and score.
 - [ApexSystem::SendSimpleSessionEvent()](@ref PixoVR::Apex::ApexSystem::SendSimpleSessionEvent()) is used during a session to signal events or report module specific data. This is a simplified version of [SendSessionEvent()](@ref PixoVR::Apex::ApexSystem::SendSessionEvent()).
 - [ApexSystem::SendSessionEvent()](@ref PixoVR::Apex::ApexSystem::SendSessionEvent()) is used during a session to signal events or report module specific data. This requires nearly a fully described xAPI Statement to be passed in.

All events used for when these functions are called are the same for when calling Authentication functionality. To see how to use the events, see <a href="authentication.html#requesthandling">Handling Authentication API Responses</a> in the \ref authentication "Authentication" page.

# Handling Authentication API Responses

All Session Event responses do not contain important information other than to indicate a failure to reach the server or if there was an error in the format of the data once it reached the server.

The approach in the Unity Apex SDK is done through specific Success and Fail unity events.

If you want more information on the data types in the events, check out the sections below.

\ref httpresponsemessage HttpResponseMessage
\ref failureresponse FailureResponse

\section session Sessions

\subsection whatissessions What is a session?

A session is one complete play-through of single scenario. Different modules handle this in different ways, but the main
idea is that a session should be the scenario itself, not the process of selecting an experience. 

Some modules have a lobby that a user loads into after authentication and return to after running through a scenario.
Time in the lobby should not count towards any session- the session begins when you choose an experience from the lobby,
and the session ends when you finish the experience, either by completing it, or using a menu option to return to the lobby.

Other modules just have a front end menu, but the concept is the same- the session begins once you have selected the experience
from the menu.

Every time a user enters a scenario, (whether directly from the login screen, from a lobby, from a front-end menu, or from
any other system you have set up), you need to call [ApexSystem::JoinSession()](@ref PixoVR::Apex::ApexSystem::JoinSession()). Similarly, once they have finished a scenario you
need to call [ApexSystem::CompleteSession()](@ref PixoVR::Apex::ApexSystem::CompleteSession()).

\subsection joinSession JoinSession()

### Overview

The [ApexSystem::JoinSession()](@ref PixoVR::Apex::ApexSystem::JoinSession()) function should be called every time the user starts a new session.

 - Joins a user to a session for a given scenario within the module.
 - Auto-generates an xAPI statement and sends it to Apex as a \ref Apex::EventTypes::PIXOVR_SESSION_JOINED "PIXOVR_SESSION_JOINED" event.
 - Adds the given context extensions to the xAPI Statement context if it's not null.
 - Returns `FALSE` if there is no logged in user. Otherwise returns `TRUE`.
 - Will log a warning message if there is already a session in progress that hasn't been ended with a
   \ref Apex::EventTypes::PIXOVR_SESSION_COMPLETE "PIXOVR_SESSION_COMPLETE" event, sent via the [ApexSystem::CompleteSession()](@ref PixoVR::Apex::ApexSystem::CompleteSession()) function.

### Parameters

The [ApexSystem::JoinSession()](@ref PixoVR::Apex::ApexSystem::JoinSession()) function has 2 optional parameters:

 - `scenarioID:String` - Sets the ApexSystem::scenarioID, which is then used as part of the `id` property of
   the `object` object. If this is not set, then whatever the ApexSystem::scenarioID was previously set to will
   be used.
 - `contextExtension:Extension` - Allows you to include any custom extensions in the context object. This is
   used to include any additional information you want to report that is not in the standard JoinSession() report.
   See [Extensions](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#miscext) for more details.

### Handling Response

PixoVR::Apex::ApexSystem::OnJoinSessionSuccess() - This Delegate is called when the platform indicates that the new session was started.
 - `response:HttpResponseMessage` - Contains the [HTTP response message](@ref HttpResponseMessage)

PixoVR::Apex::ApexSystem::OnJoinSessionFailed() - Called when the user failed to start a new session.
 - `response:FailureResponse` - Contains the error code and error message for why the user failed to join the session.

In the [Example Code](#example-code2) section, you'll see C# examples on how to bind to the event delegates.

\subsection completeSession CompleteSession()

### Overview

The [ApexSystem::CompleteSession()](@ref PixoVR::Apex::ApexSystem::CompleteSession()) function should be called every time the user completes a session.

It:

 - Completes the current session.
 - Auto-generates an xAPI statement and sends it to Apex as a \ref Apex::EventTypes::PIXOVR_SESSION_COMPLETE "PIXOVR_SESSION_COMPLETE" event.
 - Adds the given context extensions to the xAPI Statement context if it's not null. 
 - Returns `FALSE` if there is no logged in user or current session.

### Parameters

The [ApexSystem::CompleteSession()](@ref PixoVR::Apex::ApexSystem::CompleteSession()) function has 1 required parameter and 2 optional parameters:

 - `currentSessionData:SessionData` - Contains scoring information as well as if the session was completed.
 - `contextExtension:Extension` - Use this parameter to add data to the [Context](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#context) in the xAPI structure.
 - `resultExtension:Extension` Use this parameter to add data to the [Result](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#result) in the xAPI structure.

### Handling Responses

PixoVR::Apex::ApexSystem::OnCompleteSessionSuccess() - Called when the platform indicates that the session was completed.
 - `response:HttpResponseMessage` - Contains the [HTTP response message](@ref HttpResponseMessage).

PixoVR::Apex::ApexSystem::OnCompleteSessionFailed() - Called when the user either had not started a session before calling this or passed invalid information.
 - `response:FailureResponse` - Contains the error code and error message for why the user failed to join the session.

In the [Example Code](#example-code2) section, you'll see C# examples on how to bind to the event delegates.

\subsection sendsimplesessionevent SendSimpleSessionEvent()

### Overview

The [ApexSystem::SendSimpleSessionEvent()](@ref PixoVR::Apex::ApexSystem::SendSimpleSessionEvent()) function is how you send any simplified data to Apex during a session.

It:
 - Constructs an xAPI statement from provided session data.
 - Sends an event with xAPI statement data.
 - Returns `FALSE` if there is no logged in user, if [ApexSystem::JoinSession()](@ref PixoVR::Apex::ApexSystem::JoinSession()) has not been called to start a session, if the `action` is null or an empty string.

### Parameters

The [ApexSystem::SendSimpleSessionEvent()](@ref PixoVR::Apex::ApexSystem::SendSimpleSessionEvent()) function has 2 required parameters and 1 optional parameter:

 - `action:string` - The name of the event that has occurred within the module. The `action` is the name of the [Verb](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#verb).
 - `targetObject:string` - The name of the object or person that the `action` is taking place against or on. The `targetObject` is the name of the [Activity](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#activity) and used in the activity ID.
 - `contextExtension:Extension` - Use this parameter to add data to the [Context](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#context) in the xAPI structure.

\ref PixoVR::Apex::ApexSystem::SendSimpleSessionEvent "SendSimpleSessionEvent" returns `false` if there `action` is null or an emptry string. In all other cases, `true` will be returned.

### Handling Responses

PixoVR::Apex::ApexSystem::OnSendEventSuccess() - Called when the platform indicates that the session event was sent successfully.
 - `response:HttpResponseMessage` - Contains the [HTTP response message](@ref HttpResponseMessage).

PixoVR::Apex::ApexSystem::OnSendEventFailed() - Called when the platform indicates that the session event was not received successfully, had invalid data or
the server was not reachable.
 - `response:FailureResponse` - Contains the error code and error message for why the user failed to send the session event.

In the [Example Code](#example-code2) section, you'll see C# examples on how to bind to the event delegates.

\subsection sendsessionevent SendSessionEvent()

### Overview

The [ApexSystem::SendSessionEvent()](@ref PixoVR::Apex::ApexSystem::SendSessionEvent())::SendSessionEvent() function is how you send any full or custom set of data to Apex during a session.

It:
 - Constructs an xAPI statement from provided session data.
 - Sends an event with xAPI statement data.
 - Returns `FALSE` if there is no logged in user, if [ApexSystem::JoinSession()](@ref PixoVR::Apex::ApexSystem::JoinSession()) has not been called to start a session, or if any of the following members in `eventStatement` are null:
   + `eventStatement.verb`
   + `eventStatement.verb.id`
   + `eventStatement.target`

### Parameters

The [ApexSystem::SendSessionEvent()](@ref PixoVR::Apex::ApexSystem::SendSessionEvent()) function has 1 required parameter:

 - `eventStatement:Statement` - Provides all the necessary data to generate the xAPI statement.

The `eventStatement` is an xAPI [Statement](https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#statement), which contains the whole structure within the required xAPI Spec.

### Handling Responses

PixoVR::Apex::ApexSystem::OnSendEventSuccess() - Called when the platform indicates that the session event was sent successfully.
 - `response:HttpResponseMessage` - Contains the [HTTP response message](@ref HttpResponseMessage).

PixoVR::Apex::ApexSystem::OnSendEventFailed() - Called when the platform indicates that the session event was not received successfully, had invalid data or
the server was not reachable.
 - `response:FailureResponse` - Contains the error code and error message for why the user failed to send the session event.

In the [Example Code](#example-code2) section, you'll see C# examples on how to bind to the event delegates.

\section example-code2 Example Code

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
