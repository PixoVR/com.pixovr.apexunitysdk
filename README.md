# com.pixovr.apexunitysdk
Unity Plugin for the implementation of the Apex API

# Installing the Unity Package
To install the Apex Unity SDK, open Window > Package Manager.

Click the '+' in the top left corner and select 'Add package from git URL...'

Copy and paste git@github.com:PixoVR/com.pixovr.apexunitysdk.git into the input box and press the 'Add' button.

Now you should be able to see the package in the Package Manager, where you can install it into your project.

# Using the Apex Plugin

After installing the Apex Unity SDK, navigate to Runtime > Unity. Add the ApexSystem script to any object in the scene.
Any object that the ApexSystem script is added to will **NOT** be deleted between level changes.

The ApexSystem is a singleton, all functions are wrapped in static functions to make the use simpler.
If you want access to the response events in code, you will need to access the ApexSystem Instance.
  (e.g.) ApexSystem.Instance.OnPingSuccess.AddListener(YourFunctionHere);

# Requirements for building with the Apex Plugin

The projects **Api Compatbility Level** needs to be **.NET 4.x** or newer.

## Important ApexSystem variables

- ServerIP : This is the Apex server IP address that you want to send the information to. By default it will point to our production environment.
- ModuleID : This is the ID of the module that will be distributed to customers. This will be generated when you create a project on the Apex platform. This must be set for the data to be reported to the proper module.
- ModuleName : This is the name of your module.
- ModuleVersion : The version number for your module currently being distributed.
- ScenarioID : The name of the current scenario within your module that the user is taking part in. You can have multiple scenarios within a module.

## Function Explination

All server functions have corresponding Success and Failed events.

### Ping()

Contacts the Apex Server for status.

**OnPingSuccess** is called when there is a successful response from the server.
**OnPingFailed** is called when the server is not able to be reached.

### Login(login : LoginData) : Boolean
### Login(username : string, password : string) : Boolean

Sends the users information to the Apex Server to login. Returns false if the password or login are of zero length.

**OnLoginSuccess** is called when the users information is valid.
**OnLoginFailed** is called when the users information is not valid or when the server is not able to be reached.

### JoinSession(scenarioID : string) : Boolean

Joins a user to a session for a given scenario within the module.
Returns false if there is no logged in user.

Will throw an error if there is already a session in progress that hasn't been ended.

**OnJoinSessionSuccess** is called when the user was able to join the session successfully.
**OnJoinSessionFailed** is called when the user was not able to join the session or when the server is not able to be reached. Ensure the user has access to the given module.

### CompleteSession(currentSessionData : SessionData) : Boolean

Completes the current session.
Returns false if there is no logged in user or current session.

**OnCompleteSessionSuccess** is called when the session was completed successfully.
**OnCompleteSessionFailed** is called when the session was not started, unable to be completed otherwise or when the server is not able to be reached.

