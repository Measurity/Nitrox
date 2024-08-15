using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanumanInstitute.MvvmDialogs;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.Models.Patching;
using Nitrox.Launcher.Models.Utils;
using Nitrox.Launcher.ViewModels.Abstract;
using NitroxModel.Discovery.Models;
using NitroxModel.Helper;
using NitroxModel.Logger;
using NitroxModel.Platforms.OS.Shared;
using NitroxModel.Platforms.Store;
using NitroxModel.Platforms.Store.Interfaces;
using NitroxModel.Server;
using ReactiveUI;

namespace Nitrox.Launcher.ViewModels;

public partial class LaunchGameViewModel : RoutableViewModelBase
{
    private readonly OptionsViewModel optionsViewModel;
    private readonly ServersViewModel serversViewModel;
    private readonly CreateServerViewModel createServerViewModel;
    private readonly IKeyValueStore keyValueStore;
    public static Task<string> LastFindSubnauticaTask;
    private readonly IDialogService dialogService;

    [ObservableProperty]
    private Platform gamePlatform;
    [ObservableProperty]
    private string platformToolTip;
    [ObservableProperty]
    private AvaloniaList<string> galleryImageSources = [];

    public string Version => $"{NitroxEnvironment.ReleasePhase} {NitroxEnvironment.Version}";
    public string SubnauticaLaunchArguments => keyValueStore.GetSubnauticaLaunchArguments();

    public LaunchGameViewModel(IScreen screen, IDialogService dialogService, ServersViewModel serversViewModel, CreateServerViewModel createServerViewModel, OptionsViewModel optionsViewModel, IKeyValueStore keyValueStore) : base(screen)
    {
        this.dialogService = dialogService;
        this.serversViewModel = serversViewModel;
        this.createServerViewModel = createServerViewModel;
        this.optionsViewModel = optionsViewModel;
        this.keyValueStore = keyValueStore;

        NitroxUser.GamePlatformChanged += UpdateGamePlatform;

        UpdateGamePlatform();
        
# if DEBUG
        // Launch the server and Subnautica if the -instantlaunch argument is present
        string[] launchArgs = Environment.GetCommandLineArgs();
        Task.Run(async () =>
        {
            for (int i = 0; i < launchArgs.Length; i++)
            {
                if (!launchArgs[i].Equals("-instantlaunch", StringComparison.OrdinalIgnoreCase) || launchArgs.Length <= i + 1)
                {
                    continue;
                }

                // Start the server
                string serverName = launchArgs[i + 1];
                string serverPath = Path.Combine(keyValueStore.GetSavesFolderDir(), serverName);
                if (!Directory.Exists(serverPath))
                {
                    bool result = await dialogService.ShowAsync<DialogBoxViewModel>(model =>
                    {
                        model.Description = $"The save file \"{serverName}\" does not exist. Would you still like to create it?";
                        model.DescriptionFontSize = 24;
                        model.DescriptionFontWeight = FontWeight.ExtraBold;
                        model.ButtonOptions = ButtonOptions.YesNo;
                    });

                    if (!result)
                    {
                        break;
                    }
                    
                    createServerViewModel.CreateEmptySave(serverName, NitroxGameMode.SURVIVAL);
                }
                bool serverStarted = await serversViewModel.StartServer(ServerEntry.FromDirectory(serverPath));

                // Start the game in multiplayer
                if (serverStarted)
                {
                    await StartMultiplayerAsync().ContinueWithHandleError();
                }
            }
        });
#endif

        foreach (Uri asset in AssetLoader.GetAssets(new Uri($"avares://{Assembly.GetExecutingAssembly().GetName().Name}/Assets/Images/gallery-images"), null))
        {
            GalleryImageSources.Add(asset.LocalPath);
        }
    }

    [RelayCommand]
    private async Task StartSingleplayerAsync()
    {
        if (GameInspect.IsGameRunning(GameInfo.Subnautica))
        {
            return;
        }

        LauncherNotifier.Info("Starting game");
        Log.Info("Launching Subnautica in singleplayer mode");

        try
        {
            if (string.IsNullOrWhiteSpace(NitroxUser.GamePath) || !Directory.Exists(NitroxUser.GamePath))
            {
                HostScreen.Show(optionsViewModel);
                LauncherNotifier.Warning("Location of Subnautica is unknown. Set the path to it in settings");
                return;
            }
            NitroxEntryPatch.Remove(NitroxUser.GamePath);
            await StartSubnauticaAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while starting game in singleplayer mode:");
            await dialogService.ShowErrorAsync(ex, "Error while starting game in singleplayer mode");
        }
    }

    [RelayCommand]
    private async Task StartMultiplayerAsync()
    {
        LauncherNotifier.Info("Starting game");
        Log.Info("Launching Subnautica in multiplayer mode");
        try
        {
            bool setupResult = await Task.Run(async () =>
            {
                if (string.IsNullOrWhiteSpace(NitroxUser.GamePath) || !Directory.Exists(NitroxUser.GamePath))
                {
                    await Dispatcher.UIThread.InvokeAsync(() => HostScreen.Show(optionsViewModel));
                    LauncherNotifier.Warning("Location of Subnautica is unknown. Set the path to it in settings");
                    return false;
                }
                if (PirateDetection.HasTriggered)
                {
                    LauncherNotifier.Error("Aarrr! Nitrox has walked the plank :(");
                    return false;
                }
                if (GameInspect.IsGameRunning(GameInfo.Subnautica))
                {
                    return false;
                }
                if (await GameInspect.IsOutdatedGameAndNotify(NitroxUser.GamePath, dialogService))
                {
                    return false;
                }

                // TODO: The launcher should override FileRead win32 API for the Subnautica process to give it the modified Assembly-CSharp from memory
                try
                {
                    const string PATCHER_DLL_NAME = "NitroxPatcher.dll";

                    File.Copy(
                        Path.Combine(NitroxUser.CurrentExecutablePath ?? "", "lib", "net472", PATCHER_DLL_NAME),
                        Path.Combine(NitroxUser.GamePath, GameInfo.Subnautica.DataFolder, "Managed", PATCHER_DLL_NAME),
                        true
                    );
                }
                catch (IOException ex)
                {
                    Log.Error(ex, "Unable to move initialization dll to Managed folder. Still attempting to launch because it might exist from previous runs");
                }

                // Try inject Nitrox into Subnautica code.
                if (LastFindSubnauticaTask != null)
                {
                    await LastFindSubnauticaTask;
                }
                NitroxEntryPatch.Remove(NitroxUser.GamePath);
                NitroxEntryPatch.Apply(NitroxUser.GamePath);

                if (QModHelper.IsQModInstalled(NitroxUser.GamePath))
                {
                    Log.Warn("Seems like QModManager is installed");
                    LauncherNotifier.Warning("QModManager Detected in the game folder");
                }

                return true;
            });

            if (!setupResult)
            {
                return;
            }

            await StartSubnauticaAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while starting game in multiplayer mode:");
            await Dispatcher.UIThread.InvokeAsync(async () => await dialogService.ShowErrorAsync(ex, "Error while starting game in multiplayer mode"));
        }
    }

    private async Task StartSubnauticaAsync()
    {
        string subnauticaPath = NitroxUser.GamePath;
        string subnauticaLaunchArguments = $"{SubnauticaLaunchArguments} {string.Join(" ", Environment.GetCommandLineArgs())}";
        string subnauticaExe = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            subnauticaExe = Path.Combine(subnauticaPath, "MacOS", GameInfo.Subnautica.ExeName);
        }
        else
        {
            subnauticaExe = Path.Combine(subnauticaPath, GameInfo.Subnautica.ExeName);
        }
        IGamePlatform platform = GamePlatforms.GetPlatformByGameDir(subnauticaPath);

        // Start game & gaming platform if needed.
        using ProcessEx game = platform switch
        {
            Steam s => await s.StartGameAsync(subnauticaExe, GameInfo.Subnautica.SteamAppId, subnauticaLaunchArguments),
            EpicGames e => await e.StartGameAsync(subnauticaExe, subnauticaLaunchArguments),
            MSStore m => await m.StartGameAsync(subnauticaExe),
            _ => throw new Exception($"Directory '{subnauticaPath}' is not a valid {GameInfo.Subnautica.Name} game installation or the game's platform is unsupported by Nitrox.")
        };

        if (game is null)
        {
            throw new Exception($"Game failed to start through {platform.Name}");
        }
    }

    private void UpdateGamePlatform()
    {
        GamePlatform = NitroxUser.GamePlatform?.Platform ?? Platform.NONE;
        PlatformToolTip = GamePlatform.GetAttribute<DescriptionAttribute>()?.Description ?? "Unknown";
    }
}
