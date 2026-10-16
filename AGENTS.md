# Working in com.pixovr.apexunitysdk

The Apex Unity SDK. Since 2.0 it is a thin C# layer over the native
[apex-sdk-core](https://github.com/PixoVR/apex-sdk-core) library: every
platform call, session event and telemetry packet goes through the core's C
interface (`Runtime/Native/ApexNative.cs` mirrors its `Apex.h`). Read
`README.md` for how the pieces fit.

## Branches

- The 2.0 line lives on `2.0`; 1.x lives on `dev`. Feature work goes on its own
  branch and reaches `2.0` through a pull request.
- Never force-push a shared branch.

## The native core

- The binaries in `Plugins/` come from a tagged apex-sdk-core release. Update
  them with `python3 "Tools~/update_core.py" vX.Y.Z`, never by hand; the script
  also writes their import settings and records the tag in
  `Plugins/ApexCoreVersion.txt`.
- `ApexNative.kExpectedAbiMajor`/`kExpectedAbiMinor` must match the release's
  `APEX_ABI_MAJOR`/`APEX_ABI_MINOR`. When `Apex.h` changes, update
  `ApexNativeTypes.cs` and `ApexNative.cs` field for field and function for
  function; `ApexCoreClientTests.ConfigLayoutMatchesTheLibrary` catches a
  struct that drifted.
- No networking in C#: the core makes every HTTP call.

## Code style

Every C# file in the package (`Runtime/`, `Editor/`, `Tests/`, `Samples~/`,
`Tools~/`) follows the company style below, the same rules as apex-sdk-core.
CI rejects code that breaks it (see [Checking](#checking)).

### Naming

| What | Style | Example |
| --- | --- | --- |
| Classes, structs, interfaces, delegates | PascalCase (interfaces `I` + PascalCase) | `ApexCoreClient`, `IApexAnalyticsProvider` |
| Methods, properties, events | PascalCase | `JoinSession`, `IsSessionInProgress` |
| Member variables (private, protected, internal) | `m_` + camelCase | `m_coreClient`, `m_sessionInProgress` |
| Public data | properties, never public fields | `public string Token { get; set; }` |
| Fields of interop structs (`[StructLayout]`) | PascalCase, matching `Apex.h` | `ApexConfigData.ModuleId` |
| Parameters and local variables | camelCase | `moduleData`, `sessionUuid` |
| Constants and static read-only fields | `k` + PascalCase | `kLogTag`, `kResultCallback` |
| Enums | `E` + PascalCase | `EPlatformServer`, `ETelemetrySinkType` |
| Enum values | UPPER_CASE | `NA_PRODUCTION`, `APEX_ERR_NETWORK` |
| Type parameters | `T` or `T` + PascalCase | `TInstance`, `TResult` |
| Files | named after the main type they hold | `ApexCoreClient.cs` |

**Names must say what the thing is.** No single letters, no abbreviations, and
no vague words on their own: `argumentIndex` not `i`, `exception` not `ex`,
`launchUrl` not `url` where several URLs are in play, `timeoutMilliseconds` not
`timeout`. Units go in the name when a number has one. CI rejects names shorter
than three characters; the rest is for review.

Exceptions, because something outside our code fixes the name:
- Unity messages (`Awake`, `OnValidate`), and members Unity or .NET interfaces
  require (`IPreprocessBuildWithReport.callbackOrder`,
  `INotifyBindablePropertyChanged.propertyChanged`). Mark such a line with
  `// StyleCheck: external name`.
- Namespaces stay as they are (`PixoVR.Apex`).

### Serialized fields

Unity stores serialized fields by name. Renaming one to the style above needs
`[FormerlySerializedAs("oldName")]`, or existing scenes and assets lose the
value:

```csharp
[SerializeField, FormerlySerializedAs("trackPose")]
private bool m_trackPose;
```

Unity's inspector strips the `m_` prefix from the label.

### Encapsulation

Member variables are private (or protected for subclasses). Expose them through
properties, or better, through methods that do the work, so callers cannot
leave an object half-updated.

### Formatting

- Tabs for indentation, 4 columns wide. Lines up to 120 columns.
- Every opening and closing brace on its own line: namespaces, types, methods,
  property accessors with bodies, control statements and lambdas with block
  bodies. Auto-properties (`{ get; set; }`) and object initializers may stay on
  one line.
- Every `if`, `else`, `for`, `foreach`, `while` and `do` uses braces, even
  around one line.
- After the closing brace of a conditional, loop, `switch` or `try`, leave a
  blank line before more code. No blank line when the next line closes the
  enclosing block, continues the statement (`else`, `catch`, the `while` of a
  `do`) or is a preprocessor line.

```csharp
public void Poll(double nowSeconds)
{
	if (m_nativeClient == IntPtr.Zero)
	{
		return;
	}

	foreach (ApexTrackedObject trackedObject in m_trackedObjects)
	{
		SamplePose(trackedObject);
	}

	m_lastPollSeconds = nowSeconds;
}
```

## Checking

CI's `C# style` workflow runs the checker; run it before you push:

```sh
dotnet run --project "Tools~/StyleCheck" -- .
```

`.editorconfig` holds the same rules for IDEs. After changing anything under
`Runtime/`, regenerate the source hash (`python3 "Tools~/source_hash.py"
--write`); CI checks it. New files and folders need `.meta` files:
`python3 "Tools~/make_metas.py" . Runtime Editor Tests Samples~`.

## Testing

EditMode tests are in `Tests/`. The `ApexCoreClientTests` call the native
library and are skipped where it is missing (the Linux CI editor); run them in
a Windows editor before releasing.
