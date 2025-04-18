using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Nitrox.Server.Subnautica.Core;
using Nitrox.Server.Subnautica.Models.Configuration;
using Nitrox.Server.Subnautica.Models.GameLogic;
using Nitrox.Server.Subnautica.Models.GameLogic.Bases;
using Nitrox.Server.Subnautica.Models.GameLogic.Entities.Spawning;
using Nitrox.Server.Subnautica.Models.Helper;
using Nitrox.Server.Subnautica.Models.Resources;
using Nitrox.Server.Subnautica.Models.Resources.Helper;
using Nitrox.Server.Subnautica.Models.Serialization;
using Nitrox.Server.Subnautica.Services;
using NitroxModel.Helper;
using NitroxModel.Networking;
using NitroxServer.GameLogic.Entities.Spawning;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Nitrox.Server.Subnautica;

public class Program
{
    private static ServerStartOptions startOptions;
    private static readonly Stopwatch serverStartStopWatch = new();
    private static readonly Lazy<string> newWorldSeed = new(() => StringHelper.GenerateRandomString(10));

    private static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.Handler;
        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += AssemblyResolver.Handler;

        await StartupHostAsync(args);
    }

    /// <summary>
    ///     Initialize here so that the JIT can compile the EntryPoint method without having to resolve dependencies
    ///     that require the custom <see cref="AssemblyResolver.Handler" />.
    /// </summary>
    /// <remarks>
    ///     https://stackoverflow.com/a/6089153/1277156
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task StartupHostAsync(string[] args)
    {
        serverStartStopWatch.Start();

        // Parse console args into config object for type-safety.
        IConfigurationRoot configuration = new ConfigurationBuilder()
                                           .AddCommandLine(args)
                                           .Build();
        startOptions = new ServerStartOptions();
        configuration.Bind(startOptions);
        startOptions.GameInstallPath ??= NitroxUser.GamePath;

        // TODO: Do not depend on Assembly-Csharp types, only game files. Use proxy/stub types which can map to a Subnautica object.
        AssemblyResolver.GameInstallPath = startOptions.GameInstallPath;

        if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") is null)
        {
            const string COMPILE_ENV =
#if DEBUG
                    "Development"
#else
                    "Production"
#endif
                ;
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", COMPILE_ENV);
        }

        await StartServerAsync(args);
    }

    private static async Task StartServerAsync(string[] args)
    {
        // TODO: Don't use NitroxModel.Log in this project.

        // TODO: pass logs to serilog with rolling log files strategy.

        // TODO: Move to separate services
        // if (optionsProvider.ValueuseUpnpPortForwarding)
        // {
        //     _ = PortForwardAsync((ushort)portNumber, ct);
        // }
        // if (useLANBroadcast)
        // {
        //     LANBroadcastServer.Start(ct);
        // }

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.Sources.Clear();
        builder.Configuration
               .AddCommandLine(args)
               .AddNitroxConfigFile(startOptions.GetServerConfigFilePath(), SubnauticaServerOptions.CONFIG_SECTION_PATH);
        builder.Logging.ClearProviders(); // Important for logging performance.
        builder.Logging
               .SetMinimumLevel(builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information)
               .AddFilter("Nitrox.Server.Subnautica", level => level > LogLevel.Trace || (level == LogLevel.Trace && Debugger.IsAttached))
               .AddFilter($"{nameof(Microsoft)}.{nameof(Microsoft.Extensions)}.{nameof(Microsoft.Extensions.Hosting)}", LogLevel.Warning)
               .AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information)
               .AddNitroxConsole(options =>
               {
                   options.IsDevMode = builder.Environment.IsDevelopment();
                   options.ColorBehavior = startOptions.IsEmbedded ? LoggerColorBehavior.Disabled : LoggerColorBehavior.Enabled;
               });
        builder.Services.Configure<HostOptions>(options =>
        {
            options.ServicesStartConcurrently = true;
            options.ServicesStopConcurrently = true;
        });
        // Map key-value configuration to types.
        builder.Services
               .AddOptionsWithValidateOnStart<ServerStartOptions, ServerStartOptions.Validator>()
               .BindConfiguration("")
               .Configure(options =>
               {
                   if (string.IsNullOrWhiteSpace(options.GameInstallPath))
                   {
                       options.GameInstallPath = NitroxUser.GamePath;
                   }
                   if (string.IsNullOrWhiteSpace(options.NitroxAssetsPath))
                   {
                       options.NitroxAssetsPath = NitroxUser.AssetsPath;
                   }
                   if (string.IsNullOrWhiteSpace(options.NitroxAppDataPath))
                   {
                       options.NitroxAppDataPath = NitroxUser.AppDataPath;
                   }
               });
        builder.Services.AddOptionsWithValidateOnStart<SubnauticaServerOptions, SubnauticaServerOptions.Validator>()
               .BindConfiguration(SubnauticaServerOptions.CONFIG_SECTION_PATH)
               .Configure(options =>
               {
                   options.Seed = options.Seed switch
                   {
                       null or "" when builder.Environment.IsDevelopment() => "TCCBIBZXAB",
                       null or "" => newWorldSeed.Value,
                       _ => options.Seed
                   };
               });
        // Add initialization services - diagnoses the server environment on startup.
        builder.Services
               .AddHostedSingletonService<PreventMultiServerInitService>()
               .AddHostedSingletonService<NetworkPortAvailabilityService>()
               .AddHostedSingletonService<ServerPerformanceDiagnosticService>()
               .AddKeyedSingleton<Stopwatch>(typeof(ServerPerformanceDiagnosticService), serverStartStopWatch);
        // Add communication services
        builder.Services
               .AddPackets()
               .AddCommands(!startOptions.IsEmbedded);
        // Add APIs - everything else the server will need.
        builder.Services
               .AddSubnauticaEntityManagement()
               .AddSubnauticaResources()
               .AddPersistence() // TODO: Use SQLite instead.
               .AddHibernation()
               .AddHostedSingletonService<GameServerStatusService>()
               .AddHostedSingletonService<TimeService>()
               .AddHostedSingletonService<PlayerService>()
               .AddHostedSingletonService<StoryTimingService>() // TODO: Merge story services together?
               .AddHostedSingletonService<StoryScheduleService>()
               .AddHostedSingletonService<BatchEntitySpawnerService>()
               .AddHostedSingletonService<FmodService>()
               .AddHostedSingletonService<EscapePodService>()
               .AddSingleton(_ => GameInfo.Subnautica)
               .AddSingleton<BuildingManager>()
               .AddSingleton<WorldEntityManager>()
               .AddSingleton<BatchCellsParser>()
               .AddSingleton<SubnauticaServerProtoBufSerializer>()
               .AddSingleton<NtpSyncer>()
               .AddTransient<SubnauticaServerRandom>()
               .AddSingleton<IEntityBootstrapperManager, SubnauticaEntityBootstrapperManager>()
               .AddSingleton<SimulationOwnershipData>();

        await builder.Build().RunAsync();
    }
}
