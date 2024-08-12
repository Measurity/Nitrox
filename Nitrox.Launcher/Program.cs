using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia;
using Avalonia.ReactiveUI;
using NitroxModel.Logger;

namespace Nitrox.Launcher;

internal static class Program
{
    // Don't use any Avalonia, third-party APIs or any SynchronizationContext-reliant code before AppMain is called
    // Things aren't initialized yet and stuff might break
    [STAThread]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Main(string[] args)
    {
        ConfigureCultureInfo();

        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.Handler;
        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += AssemblyResolver.Handler;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        Log.Setup();

        AppBuilder builder = AppBuilder.Configure<App>()
                                       .UsePlatformDetect()
                                       .LogToTrace()
                                       .UseReactiveUI();

        // The Wayland renderer on Linux using GPU rendering is not (yet) supported by Avalonia
        // Waiting on PR: https://github.com/AvaloniaUI/Avalonia/pull/11546 to enable rendering on GPU
        if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") is not null)
        {
            builder = builder.With(new X11PlatformOptions { RenderingMode = [X11RenderingMode.Software] });
        }

        return builder;
    }

    private static void ConfigureCultureInfo()
    {
        CultureInfo cultureInfo = new("en-US");

        // Although we loaded the en-US cultureInfo, let's make sure to set these incase the
        // default was overriden by the user
        cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        cultureInfo.NumberFormat.NumberGroupSeparator = ",";

        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
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
