# Getting Started

# Unity Plugin for the implementation of the Apex API

Sample provided to show Apex Unity SDK use examples in the `Samples` folder.

# Installing the Unity Package
To install the Apex Unity SDK, open Window > Package Manager.

Click the '+' in the top left corner and select 'Add package from disk...'

Navigate to where you've extracted the downloaded zip file and select package.json.

Now you should be able to see the package in the Package Manager, where you can install it into your project.

# Using the Apex Plugin

After installing the Apex Unity SDK, navigate to Runtime > Unity. Add the ApexSystem script to any object in the scene.
Any object that the [ApexSystem](@ref PixoVR::Apex::ApexSystem) script is added to will **NOT** be deleted between level changes.

The following properties will need to be set on the ApexSystem component once it's added to the scene.
- Platform Target Server
  + This fills in the server IP address that you want to send the information to.
  + Defaults to the North America - Production envrionment.
- Server IP
  + This is the Apex server IP address that you want to send the information to. This is filled in by the Platform Target Server property at run time.
- Module ID
  + This is the ID of the module that will be distributed to customers. This will be generated when you create a project on the Apex platform. This must be set for the data to be reported to the proper module.
- Module Name
  + This is the name of your module.
- Module Version
  + The version number for your module currently being distributed.
  + The Module Version is based on Semantic numbering with a Major, Minor and Patch number. Both Minor and Patch numbers are always 2 digits, using a 0 as the first value when the number is below 10. (e.g.) 1.03.08. This is enforced on the platform itself as well.
- Scenario ID
  + The name of the current scenario within your module that the user is taking part in. You can have multiple scenarios within a module.

The ApexSystem is a singleton, all functions are wrapped in static functions to make the use simpler.
If you want access to the response events in code, you will need to access the ApexSystem Instance.
  (e.g.) ApexSystem.Instance.OnPingSuccess.AddListener(YourFunctionHere);

# Understanding xAPI and TinCan.NET

Data sent to Apex is formatted using the xAPI Standard. The Unity Apex SDK Plugin utilizes TinCan.NET to ensure all data is xAPI Compliant.

To understand how TinCan.NET is used, visit the documentation [here](https://rusticisoftware.github.io/TinCan.NET/).
To get a better understanding of the xAPI Standard, visit the xAPI Spec [here](https://github.com/adlnet/xAPI-Spec).

# Requirements for building with the Apex Plugin

The projects **Api Compatbility Level** needs to be <b>.NET 4.x</b> or newer.

## Quick Function Explanation

All server functions have corresponding Success and Failed events.

### Ping

- PixoVR::Apex::ApexSystem::Ping() Contacts the Apex Server for status.
- PixoVR::Apex::ApexSystem::OnPingSuccess() is called when there is a successful response from the server.
- PixoVR::Apex::ApexSystem::OnPingFailed() is called when the server is not able to be reached.

### Login

Sends the user's information to the Apex Server to login. Returns false if the password or login are of zero length.

- PixoVR::Apex::ApexSystem::Login (LoginData login) : Boolean
- PixoVR::Apex::ApexSystem::Login(string username, string password) : Boolean
- PixoVR::Apex::ApexSystem::OnLoginSuccess() is called when the user's information is valid.
- PixoVR::Apex::ApexSystem::OnLoginFailed() is called when the user's information is not valid or when the server is not able to be reached.

### Join Session

PixoVR::Apex::ApexSystem::JoinSession(string scenarioID, Extension contextExtension) : Boolean

- Joins a user to a session for a given scenario within the module.
- Auto-generates an xAPI statement and sends it to Apex.
- Adds the given context extensions to the xAPI Statement context if it's not null.
- Returns false if there is no logged in user.
- Will throw an error if there is already a session in progress that hasn't been ended.
- PixoVR::Apex::ApexSystem::OnJoinSessionSuccess() is called when the user was able to join the session successfully.
- PixoVR::Apex::ApexSystem::OnJoinSessionFailed() is called when the user was not able to join the session or when the server is not able to be reached. Ensure the user has access to the given module.

### Complete Session

PixoVR::Apex::ApexSystem::CompleteSession(SessionData currentSessionData, Extension contextExtension = null, Extension resultExtension = null) : Boolean

- Completes the current session.
- Auto-generates an xAPI statement and sends it to Apex
- Adds the given context extensions to the xAPI Statement context if it's not null.
- Adds the given result extensions to the xAPI Statement result if it's not null.
- Returns false if there is no logged in user or current session.
- PixoVR::Apex::ApexSystem::OnCompleteSessionSuccess() is called when the session was completed successfully.
- PixoVR::Apex::ApexSystem::OnCompleteSessionFailed() is called when the session was not started, unable to be completed otherwise or when the server is not able to be reached.

### Send Session Event

PixoVR::Apex::ApexSystem::SendSessionEvent(TinCan.Statement eventStatement) : Boolean

- Constructs an xAPI statement from provided session data
- Sends an event with xAPI statement data.
- Returns false if there is no logged in user, current session or `eventStatement` is null.
- `TinCan.Statement` is a data structure compatible with the current xAPI standard.
- The actor information set within `eventStatement` will be set within `SendSessionEvent`.
- The context information for `registration`, `revision` and `platform` within `eventStatement` will be set within `SendSessionEvent`.
- PixoVR::Apex::ApexSystem::OnSendEventSuccess() is called when the event has been sent successfully.
- PixoVR::Apex::ApexSystem::OnSendEventFailed() is called when the session was not started, contains invalid information or when the server is not able to be reached.

### Send Simple Session Event

PixoVR::Apex::ApexSystem::SendSimpleSessionEvent(string action, string targetObject, Extension contextExtension) : Boolean

- Constructs an xAPI Statement from provided session data
- Sends an event with xAPI statement data formed from the current users information, `action` as the verb and `targetObject` as the activity/object.
- Returns false if there is no logged in user, there is no current session or no action is assigned.
- PixoVR::Apex::ApexSystem::OnSendEventSuccess() is called when the event has been sent successfully.
- PixoVR::Apex::ApexSystem::OnSendEventFailed() is called when the session was not started, contains invalid information or when the server is not able to be reached.

