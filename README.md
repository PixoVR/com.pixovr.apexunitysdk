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