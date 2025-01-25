# Changelog

<!--
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).
-->

### [1.0.0] - 2022-05-31
 - This is the first release of *com.pixovr.apexunitysdk*.

### [1.0.1] - 2022-06-03
 - Updates the Newtonsoft.Json dependency to the latest version and rebuilt TinCan.Net along side it.
 - Make changes to the Sample to fix UI orientation.
 - Added new changes to the README to reflect info for building with various versions of Unity.

### [1.0.2] - 2022-06-04
 - Added **SendSessionEvent** to allow sending events during a session.
 - Updated documentation for **SendSessionEvent**.
 - Added static properties to ApexSystem.

### [1.0.3] - 2022-06-07
 - Created Extension which simplifies adding extension data for xAPI.
 - Added Context Extension parameter to **JoinSession**.
 - Added Result and Context Extension parameters to **CompleteSession**.
 - Added the missing Statement to **SendSessionEvent**.
 - Changed the platform variable to reflect the actual platform.
 - Added deviceId and deviceModel to any event within the statement context.

### [1.0.4] - 2022-06-08
 - Exception catching for Extension.
 - Fixed incorrect IRIs within **JoinSession**, **SendEvent** and **CompleteSession**.
 - Fixed incorrect IRIs within the Sample provided.
 - Added documentation to point to xAPI spec and TinCan.NET documentation.

### [1.0.5] - 2022-07-19
 - Removed custom event name being passed into the SendSessionEvent function.
 - Added support to create an Extension from a jobject.
 - Merge any existing extensions from the SendSessionEvent context.

### [1.0.6] - 2022-11-28
 - Adds error checking to ensure an instance is created if there is not an existing instance.

### [1.0.7] - 2023-02-17
 - Determine LessonStatus is passed or failed based on the reported result success instead of result completion.

### [1.1.0] - 2023-04-13
 - Adds Pin SSO, to use on https://apex.pixovr.com/authenticate.
 - Makes sure that the module version matches the required pattern.
 
### [1.1.1] - 2023-04-14
 - Wraps the Ping call with an exception to handle when there is no connection.
 - Adds SendSimpleSessionEvent to allow sending a custom session event simply.
 - Adds a constructor to Extension to accept a Dictionary<string, string>.
 - Sets the execution order of the ApexSystem to -50 to execute before other scripts.

 ### [1.1.2] - 2023-05-08
 - Added module access check on user login.
 - Added passing score to logged in user property on successful login.

 ### [1.1.3] - 2023-06-07
 - Added logic to calculate the scaled score when scaled score is invalid, or set to 0 when the score is not 0.
 - Added the SDK version to the context extension when event API calls are made.
 - Removed the need for a password when using One-Time Login codes.
 - Fixed the Sample project for the package.
 - Added the minimum passing score in sample project 'Current User' display.
 - Added example of the Request Authorization Pin functionality to the sample project.

  ### [1.2.0] - 2024-01-11
 - Added the ability to change what platform server an application points to at runtime, through the **ChangePlatformServer** function.
 - Separates the Login and User Module Access Verification steps to allow a Login to happen on its own.
 - Added **CheckModuleAccess** which verifies the users access to a given module.
 - Added **GetCurrentUserModules** and **GetUserModules** which returns all of the module id's that the user can access.

 ### [1.4.0] - 2024-06-21
 - Fixes a bug with **PlatformTargetServer** when playing in editor, it would reset the value back to the first selectable value.
 - Adds **GetAuthenticationToken** to get any authentication token that is passed in via **OpenURL** call to open the application.
 - Adds **LoginWithToken** to allow login with an authentication token.
 - Adds **ReturnToHub** to return the user to either the Hub Application or Training Academy depending on the endpoint.
 - Adds **GetModulesList** to get a list of all modules accessible to the users organization.

 ### [1.4.1] - 2024-08-02
 - Fixes a bug that could allow developers to not check for user's ability to access a module before starting a session.
 
 ### [1.4.2] - 2024-08-23
 - Implements functionality from ManageXR to integrate with the device management platform.
 - Adds a custom AndroidManifest XML that implements the basics permissions for ManageXR.

 ### [1.5.3] - 2025-01-24
 - Removes **ReturnToHub** and replaces it with **ExitApplication**.
 - Adds the **ReturnTarget** property. This value is read in when another application launches the current application. Can also be set to allow **ExitApplication** to launch another application.
 - **ExitApplication** will also now exit the application back to the headset's dashboard when no return target is provided.
 - Adds **optionalParameter** to allow entirely custom strings of data to be sent to another application.
 - Fixes the issue that occurs when launching between Unity and Unreal Engine applications and the application didn't completely quit.