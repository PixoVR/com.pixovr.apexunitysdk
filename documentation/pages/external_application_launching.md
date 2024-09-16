\page applicationlaunching Launch to other Applications

\section hub How to Return To A Hub

Returning to the Hub App, or the Training Academy, or another application managing app, you need to make a single function call.

[ApexSystem::ExitApplication()](@ref PixoVR::Apex::ApexSystem::ExitApplication()) - Takes 2 optional parameters.
 - `returnTarget:string` - This value should be either a package name or deep link url.
 - `returnTargetType:string` - Use 'url' for a deep link url or any other value for a standard package.
This function determines which of the above applications, or custom return application, to return to.
The currently logged in users session token is passed to the return application. The following data is also passed.

 - `optional` - This is optional data that can be passed to other applications. It will also be returned back to any return applications.
 - `returntarget` - This is a package name or deep link url for what application to return to.
 - `targettype` - This is what kind of return target it is. It should either be set to 'url' for deep links, or any other value for a standard android package.

 \section launchapp How to Launch Other Applications

To launch any another application by package name you need to make a single function call.

[PixoAndroidUtils::LaunchApp()](@ref PixoVR::Apex::PixoAndroidUtils::LaunchApp()) - Takes 3 required parameters.
 - `packageName:string` - This is the package name of the application to launch.
 - `extraKeys:string[]` - This is an array of keys to pass to the application.
 - `extraValues:string[]` - This is an array of values to pass to the application.
This application will launch the application with the given package name. It takes in optional data that can be passed to the application.
The following values are read in by the Apex SDK.

 - `pixotoken` - This is the users session token. To fill in this value, the `CurrentActiveLogin.Token` property can be used.
 - `optional` - This is optional data that can be passed to other applications. It will also be returned back to any return applications.
 - `returntarget` - This is a package name or deep link url for what application to return to.
 - `targettype` - This is what kind of return target it is. It should either be set to 'url' for deep links, or any other value for a standard android package.

 To launch another application by deep link url, you only need to call `Application.OpenURL()` which is a standard Unity call.

 \section passedvalues Passed in Parameters

 Below is a way to access the parameters passed to the application when it's been launched by any of the above functions.

 - `pixotoken` - This value can be accessed by accessing `ApexSDK::PassedLoginToken`. This value cannot be set.
 - `optional` - This value can be accessed by `ApexSDK::OptionalData`. This value can also be set.
 - `returntarget` - This value can be accessed by `ApexSDK::ReturnTarget`. This value can also be set.
 - `targettype` - This value can be accessed by `ApexSDK::TargetType`. This value can also be set.

 These values can be either an empty string or null if there is no value passed in.