This is a Unity plugin which implements the Apex API.

Communication to the Apex Server uses REST https JSON requests which follow the xAPI Standard.

xAPI Spec: https://github.com/adlnet/xAPI-Spec

Documentation here: https://docs.pixovr.com/ApexSDK-Unity/

Please make sure to run setup at the start of a new project. Setup can be found in the menu under PixoVR > Setup.

## SDK source hash

`ApexUtils.SDKSourceHash` is a SHA-256 of every `.cs` and `.asmdef` file under `Runtime/` (excluding the generated files). It is sent as `sdk_source_hash` with every xAPI statement, so you can tell whether two builds contain identical SDK code regardless of the version number.

The value lives in `Runtime/SDK/ApexSourceHashGenerated.cs`. When the package is embedded or local, the Unity editor regenerates it after every script reload. When editing outside Unity, run `python3 "Tools~/source_hash.py" --write` before committing; the `Validate SDK source hash` workflow fails if it is stale.
