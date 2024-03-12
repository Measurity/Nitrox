﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nitrox.Launcher.Models.Patching;
using Nitrox.Launcher.ViewModels.Abstract;
using NitroxModel;
using NitroxModel.Discovery;
using NitroxModel.Discovery.Models;
using NitroxModel.Helper;
using NitroxModel.Platforms.OS.Shared;
using ReactiveUI;

namespace Nitrox.Launcher.ViewModels;

public partial class OptionsViewModel : RoutableViewModelBase
{
    //public AvaloniaList<KnownGame> KnownGames { get; init; }

    private readonly string savesFolderDir = KeyValueStore.Instance.GetValue("SavesFolderDir", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nitrox", "saves"));
    
    private static string defaultLaunchArg => "-vrmode none";
    
    public string SubnauticaPath => KeyValueStore.Instance.GetValue<string>("SubnauticaPath");
    public string SubnauticaLaunchArguments => KeyValueStore.Instance.GetValue<string>("SubnauticaLaunchArguments", "-vrmode none");
    
    [ObservableProperty]
    private KnownGame selectedGame;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangeArgumentsCommand))]
    private string launchArgs;
    
    [ObservableProperty]
    private bool showResetArgsBtn;
    
    public OptionsViewModel(IScreen hostScreen) : base(hostScreen)
    {
        SelectedGame = new()
        {
            PathToGame = NitroxUser.GamePath,
            Platform = NitroxUser.GamePlatform?.Platform ?? Platform.NONE
        };
        //KnownGames =
        //[
        //    new()
        //    {
        //        PathToGame = NitroxUser.GamePath,
        //        Platform = NitroxUser.GamePlatform?.Platform ?? Platform.NONE
        //    }
        //];

        LaunchArgs = KeyValueStore.Instance.GetValue<string>("SubnauticaLaunchArguments", defaultLaunchArg);
    }

    [RelayCommand]
    private async void ChangePath()
    {
        // TODO: Maybe use Window.StorageProvider API instead of OpenFileDialog
        OpenFolderDialog dialog = new()
        {
            Title = "Select Subnautica installation directory",
            Directory = new(SelectedGame.PathToGame)
        };
        string selectedDirectory = await dialog.ShowAsync(MainWindow) ?? "";
        
        if (selectedDirectory == "")
        {
            LaunchArgs = "Cancelled";    //TEMP
            return;
        }
        
        if (!GameInstallationHelper.HasGameExecutable(selectedDirectory, GameInfo.Subnautica))
        {
            //LauncherNotifier.Error("Invalid subnautica directory");
            LaunchArgs = "Invalid subnautica directory";    //TEMP
            return;
        }
        
        if (selectedDirectory != SelectedGame.PathToGame)
        {
            await SetTargetedSubnauticaPath(selectedDirectory);
            //LauncherNotifier.Success("Applied changes");
            LaunchArgs = "Applied changes";    //TEMP
        }
    }

    //[RelayCommand]
    //private void AddGameInstallation()
    //{
    //}

    public async Task<string> SetTargetedSubnauticaPath(string path)
    {
        if ((string.IsNullOrWhiteSpace(SubnauticaPath) && SubnauticaPath == path) || !Directory.Exists(path))
        {
            return null;
        }

        KeyValueStore.Instance.SetValue("SubnauticaPath", path);
        if (LaunchGameViewModel.LastFindSubnauticaTask != null)
        {
            await LaunchGameViewModel.LastFindSubnauticaTask;
        }

        LaunchGameViewModel.LastFindSubnauticaTask = Task.Factory.StartNew(() =>
        {
            PirateDetection.TriggerOnDirectory(path);

            if (!FileSystem.Instance.IsWritable(Directory.GetCurrentDirectory()) || !FileSystem.Instance.IsWritable(path))
            {
                // TODO: Move this check to another place where Nitrox installation can be verified. (i.e: another page on the launcher in order to check permissions, network setup, ...)
                if (!FileSystem.Instance.SetFullAccessToCurrentUser(Directory.GetCurrentDirectory()) || !FileSystem.Instance.SetFullAccessToCurrentUser(path))
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        //MessageBox.Show(Application.Current.MainWindow!, "Restart Nitrox Launcher as admin to allow Nitrox to change permissions as needed. This is only needed once. Nitrox will close after this message.", "Required file permission error", MessageBoxButton.OK,
                        //                MessageBoxImage.Error);
                        Environment.Exit(1);
                    }, DispatcherPriority.ApplicationIdle);
                }
            }
            
            // Save game path as preferred for future sessions.
            NitroxUser.PreferredGamePath = path;
            
            if (LaunchGameViewModel.NitroxEntryPatch?.IsApplied == true)
            {
                LaunchGameViewModel.NitroxEntryPatch.Remove();
            }
            LaunchGameViewModel.NitroxEntryPatch = new NitroxEntryPatch(() => SubnauticaPath);

            //if (Path.GetFullPath(path).StartsWith(WindowsHelper.ProgramFileDirectory, StringComparison.OrdinalIgnoreCase))
            //{
            //    WindowsHelper.RestartAsAdmin();
            //}

            return path;
        }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

        return await LaunchGameViewModel.LastFindSubnauticaTask;
    }
    
    [RelayCommand]
    private void ResetArguments()
    {
        LaunchArgs = defaultLaunchArg;
        ShowResetArgsBtn = false;
    }
    
    [RelayCommand(CanExecute = nameof(CanChangeArguments))]
    private void ChangeArguments()
    {
        KeyValueStore.Instance.SetValue("SubnauticaLaunchArguments", LaunchArgs);
    }
    private bool CanChangeArguments()
    {
        if (LaunchArgs != defaultLaunchArg)
        {
            ShowResetArgsBtn = true;
        }

        return LaunchArgs != KeyValueStore.Instance.GetValue<string>("SubnauticaLaunchArguments", defaultLaunchArg);
    }

    [RelayCommand]
    private void ViewFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = savesFolderDir,
            Verb = "open",
            UseShellExecute = true
        })?.Dispose();
    }
    
    public class KnownGame
    {
        public string PathToGame { get; init; }
        public Platform Platform { get; init; }
    }
}
