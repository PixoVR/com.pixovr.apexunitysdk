\page authentication Authentication

Authentication with Apex is as simple as calling the Login function. If you read the Apex API
documentation you can get a better understanding of what is happening under the hood, but the SDK automates
nearly everything and makes authentication a breeze.

To authenticate, you can either bundle a username and password into a LoginData object or pass a username and password to ApexSystem::Login().
You can also bundle a One-Time Code, generated on the PixoVR Platform, as the username and call ApexSystem::Login() with an empty password string.

Features of this function are that it:

 - Sends the users information to the Apex Server to login. Return `true`.
 - Returns `false` if the password or login are of zero length.

ApexSystem::OnLoginSuccess is called when the users information is valid and has access to the module.

ApexSystem::OnLoginFailed is called when the users information is not valid, does not have access to the module or when the server is not able to be reached.

\section logindata LoginData

## Accessing the Authentication call in Unity

The ApexSystem is a Singleton, but also contains Static functions to access functionality like Login.

\code{.cs}
ApexSystem ApexAPI = ApexSystem.Login(username, password);
\endcode

## Constructor

\code{.cs}
public Login(string Username, string Password)
{
    Login = Username; 
    Password = Password; 
}
\endcode

## Handling Authentication API Responses

The Apex SDK provides a way to receive the data from the API calls.
The approach in the Unity Apex SDK is done through specific Success and Fail unity events.

ApexSystem::OnLoginSuccess(LoginResponseContent loginResponse) - This Delegate is called when the users information is valid and has access to the module.
`loginResponse` contains information about the user that has logged in.

For more information on what is in `loginResponse`, checkout the [LoginResponseContent](@ref LoginResponseContent) class.

ApexSystem::OnLoginFailed(FailureResponse response) - This Delegate is called when the users information is invalid, does not have access to the module or the server is not able to be reached.
`response` contains the error code and error message from why the login failed.

For more information on what is in `response`, checkout the [FailureResponse](@ref FailureResponse) class.

In the [Example Code](#example-code) section, you'll see C# examples on how to bind to the event delegates.

## Example Code

### Calling Login

\code{.cs}
LoginData pixoLoginData = new LoginData("pixo@pixovr.com", "123abc");
ApexSystem.Login(pixoLoginData);
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

Q. Is the users information encrypted?
A. Yes, but not by anything internal to the Apex SDK or API calls. Instead, we use TLS over HTTPS.

Q. Does the Login call do anything besides log-in the user?
A. Yes, Login goes beyond and checks the user against the module to see if they have access to module. During this, we also pass back some module and user specific information which will be seen a successful login response.