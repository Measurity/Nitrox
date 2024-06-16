﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HanumanInstitute.MvvmDialogs;
using Nitrox.Launcher.Models;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.Models.Utils;
using Nitrox.Launcher.ViewModels.Abstract;
using NitroxModel.Helper;
using NitroxModel.Serialization;
using NitroxModel.Server;
using NitroxServer.Serialization;
using NitroxServer.Serialization.World;
using ReactiveUI;

namespace Nitrox.Launcher.ViewModels;

public partial class ServersViewModel : RoutableViewModelBase
{
    public static readonly string SavesFolderDir = KeyValueStore.Instance.GetValue("SavesFolderDir", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nitrox", "saves"));
    private readonly IDialogService dialogService;
    private readonly ManageServerViewModel manageServerViewModel;
    private CancellationTokenSource serverRefreshCts;
    private bool isFirstView = true;

    [ObservableProperty]
    private AvaloniaList<ServerEntry> servers = [];

    private bool shouldRefreshServersList;

    private FileSystemWatcher watcher;

    public ServersViewModel(IScreen screen, IDialogService dialogService, ManageServerViewModel manageServerViewModel) : base(screen)
    {
        this.dialogService = dialogService;
        this.manageServerViewModel = manageServerViewModel;

        GetSavesOnDisk();

        WeakReferenceMessenger.Default.Register<ServerEntryPropertyChangedMessage>(this, (sender, message) =>
        {
            if (message.PropertyName == nameof(ServerEntry.IsOnline))
            {
                ManageServerCommand.NotifyCanExecuteChanged();
            }
        });
        WeakReferenceMessenger.Default.Register<SaveDeletedMessage>(this, (sender, message) =>
        {
            for (int i = Servers.Count - 1; i >= 0; i--)
            {
                if (Servers[i].Name == message.SaveName)
                {
                    Servers.RemoveAt(i);
                }
            }
        });

        this.WhenActivated(disposables =>
        {
            // Activation
            serverRefreshCts = new();
            if (!isFirstView)
            {
                GetSavesOnDisk();
            }
            isFirstView = false;
            InitializeWatcher();

            // Deactivation
            Disposable
                .Create(this, vm =>
                {
                    vm.watcher?.Dispose();
                    vm.serverRefreshCts.Cancel();
                })
                .DisposeWith(disposables);
        });
    }

    [RelayCommand]
    public async Task CreateServer(IInputElement focusTargetOnClose = null)
    {
        CreateServerViewModel result = await dialogService.ShowAsync<CreateServerViewModel>();
        if (result == null)
        {
            Dispatcher.UIThread.Post(() => focusTargetOnClose?.Focus());
            return;
        }

        Dispatcher.UIThread.Post(() => focusTargetOnClose?.Focus());
        AddServer(result.Name, result.SelectedGameMode);
    }

    [RelayCommand]
    public async Task StartServer(ServerEntry server)
    {
        if (server.Version != NitroxEnvironment.Version && !await ConfirmServerVersionAsync(server)) // TODO: Exclude upgradeable versions + add separate prompt to upgrade first?
        {
            return;
        }

        server.Start();
        server.Version = NitroxEnvironment.Version;
    }

    [RelayCommand]
    public async Task ManageServer(ServerEntry server)
    {
        if (server.Version != NitroxEnvironment.Version && !await ConfirmServerVersionAsync(server)) // TODO: Exclude upgradeable versions + add separate prompt to upgrade first?
        {
            return;
        }

        manageServerViewModel.LoadFrom(server);
        HostScreen.Show(manageServerViewModel);
    }

    private async Task<bool> ConfirmServerVersionAsync(ServerEntry server)
    {
        DialogBoxViewModel modelViewModel = await dialogService.ShowAsync<DialogBoxViewModel>(model =>
        {
            model.Description = $"The version of '{server.Name}' is v{server.Version}. It is highly recommended to NOT use this save file with Nitrox v{NitroxEnvironment.Version}. Would you still like to continue?";
            model.DescriptionFontSize = 24;
            model.DescriptionFontWeight = FontWeight.ExtraBold;
            model.ButtonOptions = ButtonOptions.YesNo;
        });
        return modelViewModel != null;
    }

    public void GetSavesOnDisk()
    {
        Directory.CreateDirectory(SavesFolderDir);

        List<ServerEntry> serversOnDisk = [];

        foreach (string folder in Directory.EnumerateDirectories(SavesFolderDir))
        {
            // Don't add the file to the list if it doesn't validate
            if (!File.Exists(Path.Combine(folder, "server.cfg")) || !File.Exists(Path.Combine(folder, "Version.json")))
            {
                continue;
            }

            string saveName = Path.GetFileName(folder);
            string saveDir = Path.Combine(SavesFolderDir, saveName);

            Bitmap serverIcon = null;
            string serverIconPath = Path.Combine(saveDir, "servericon.png");
            if (File.Exists(serverIconPath))
            {
                serverIcon = new(Path.Combine(saveDir, "servericon.png"));
            }

            SubnauticaServerConfig server = SubnauticaServerConfig.Load(saveDir);
            string fileEnding = "json";
            if (server.SerializerMode == ServerSerializerMode.PROTOBUF) { fileEnding = "nitrox"; }

            Version version;
            using (FileStream stream = new(Path.Combine(folder, $"Version.{fileEnding}"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                version = new ServerJsonSerializer().Deserialize<SaveFileVersion>(stream)?.Version ?? NitroxEnvironment.Version;
            }

            ServerEntry entry = new()
            {
                Name = saveName,
                ServerIcon = serverIcon,
                Password = server.ServerPassword,
                Seed = server.Seed,
                GameMode = server.GameMode,
                PlayerPermissions = server.DefaultPlayerPerm,
                AutoSaveInterval = server.SaveInterval / 1000,
                MaxPlayers = server.MaxConnections,
                Port = server.ServerPort,
                AutoPortForward = server.AutoPortForward,
                AllowLanDiscovery = server.LANDiscoveryEnabled,
                AllowCommands = !server.DisableConsole,
                IsNewServer = !File.Exists(Path.Combine(saveDir, "WorldData.json")),
                Version = version,
                LastAccessedTime = File.GetLastWriteTime(File.Exists(Path.Combine(folder, $"WorldData.{fileEnding}"))
                                                             ?
                                                             // This file is affected by server saving
                                                             Path.Combine(folder, $"WorldData.{fileEnding}")
                                                             :
                                                             // If the above file doesn't exist (server was never ran), use the Version file instead
                                                             Path.Combine(folder, $"Version.{fileEnding}"))
            };

            serversOnDisk.Add(entry);
        }

        // Remove any servers from the Servers list that are not found in the saves folder
        for (int i = Servers.Count - 1; i >= 0; i--)
        {
            if (serversOnDisk.All(s => s.Name != Servers[i].Name))
            {
                Servers.RemoveAt(i);
            }
        }

        // Add any new servers found on the disk to the Servers list
        foreach (ServerEntry server in serversOnDisk)
        {
            if (Servers.All(s => s.Name != server.Name))
            {
                Servers.Add(server);
            }
        }

        Servers = new AvaloniaList<ServerEntry>(Servers.OrderByDescending(entry => entry.LastAccessedTime));
    }

    private void AddServer(string name, NitroxGameMode gameMode)
    {
        Servers.Insert(0, new ServerEntry
        {
            Name = name,
            GameMode = gameMode,
            Seed = "",
            Version = NitroxEnvironment.Version
        });
    }

    private void InitializeWatcher()
    {
        watcher = new FileSystemWatcher
        {
            Path = SavesFolderDir,
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
            Filter = "*.*",
            IncludeSubdirectories = true
        };
        watcher.Changed += OnDirectoryChanged;
        watcher.Created += OnDirectoryChanged;
        watcher.Deleted += OnDirectoryChanged;
        watcher.Renamed += OnDirectoryChanged;
        watcher.EnableRaisingEvents = true;

        Task.Run(async () =>
        {
            while (!serverRefreshCts.IsCancellationRequested)
            {
                while (shouldRefreshServersList)
                {
                    try
                    {
                        GetSavesOnDisk();
                        shouldRefreshServersList = false;
                    }
                    catch (IOException)
                    {
                        await Task.Delay(500);
                    }
                }
                await Task.Delay(1000);
            }
        }).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                LauncherNotifier.Error(t.Exception.Message);
            }
        });
    }

    private void OnDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        shouldRefreshServersList = true;
    }
}
