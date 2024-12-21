using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Svg.Skia;
using NitroxModel.Helper;
using NitroxModel.Logger;
using NitroxModel.Platforms.OS.Shared;

namespace Nitrox.Launcher;

internal static class Program
{
    // Don't use any Avalonia, third-party APIs or any SynchronizationContext-reliant code before AppMain is called
    // Things aren't initialized yet and stuff might break
    [STAThread]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.Handler;
        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += AssemblyResolver.Handler;

        LoadAvalonia(args);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        // https://github.com/wieslawsoltes/Svg.Skia?tab=readme-ov-file#avalonia-previewer
        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);

        CultureManager.ConfigureCultureInfo();
        CheckForRunningInstance();
        Log.Setup();

        AppBuilder builder = AppBuilder.Configure<App>()
                                       .UsePlatformDetect()
                                       .LogToTrace()
                                       .UseReactiveUI()
                                       .With(new SkiaOptions { UseOpacitySaveLayer = true });
        builder = WithRenderingMode(builder, Environment.GetCommandLineArgs());
        return builder;
        
        static AppBuilder WithRenderingMode(AppBuilder builder, params string[] args)
        {
            if (args.GetCommandArgs("--rendering")?.FirstOrDefault()?.Equals("software", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                return builder.With(new X11PlatformOptions { RenderingMode = [X11RenderingMode.Software] });
            }
            // The Wayland+GPU is not supported by Avalonia, but Xwayland should work.
            if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") is not null)
            {
                if (!ProcessEx.ProcessExists("Xwayland"))
                {
                    return builder.With(new X11PlatformOptions { RenderingMode = [X11RenderingMode.Software] });
                }
            }
            return builder;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadAvalonia(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    private static void CheckForRunningInstance()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        try
        {
            using ProcessEx process = ProcessEx.GetFirstProcess("Nitrox.Launcher", process => process.Id != Environment.ProcessId);
            if (process is not null)
            {
                process.SetForegroundWindowAndRestore();
                Environment.Exit(0);
            }
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    private static class AssemblyResolver
    {
        private static string currentExecutableDirectory;

        public static Assembly Handler(object sender, ResolveEventArgs args)
        {
            static Assembly ResolveFromLib(ReadOnlySpan<char> dllName)
            {
                dllName = dllName.Slice(0, dllName.IndexOf(','));
                if (!dllName.EndsWith(".dll"))
                {
                    dllName = string.Concat(dllName, ".dll");
                }

                if (dllName.EndsWith(".resources.dll"))
                {
                    return null;
                }

                string dllNameStr = dllName.ToString();

                string dllPath = Path.Combine(GetExecutableDirectory(), "lib", dllNameStr);
                if (!File.Exists(dllPath))
                {
                    dllPath = Path.Combine(GetExecutableDirectory(), dllNameStr);
                }

                try
                {
                    return Assembly.LoadFile(dllPath);
                }
                catch
                {
                    return null;
                }
            }

            Assembly assembly = ResolveFromLib(args.Name);
            if (assembly == null && !args.Name.Contains(".resources"))
            {
                assembly = Assembly.Load(args.Name);
            }

            return assembly;
        }

        private static string GetExecutableDirectory()
        {
            if (currentExecutableDirectory != null)
            {
                return currentExecutableDirectory;
            }
            string pathAttempt = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrWhiteSpace(pathAttempt))
            {
                using Process proc = Process.GetCurrentProcess();
                pathAttempt = proc.MainModule?.FileName;
            }
            return currentExecutableDirectory = new Uri(Path.GetDirectoryName(pathAttempt ?? ".") ?? Directory.GetCurrentDirectory()).LocalPath;
        }
    }
}
