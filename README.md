# Apex Unity SDK

The Unity package for the Apex platform: login (password, launcher token,
Quick ID, authorization code), module access, the module list, sessions,
telemetry and returning to the launcher.

Since 2.0 the SDK is a thin C# layer over
[apex-sdk-core](https://github.com/PixoVR/apex-sdk-core), the native library
the Unity and Unreal SDKs share. The core makes every HTTP call, sends the
session heartbeat and builds the telemetry packets; C# makes no network calls.

Documentation: https://docs.pixovr.com/ApexSDK-Unity/

## Requirements

- Unity 2021.3 LTS or newer.
- Platforms with a native core: **Windows x64** (editor and player) and
  **Android arm64** (Quest). Elsewhere (macOS and Linux editors, iOS, WebGL)
  the SDK loads but every platform call fails with `APEX_ERR_INVALID_STATE`
  and `ApexSystem.CoreErrorMessage` says why.

## Setup

1. Add the package (Package Manager > Add package from git URL).
2. Run **PixoVR > Setup** once per project (Android manifest and launcher
   utilities).
3. Fill in **Project Settings > PixoVR**: target server, module ID, module
   version (`MAJOR.MINOR.PATCH`).
4. Add an `ApexSystem` to your first scene, or let it create itself on first
   use.

To try every call first, import the **Quick Start** sample (Package Manager >
Apex Unity SDK > Samples) and open its scene.

## Using ApexSystem

Every call returns at once. Its result arrives on a later frame, on the main
thread, through the success or failure callback. Both get an `ApexResponse`
(`Error`, `HttpStatus`, `Message`, `Succeeded`); success also gets the parsed
result.

```csharp
ApexSystem.Login(username, password,
	(response, userInformation) =>
	{
		// The module-access check has already run (unless PIXOVR_MODULEACCESS_BYPASS is defined).
		Debug.Log($"Hello {userInformation.User.First}; access: {ApexSystem.UserHasAccess}");
	},
	(response, failure) => Debug.LogWarning($"Login failed: {response}"));

// Launched from the Apex launcher: log in with the token it passed.
ApexSystem.LoginWithToken(onLoggedIn, onLoginFailed);
```

### Modules

`GetModulesPage` pages through the modules the user can launch on a platform.
The first call fetches the whole list; later pages, other filters and other
sort orders reuse it until logout, the next login, `GetModulesList`, or
`refresh: true`. A page served from the kept list has `HttpStatus` 0.

```csharp
ApexSystem.GetModulesPage("android", pageIndex: 0, pageSize: 20,
	EModuleLifecycle.RELEASED | EModuleLifecycle.UAT, EModuleSortOrder.NAME, refresh: false,
	(response, page) =>
	{
		Debug.Log($"Page {page.PageIndex + 1} of {page.PageCount}, {page.TotalCount} modules");
		foreach (Module module in page.Modules)
		{
			Debug.Log($"{module.Name}: {module.Versions[0].Version} ({module.Versions[0].Lifecycle.Name})");
		}
	}, onFailure);
```

Each version has a `Lifecycle` (`Id`, `Name`, `Stage`). Pages keep versions in
the requested lifecycles, `RELEASED` by default, and modules with at least one
such version. `Stage` maps the platform's lifecycle names: Released,
Development, QA and UAT to their own flags, any other name to `OTHER`.
Archived versions are never returned.

### Sessions

Session calls post to the platform's `/event` route. Their data is JSON the
SDK builds for you, plus anything the module adds:

```csharp
JObject sessionData = new JObject { ["difficulty"] = "hard" };
ApexSystem.JoinSession("FireSafety", sessionData, (response, joinResponse) => { }, onFailure);

ApexSystem.SendSessionEvent("extinguisher_used", "Extinguisher", new JObject { ["seconds"] = 4.2 });

SessionData result = new SessionData(score: 85, scaledScore: 0, minimumScore: 0, maximumScore: 100,
	durationSeconds: 312, complete: true, success: true);
ApexSystem.CompleteSession(result, new JObject { ["mistakes"] = 1 });
```

| Call | Data sent |
| --- | --- |
| `JoinSession` | `{"scenarioId", "moduleName", "moduleVersion", "platform", "deviceId", "deviceModel", "deviceSerial", "sdkVersion", "sdkSourceHash", "coreVersion", "moduleAccessChecked", "data"}` |
| `SendSessionEvent` | `{"action", "target", "scenarioId", "data"}` |
| `CompleteSession` | `{"scenarioId", "score", "scaledScore", "minimumScore", "maximumScore", "durationSeconds", "complete", "success", "data"}` |

`data` is what the module passed, or `null`. A module with its own format uses
`JoinSessionWithRawData`, `SendSessionEventWithRawData` and
`CompleteSessionWithRawData`, which send the bytes as they are. The core
base64-encodes the data and sends the session heartbeat while a session is
active.

#### xAPI sessions

Until the platform's new session route is live, sessions can send the xAPI
statements the 1.x SDK sent. Turn on **Send xAPI Session Events** in
**Project Settings > PixoVR**, or set `ApexSystem.SendXapiSessionEvents` (it
applies from the next join). The same calls then send:

| Call | Statement |
| --- | --- |
| `JoinSession` | verb `joined_session`, object `https://pixovr.com/xapi/objects/<moduleId>/<scenarioId>`, `sessionData` as context extensions |
| `SendSessionEvent` | verb `https://pixovr.com/xapi/verbs/<action>`, object `.../<scenarioId>/<target>` (lowercase, spaces as `_`), `eventData` as context extensions |
| `CompleteSession` | verb `completed_session`, the score, success and duration as the result, `resultData` as result extensions |

- **Every statement** carries the user's email as the actor, the session as
  `context.registration`, the module version and platform, and the standard
  extensions (`moduleIds`, `device_id`, `device_model`, `sdk_version`,
  `sdk_source_hash`, `module_access_checked`, `device_serial`).
- **Extensions:** each key becomes `https://pixovr.com/xapi/extension/<key>`
  and each value becomes text.
- **Statements you build yourself:** the 1.x `SendSessionEvent(Statement)` is
  `SendSessionStatement(JObject)`:

```csharp
ApexSystem.SendSessionStatement(new JObject
{
	["verb"] = new JObject { ["id"] = "https://pixovr.com/xapi/verbs/opened", ["display"] = new JObject { ["en"] = "Opened" } },
	["object"] = new JObject { ["objectType"] = "Activity", ["id"] = "https://pixovr.com/xapi/objects/13/FireSafety/door" },
});
```

The `*WithRawData` calls need a JSON object in this mode: the SDK's session
JSON or a statement.

### Returning to the launcher

`ApexSystem.ExitApplication(nextTarget)` opens the app or URL that launched
the module (passing the login and its `optional` data on), then quits.

### Migrating from 1.x and earlier 2.0 builds

- Replace `HttpResponseMessage` with `ApexResponse` in callbacks.
- Replace xAPI `Statement` and `Extension` arguments with `JObject` data
  (`SendSimpleSessionEvent` is now `SendSessionEvent`, `SendSessionEvent(Statement)`
  is `SendSessionStatement(JObject)`). To keep sending xAPI statements to the
  platform, turn on **Send xAPI Session Events**.
- `GetUser`, the reporting queries and `ApexAPIHandler` are gone.
- Model fields are properties now, and enums are renamed (`PlatformServer` is
  `EPlatformServer`). See CHANGELOG.md.

## Analytics providers

Analytics integrations can be supplied by a separate package through
`IApexAnalyticsProvider`; every method has an empty default:

```csharp
public sealed class MyAnalyticsProvider : IApexAnalyticsProvider
{
	public string Name => "MyAnalytics";
}

public static class MyAnalyticsRegistration
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Register()
	{
		ApexAnalytics.Register(new MyAnalyticsProvider());
	}
}
```

Add `ApexTrackedObject` to objects providers should track; providers get
registration, unregistration, engagement and interaction callbacks for them,
including objects enabled before the provider registered. `ApexInteractable`,
`ApexGazeTracker` (with an `ApexGazeSource` such as `ApexHeadGazeSource` or
the OpenXR eye-gaze source), `ApexStepTracker` and `ApexSpatialSampler`
provide interaction, gaze, step and head-pose hooks. The optional OpenXR and
XRI bridges live in their own assemblies.

## PixoVR telemetry

The built-in `PixoVR` provider hands everything to the core, which numbers
objects, timestamps and rounds the records (centimeters, whole degrees), drops
unchanged poses, and batches them into compact JSON packets
([schema](https://github.com/PixoVR/apex-sdk-core/blob/main/spec/schema/telemetry_packet.json)).
A packet goes out every 10 s while a session is active, at 500 records, when
the app pauses, and when the session ends. Each packet's `r` is the session's
UUID, which links it to the session events.

Create an `ApexAnalyticsSettings` asset in a `Resources` folder to choose the
sink (`NONE`, `LOG`, `FILE` under `persistentDataPath/ApexTelemetry`, or
`HTTP` to a URL), the flush interval, the pose sampling rate and the packet
size, or to disable analytics or individual providers. Without the asset,
packets go to the log.

## The native core

`Plugins/Windows/x86_64/apex_core.dll` and
`Plugins/Android/arm64-v8a/libapex_core.so` come from an apex-sdk-core release;
`Plugins/ApexCoreVersion.txt` names it. Update them with:

```sh
python3 "Tools~/update_core.py" v0.2.0
```

On Android the SDK loads the library through `System.loadLibrary`, so the
core's HTTP backend gets the JavaVM; nothing else is needed. The core's own
log lines appear in the Unity log under the `ApexCore` tag.

## SDK source hash

`ApexUtils.SDKSourceHash` is a SHA-256 of every `.cs` and `.asmdef` file under
`Runtime/` (excluding the generated files). It is sent in each session's join
data, so you can tell whether two builds contain identical SDK code
regardless of the version number. The editor regenerates
`Runtime/SDK/ApexSourceHashGenerated.cs` when the package is embedded or
local; outside Unity run `python3 "Tools~/source_hash.py" --write`. CI fails
if it is stale.

## Development

The code follows the company style described in [AGENTS.md](AGENTS.md); CI
checks it with `dotnet run --project "Tools~/StyleCheck" -- .`.
