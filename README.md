# com.pixovr.apexunitysdk
Unity Plugin for the implementation of the Apex API

# Installing the Unity Package
To install the Apex Unity SDK, open Window > Package Manager.

Click the '+' in the top left corner and select 'Add package from git URL...'

Copy and paste git@github.com:PixoVR/com.pixovr.apexunitysdk.git into the input box and press the 'Add' button.

Now you should be able to see the package in the Package Manager, where you can install it into your project.

# Using the Apex Plugin

After installing the Apex Unity SDK, navigate to Runtime > Unity. Add the ApexSystem script to any object in the scene.
Any object that the ApexSystem script is added to will **NOT** be deleted between level changes.

The ApexSystem is a singleton, all functions are wrapped in static functions to make the use simpler.
If you want access to the response events in code, you will need to access the ApexSystem Instance.
  (e.g.) ApexSystem.Instance.OnPingSuccess.AddListener(YourFunctionHere);

## Important ApexSystem variables
- ServerIP : This is the Apex server IP address that you want to send the information to. By default it will point to our production environment.
- ModuleID : This is the ID of the module that will be distributed to customers. This will be generated when you create a project on the Apex platform. This must be set for the data to be reported to the proper module.
- ModuleName : This is the name of your module.
- ModuleVersion : The version number for your module currently being distributed.
- ScenarioID : The name of the current scenario within your module that the user is taking part in. You can have multiple scenarios within a module.