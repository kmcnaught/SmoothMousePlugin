# SmoothMousePlugin

A sample eyetracker plugin for Optikey that uses mouse input with added smoothing.

This is intended to illustrate how to use the Optikey plugin interface to support your own eye tracker or alternative points source. 

## How this plugin implementation works

`MouseInput.cs` defines a class that implements the `IPointService` interface from Optikey's `JuliusSweetland.OptiKey.Contracts`. It has a `Point` event which publishes (x,y) positions on a regular basis. In this example, the data comes from polling the mouse cursor, but it could just as easily come from an event-driven eye tracker API. It also has an `Error` event from `INotifyErrors` which allows it to publish any errors. 

The project depends on:
- System libraries: `System.Reactive.Linq` and `WindowsBase`
- Optikey's provided DLLs: `JuliusSweetland.OptiKey.Contracts` and `PointSourceUtils` which gives us `Time.HighResolutionUtcNow` from `JuliusSweetland.Optikey.Static`. The latter library also contains graphics-related utilities such as `DipScalingFactorX` and `PrimaryScreenHeightInPixels` which may be useful for eye trackers that report their positions relative to screen dimensions. 

## Building this repo

-  Clone the repo from github:
`git clone https://github.com/kmcnaught/SmoothMousePlugin.git`
- Open `SmoothMouse.sln` in Visual Studio
- Build for x64

## Testing the resulting plugin locally

Put the entirety of the `Release` folder into `%APPDATA%\Optikey\OptiKey\EyeTrackerPlugins`. You can name the folder anything you want. 

Now in Optikey's Management console, the plugin will appear in the list of points sources as well as in the Plugin Search window.

## Publishing your own plugin 

- Fork this repo
- Replace the implementation with your own eye tracker integration
- After compiling your code, zip up the contents of your `Release` folder to use as a release asset
- Create a release on github, with your zipped folder as an asset
- Add the topic `optikey-plugin` to your repo and your plugin will now be discovered by Optikey's Plugin Search Wizard. 

