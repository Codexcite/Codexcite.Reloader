# Codexcite.Reloader

Lightweight library for "live reloading" XAML pages in Xamarin.Forms on Android, UWP and iOS.
Allows simultaneous updating of multiple connected clients - great if you need to see the impact of your changes instantly on different platforms.

Similar to Xamarin LiveReload that unfortunately was discontinued and stopped working for me after updating to Xamarin.Forms 3.5.

## Usage

### Codexcite.Reloader.Monitor 

Acts as a SignalR server and monitors a specific folder (and sub-folders) for changes to .xaml files. Sends update notifications to the connected clients upon file changes.
* Download and build the project
* Open command line at the Codexcite.Reloader.Monitor\bin\[platform] location
* Run the program
```
dotnet .\Codexcite.Reloader.Monitor.dll -path [PATH] -url [URL] -host [HOST] -port [PORT]
```
#### Parameters:
* -path : the folder containing the .xaml files to be monitored. Default: Environment.CurrentDirectory
* -url : the url where the SignalR server will be run. If set, -host and -port are ignored. 
* -host : the IP address for the SignalR server. Default: the first available internal network IP address.
* -port : the port number for the SignalR server. Default: 5500.

### Codexcite.Reloader.Forms

Runs inside the Xamarin.Forms app, connects to the Codexcite.Reloader.Monitor server. Updates the XAML for the current displayed page upon receiving notifications from the server.

* Download and build the project
* Reference the project or the resulting .dll in your Xamarin.Forms .Net Standard project. Or, use the nuget [Codexcite.Reloader.Forms](http://www.nuget.org/packages/Codexcite.Reloader.Forms) [![NuGet](https://img.shields.io/nuget/v/Codexcite.Reloader.Forms.svg?label=NuGet)](https://www.nuget.org/packages/Codexcite.Reloader.Forms) 
* Recommended to use a condition with your project reference
```
<ProjectReference Include="..\..\..\Codexcite.Reloader.Forms\Codexcite.Reloader.Forms.csproj" Condition="'$(Configuration)'=='Debug'" />
```
* Initialize the Reloader in your Xamarin.Forms.Application, using the same url that you configured for the Codexcite.Reloader.Monitor.
```csharp
public App()
{
#if DEBUG
  Reloader.Forms.Reloader.Init("http://[your local ip here]:5500");
#endif

  // normal initialization here
  InitializeComponent();

  MainPage = new NavigationPage(new MainPage());
}
```
* On the UWP project, enable the "Private Networks (Client and Server)" capability in the Package.appxmanifest. 

### Remarks
* Tested on UWP and Android so far, but it should work on iOS too.
* The Xaml updating is pretty basic so far:
  * ~~Only handles updates for the current page, ignores updates for other pages.~~
  * Navigating back keeps the changes to the previous page. Page1 (original) -> Page1 (modified) -> Page2 ->back-> Page1 (modified)
  * ~~Navigating operations that recreate pages will load the original versions of those pages. Page1 -> Page2 (original) -> Page2 (modified) ->back-> Page1 -> Page2 (original)~~
  * __UPDATE 1.0.1 Now handling updates to other pages and caching the updated xaml for future reuse.__
  * Upon updating the xaml for the page, the Page.Dissapearing and Page.Appearing events are forced triggered, so any initial setup code you have in the page code behind can be run again. Worked well with ReactiveUI WhenActivated.
  * Only handled the NavigationPage with ContentPages case so far, still pending for other Page types.
	* __UPDATE 1.0.2 Now also handling updates for the App.xaml Resources, like control styles.__

### License
The MIT License (MIT) see [License file](LICENSE)

### Contributing

Please feel free to add Issues or Pull Requests.
