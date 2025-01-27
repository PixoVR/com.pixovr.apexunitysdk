\page applicationlaunching Launch to other Applications

\section hub How to Return To A Hub

Returning to the Hub App, or the Training Academy, or another application managing app, you need to make a single function call.

[ApexSDK::ExitApplication()](@ref PixoVR::Apex::ApexSystem::ExitApplication()) - Takes 1 optional parameters.
 - `returnTarget:string` - This value should be either a package name or deep link url. By default, it should be an empty string.
This function determines which of the above applications, or custom return application, to return to.
If `ApexSDK::ReturnTarget` is an empty string, the application will exit by default. This indicates that it was launched from the headset and not from another application.
The currently logged in users session token is passed to the return application. The following data is also passed.

 - `optional` - This is optional data that can be passed to other applications. It will also be returned back to any return applications.
 - `returntarget` - This is a package name or deep link url for what application to return to.
 - `targettype` - This is what kind of return target it is. It should either be set to 'url' for deep links, or any other value for a standard android package.

 \section launchapp How to Launch Other Applications

To launch any another application by either package name or deep link url, you only need to do the 2 following steps.

First, set `ApexSDK::ReturnTarget` to either the deeplink url or package name of the application you want to launch.
Then call `ApexSDK::ExitApplication`. If you want the launched application to return to your application, pass in the deeplink url or package name of your current application.

 \section passedvalues Passed in Parameters

 Below is a way to access the parameters passed to the application when it's been launched by any of the above functions.

 - `pixotoken` - This value can be accessed by accessing `ApexSDK::PassedLoginToken`. This value cannot be set.
 - `optional` - This value can be accessed by `ApexSDK::OptionalData`. This value can also be set.
 - `returntarget` - This value can be accessed by `ApexSDK::ReturnTarget`. This value can also be set.

 These values can be either an empty string or null if there is no value passed in.