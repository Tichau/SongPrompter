using System;
using System.Diagnostics;
using System.Reflection;
using Core.Services;
using Godot;

#pragma warning disable CA1050 // No namespace for root application classes

public partial class App : Node
{
    [Export] private Label? versionLabel;

    public static readonly string AppName;
    public static readonly Version Version;
    public static readonly OS.RenderingDriver RenderingDriver = OS.RenderingDriver.Vulkan;

    public class Options
    {
    }

    public static Collection<ISingleton<App>> Services
    {
        get;
        private set;
    }

    static App()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Debug.Assert(assembly != null);

        AssemblyTitleAttribute? titleAttribute = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
        Debug.Assert(titleAttribute != null);
        App.AppName = titleAttribute.Title;

        AssemblyFileVersionAttribute? versionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        Debug.Assert(versionAttribute != null);
        string version = versionAttribute.Version.EndsWith(".0") ? versionAttribute.Version[..^2] : versionAttribute.Version;
        App.Version = new Version(version);

        App.Services = new Collection<ISingleton<App>>([]);
    }

    public App()
    {
        // Trace listener register can't be called from static constructor (otherwise Godot fails to reload assemblies).
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new Core.TraceListener(hideInternalCalls: true));

        Trace.WriteLine($"Start Application '{App.AppName}' v{App.Version}...");
        Trace.WriteLine($"user:// is located at path: {ProjectSettings.GlobalizePath("user://")}");
    }

    public override void _EnterTree()
    {
        try
        {
            // Randomize the seed of Godot main RNG.
            GD.Randomize();

            Engine.MaxFps = 30;
            this.GetWindow().ContentScaleFactor = DisplayServer.ScreenGetDpi() / 170;

            DependencyBuilder<ISingleton<App>> dependencyBuilder = new("Bind", this);

            dependencyBuilder
                .AddSingleton<Application.AppSettings>(new Application.AppSettings())
                .AddSingleton<Application.AppState>(new Application.AppState())
                .AddSingleton<Application.SetlistDatabase>();

            App.Services = dependencyBuilder.Build();

            foreach (ISingleton<App> service in App.Services)
            {
                service.Initialize();
            }
        }
        catch (Exception exception)
        {
            Trace.TraceError($"Exception thrown during app initialization: {exception}");
            this.GetTree().Quit(-1);
        }

        Trace.WriteLine("App services loaded.");
    }

    public override void _Ready()
    {
        Debug.Assert(this.versionLabel != null);
        this.versionLabel.Text = $"v{App.Version}";
    }

    public override void _ExitTree()
    {
        foreach (ISingleton<App> service in App.Services)
        {
            service.Release();
        }

        App.Services = new Collection<ISingleton<App>>([]);
    }

    public override void _Process(double delta)
    {
        foreach (ISingleton<App> service in App.Services)
        {
            service.Update();
        }
    }
}
