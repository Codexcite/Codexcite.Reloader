# Codexcite.Reloader

Lightweight library for "live reloading" XAML pages in Xamarin.Forms on Android, UWP and iOS.
Allows simultaneous updating of multiple connected clients - great if you need to see the impact of your changes instantly on different platforms.

![Sample GIF](https://github.com/vladhorby/Codexcite.Reloader/blob/master/Extra/Screenshots/simultaneous_xaml_example.gif?raw=true)

Similar to Xamarin LiveReload that unfortunately was discontinued and stopped working for me after updating to Xamarin.Forms 3.5.

## Usage

### Codexcite.Reloader.Monitor 

Acts as a TCP server and monitors a specific folder (and sub-folders) for changes to .xaml files. Sends update notifications to the connected clients upon file changes.
__Update: now the [Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=CodexciteSAdeCV.ReloaderMonitor) is available, check below for details.__
* Download and build the project
* Open command line at the Codexcite.Reloader.Monitor.Console\bin\[platform] location
* Run the program
```
dotnet .\Codexcite.Reloader.Monitor.Console.dll -path [PATH] -host [HOST] -port [PORT]
```
#### Parameters:
* -path : the folder containing the .xaml files to be monitored. Default: Environment.CurrentDirectory
* -host : the IP address for the SignalR server. Default: the first available internal network IP address.
* -port : the port number for the SignalR server. Default: 5500.

### Codexcite.Reloader.Forms

Runs inside the Xamarin.Forms app, connects to the Codexcite.Reloader.Monitor server. Updates the XAML for the current displayed page upon receiving notifications from the server.

* Download and build the project
* Reference the project or the resulting .dll in your Xamarin.Forms .Net Standard project. Or, use the nuget [Codexcite.Reloader.Forms](http://www.nuget.org/packages/Codexcite.Reloader.Forms) [![NuGet](https://img.shields.io/nuget/v/Codexcite.Reloader.Forms.svg?label=NuGet)](https://www.nuget.org/packages/Codexcite.Reloader.Forms) 
* Recommended to use a Debug condition with your package reference
```
<PackageReference Condition="'$(Configuration)'=='Debug'" Include="Codexcite.Reloader.Forms" Version="1.2.1" />
```
* On Android, if you get a build error *Can not resolve reference: `System.Threading.Tasks.Extensions`, referenced by `System.Reactive`*, it is a [known issue](https://github.com/dotnet/reactive/issues/803#issuecomment-450520889) when referencing a .Net Standard 2.0 library that uses System.Reactive. Fix: add this to your Android .csproj file:
```
  <ItemGroup>
    ... other package references here
    <PackageReference Include="System.Runtime.CompilerServices.Unsafe" Version="4.5.2" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include="System.Threading.Tasks.Extensions">
    <HintPath>$(UserProfile)\.nuget\packages\system.threading.tasks.extensions\4.5.2\lib\netstandard2.0\System.Threading.Tasks.Extensions.dll</HintPath>
    </Reference>
  </ItemGroup>
```
* Initialize the Reloader in your Xamarin.Forms.Application, using the same url that you configured for the Codexcite.Reloader.Monitor.
```csharp
public App()
{
#if DEBUG
  Reloader.Forms.Reloader.Init("[your local ip here]", 5500);
#endif

  // normal initialization here
  InitializeComponent();

  MainPage = new NavigationPage(new MainPage());
}
```
* On the UWP project, enable the "Private Networks (Client and Server)" capability in the Package.appxmanifest. 

### Codexcite.Reloader.Monitor Visual Studio extension
* Download from [the MS Marketplace](https://marketplace.visualstudio.com/items?itemName=CodexciteSAdeCV.ReloaderMonitor), or use the [local .vsix file](https://github.com/vladhorby/Codexcite.Reloader/blob/master/Extra/VSIX/Codexcite.Reloader.Monitor.VSIX.vsix?raw=true), or download and build the .VSIX project.
* Install in Visual Studio
* (Optional) [Enable the Reload Monitor toolbar](https://github.com/vladhorby/Codexcite.Reloader/blob/master/Extra/Screenshots/VS_enable_toolbar.jpg?raw=true)
* Start the Reloader Monitor from the [Tools menu option](https://github.com/vladhorby/Codexcite.Reloader/blob/master/Extra/Screenshots/VS_tools_menu.jpg?raw=true) or from the [Toolbar option](https://github.com/vladhorby/Codexcite.Reloader/blob/master/Extra/Screenshots/VS_toolbar_button.jpg?raw=true).
* Select the path to the .xaml files to be monitored. Choose your server IP and port. 
![Start dialog](https://github.com/vladhorby/Codexcite.Reloader/blob/master/Extra/Screenshots/VS_start_dialog.jpg?raw=true)
* Click "Start". You'll be able to see notifications about client connections in the Output -> Debug window. 
![Output window](https://github.com/vladhorby/Codexcite.Reloader/blob/master/Extra/Screenshots/VS_output_window.jpg?raw=true)
* Run your apps, making sure you use the same IP and port for Reloader.Init().


### Remarks
* Tested on UWP and Android so far, but it should work on iOS too.
* The Xaml updating is pretty basic so far:
  * __UPDATE 1.0.1 Now handling updates to other pages and caching the updated xaml for future reuse.__
  * Upon updating the xaml for the page, the Page.Dissapearing and Page.Appearing events are forced triggered, so any initial setup code you have in the page code behind can be run again. Worked well with ReactiveUI WhenActivated.
  * Only handled the NavigationPage with ContentPages case so far, still pending for other Page types.
  * __UPDATE 1.0.2 Now also handling updates for the App.xaml Resources, like control styles.__
  ![Sample GIF](https://github.com/vladhorby/Codexcite.Reloader/blob/master/Extra/Screenshots/app.xaml_example.gif?raw=true)
  * __UPDATE 1.1.0 Removed dependency on SignalR, using a simple TcpListener / TcpClient connection now.__
  * __UPDATE 1.2.2 Having problems keeping the TcpClient's Socket alive on Android (it dies after ~1 minute, even with pinging going on every 5 seconds. Fixed for now by forcing a reconnect every 30 seconds. Help on this would be appreciated.__

### License
The MIT License (MIT) see [License file](LICENSE)

### Contributing

Please feel free to add Issues or Pull Requests.
