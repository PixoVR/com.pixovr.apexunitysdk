# Apex Quick Start

A scene for trying the SDK against a real Apex environment, from the editor or
a Windows build.

1. Open `Scenes/QuickStart.unity` and press Play. The **Apex** object holds an
   `ApexSystem` and the `ApexQuickStart` panel; it starts on stage with module
   13. Change both on `ApexQuickStart` in the inspector.
2. Log in with a user that can access the module (or with Quick ID, using a
   device serial number that has enrolled users).
3. Use the buttons. Every call logs its result in the panel and the console:
   - **Check access**, **Module list** and **Login code** call the platform as
     the logged-in user.
   - **Join**, **Send event** and **Complete** run a session. Join sends the
     scenario ID with the module and device details; events and completion
     send JSON data. By default these reach the platform as xAPI statements
     (**Send xAPI Session Events** in Project Settings > PixoVR).
   - **Begin/Complete step** and the cube buttons record telemetry, which the
     core batches into packets. Without an `ApexAnalyticsSettings` asset the
     packets go to the console; while a session is active they are sent every
     10 seconds and when it completes.
   - **Exit to launcher** returns to the app that launched the module, passing
     the login on.

The panel is drawn with IMGUI, so it shows on a desktop screen, not inside a
headset. On a Quest, call the same `ApexSystem` methods from your own UI.

Platform calls need the native apex-sdk-core library for the platform you are
running on: Windows x64 (editor and player) and Android arm64 are included.
