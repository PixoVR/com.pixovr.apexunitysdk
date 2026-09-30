This is a Unity plugin which implements the Apex API.

Communication to the Apex Server uses REST https JSON requests which follow the xAPI Standard.

xAPI Spec: https://github.com/adlnet/xAPI-Spec

Documentation here: https://docs.pixovr.com/ApexSDK-Unity/

Please make sure to run setup at the start of a new project. Setup can be found in the menu under PixoVR > Setup.

## Analytics providers

Analytics integrations can be supplied by a separate package through `IApexAnalyticsProvider`:

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

Add `ApexTrackedObject` to objects that an analytics provider should track. Providers receive
registration, unregistration, and engagement callbacks for those objects. Create an
`ApexAnalyticsSettings` asset in a `Resources` folder to disable analytics globally or list
provider names under `disabledProviders`. Providers registered after tracked objects are enabled
also receive `OnTrackedObjectRegistered` for those existing objects. Providers can implement only
the callbacks they need. Providers receive detached copies of payloads, so mutations are not sent
to Apex.

## PixoVR provider & telemetry packet

The built-in `PixoVR` provider records compact telemetry and sends one JSON
packet per flush through a pluggable sink. Packets contain short field names:

| Field | Meaning |
| --- | --- |
| `v`, `seq`, `t0` | Version, sequence, and Unix-millisecond packet start |
| `s`, `r`, `u`, `m`, `sc` | Session, registration, user, module, and scenario |
| `ob` | Object table entries referenced by the packet |
| `ev` | Relative-time records |
| `end` | Final session packet marker |

Records use `r`/`u` for object registration, `e`/`x` for engagement,
`i` for interactions, `sb`/`se` for steps, and `p` for poses. Pose positions
are centimeters and rotations are whole-degree Euler values. Available sinks
are `LogTelemetrySink`, `FileTelemetrySink`, and `HttpTelemetrySink`.

Create an `ApexAnalyticsSettings` asset in `Resources` to configure provider
enablement, sink type, HTTP URL, flush interval, pose sampling rate, and
maximum records per packet. Without an asset, the provider is enabled with the
log sink. `ApexInteractable`, `ApexGazeTracker`, `ApexStepTracker`, and
`ApexSpatialSampler` provide module-facing interaction, gaze, step, and head
sampling hooks. `ApexGazeTracker` consumes an `ApexGazeSource`; the built-in
`ApexHeadGazeSource` uses a configured head, `ApexSpatialSampler.Head`, or its
own transform. The optional `ApexOpenXREyeGazeSource` is isolated in the
`PixoVR.ApexUnitySDK.OpenXR` assembly and falls back to head gaze when enabled.
The optional XRI bridge is in the `PixoVR.ApexUnitySDK.XRI` assembly.