\page authentication Authentication

\section basiclogin Login

Authentication with Apex is as simple as calling the Login function. If you read the Apex API
documentation you can get a better understanding of what is happening under the hood, but the SDK automates
nearly everything and makes authentication a breeze.

To authenticate, you can either bundle a username and password into a LoginData object or pass a username and password to [ApexSystem::Login()](@ref PixoVR::Apex::ApexSystem::Login()).
You can also bundle a One-Time Code, generated on the PixoVR Platform, as the username and call [ApexSystem::Login()](@ref PixoVR::Apex::ApexSystem::Login()) with an empty password string.

Features of [ApexSystem::Login()](@ref PixoVR::Apex::ApexSystem::Login()) function are that it:

 - Sends the users information to the Apex Server to login. Return `TRUE`.
 - Returns `FALSE` if the password or login are of zero length.

## Parameters

The [ApexSystem::Login()](@ref PixoVR::Apex::ApexSystem::Login()) function has 1 required parameter and 1 optional parameter:
 - `username:string` - The email or username of the user logging in. This parameter is also used for the Assisted Login code generated on the PixoVR Platform.
 - `password:string` - The password of the user logging in. This parameter can be left empty when using an Assisted Login code.

\subsection Handling Authentication Responses

The Apex SDK provides a way to receive the data from the API calls.
The approach in the Unity Apex SDK is done through individual event Delegates.

For Authentication, we have a success and failed delegate:

[ApexSystem::OnLoginSuccess](@ref PixoVR::Apex::ApexSystem::OnLoginSuccess) is called when the users information is valid and has access to the module.
 - `loginResponse:LoginResponseContent` - contains information about the user that has logged in.

[ApexSystem::OnLoginFailed](@ref PixoVR::Apex::ApexSystem::OnLoginFailed) is called when the users information is not valid, does not have access to the module or when the server is not able to be reached.
 - `response:FailureResponse` contains the error code and error message from why the login failed.

In the [Example Code](#example-code1) section you'll see C# examples on how to bind to the event delegates.

## Accessing the Authentication call in Unity

The ApexSystem is a Singleton, but also contains Static functions to access functionality like Login.

\code{.cs}
ApexSystem.Login(username, password);
\endcode

\anchor example-code1
## Example Code

### Calling Login

\code{.cs}
ApexSystem.Login("pixo@pixovr.com", "123abc");
\endcode

### Binding to API Response Delegates

\code{.cs}
void Start()
{
  ApexSystem.Instance.OnLoginSuccess.AddListener(OnLoginSuccess);
  ApexSystem.Instance.OnLoginFailed.AddListener(OnLoginFailed);
}

void OnLoginFailed(FailureResponse response)
{
  // Handle failed login here...
}

void OnLoginSuccess(LoginResponseContent loginResponse)
{
  // Handle successful login here...
}
\endcode

# Q&A On Authentication

We've decided to provide some notes on questions we've been asked about authentication.

**Q.** Is the users information encrypted? \n
**A.** Yes, but not by anything internal to the Apex SDK or API calls. Instead, we use TLS over HTTPS.

**Q.** Does the Login call do anything besides log-in the user? \n
**A.** Yes, Login goes beyond and checks the user against the module to see if they have access to module. During this, we also pass back some module and user specific information which will be seen a successful login response.
