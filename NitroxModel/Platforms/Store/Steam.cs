using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NitroxModel.Discovery.Models;
using NitroxModel.Helper;
using NitroxModel.Platforms.OS.Shared;
using NitroxModel.Platforms.OS.Windows;
using NitroxModel.Platforms.Store.Exceptions;
using NitroxModel.Platforms.Store.Interfaces;

namespace NitroxModel.Platforms.Store;

public sealed class Steam : IGamePlatform
{
    private static Steam instance;
    public static Steam Instance => instance ??= new Steam();
    public string Name => nameof(Steam);
    public Platform Platform => Platform.STEAM;

    public bool OwnsGame(string gameDirectory)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (Directory.Exists(Path.Combine(gameDirectory, "Plugins", "steam_api.bundle")))
            {
                return true;
            }
        }
        else
        {
            if (File.Exists(Path.Combine(gameDirectory, GameInfo.Subnautica.DataFolder, "Plugins", "x86_64", "steam_api64.dll")))
            {
                return true;
            }
            if (File.Exists(Path.Combine(gameDirectory, GameInfo.Subnautica.DataFolder, "Plugins", "steam_api64.dll")))
            {
                return true;
            }
        }
        return false;
    }

    public async Task<ProcessEx> StartPlatformAsync()
    {
        // If steam is already running, do not start it.
        // TODO: fix this for macos p => p.MainModuleDirectory != null && File.Exists(Path.Combine(p.MainModuleDirectory, "steamclient.dll"))
        ProcessEx steam = ProcessEx.GetFirstProcess("steam");
        if (steam != null)
        {
            return steam;
        }

        // Steam is not running, start it.
        string exe = GetExeFile();
        if (exe == null)
        {
            return null;
        }
        steam = new ProcessEx(Process.Start(new ProcessStartInfo
        {
            WorkingDirectory = Path.GetDirectoryName(exe) ?? Directory.GetCurrentDirectory(),
            FileName = exe,
            WindowStyle = ProcessWindowStyle.Minimized,
            Arguments = "-silent" // Don't show Steam window
        }));

        // Wait for steam to write to its log file, which indicates it's ready to start games.
        using CancellationTokenSource steamReadyCts = new(TimeSpan.FromSeconds(30));
        try
        {
            DateTime consoleLogFileLastWrite = GetSteamConsoleLogLastWrite(Path.GetDirectoryName(exe));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await RegistryEx.CompareAsync<int>(@"SOFTWARE\Valve\Steam\ActiveProcess\ActiveUser",
                                                   v => v > 0,
                                                   steamReadyCts.Token);
            }
            while (consoleLogFileLastWrite == GetSteamConsoleLogLastWrite(Path.GetDirectoryName(exe)) && !steamReadyCts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(250, steamReadyCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return steam;
    }

    public string GetExeFile()
    {
        string exe = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            exe = Path.Combine(RegistryEx.Read(@"SOFTWARE\Valve\Steam\SteamPath", ""), "steam.exe");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Steam", "Steam.AppBundle", "Steam", "Contents", "MacOS", "steam_osx");
        }

        return File.Exists(exe) ? Path.GetFullPath(exe) : null;
    }

    public async Task<ProcessEx> StartGameAsync(string pathToGameExe, int steamAppId, string launchArguments)
    {
        try
        {
            using ProcessEx steam = await StartPlatformAsync();
            if (steam == null)
            {
                throw new PlatformException(Instance, "Steam is not running and could not be found.");
            }
        }
        catch (OperationCanceledException ex)
        {
            throw new PlatformException(Instance, "Timeout reached while waiting for platform to start. Try again once platform has finished loading.", ex);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ProcessEx.Start(
                pathToGameExe,
                [("SteamGameId", steamAppId.ToString()), ("SteamAppID", steamAppId.ToString()), (NitroxUser.LAUNCHER_PATH_ENV_KEY, NitroxUser.LauncherPath)],
                Path.GetDirectoryName(pathToGameExe),
                launchArguments
            );
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return StartGameWithProton(pathToGameExe, steamAppId, launchArguments);
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = pathToGameExe,
            WorkingDirectory = Path.GetDirectoryName(pathToGameExe) ?? "",
            UseShellExecute = false,
            Environment =
            {
                [NitroxUser.LAUNCHER_PATH_ENV_KEY] = NitroxUser.LauncherPath,
                ["SteamGameId"] = steamAppId.ToString(),
                ["SteamAppID"] = steamAppId.ToString()
            }
        };
        Log.Info($"Starting game with arguments: {startInfo.FileName} {launchArguments}");
        return new ProcessEx(Process.Start(startInfo));
    }

    private ProcessEx StartGameWithProton(string pathToGameExe, int steamAppId, string launchArguments)
    {
        // function to get library path for given game id
        static string GetLibraryPath(string steamPath, string gameId)
        {
            string libraryFoldersPath = Path.Combine(steamPath, "config", "libraryfolders.vdf");
            string content = File.ReadAllText(libraryFoldersPath);

            // Regex to match library folder entries
            Regex folderRegex = new(@"""(\d+)""\s*\{[^}]*""path""\s*""([^""]+)""[^}]*""apps""\s*\{([^}]+)\}", RegexOptions.Singleline);
            MatchCollection matches = folderRegex.Matches(content);

            foreach (Match match in matches)
            {
                string path = match.Groups[2].Value;
                string apps = match.Groups[3].Value;

                // Check if the gameId exists in the apps section
                if (Regex.IsMatch(apps, $@"""{gameId}""\s*""[^""]+"""))
                {
                    return path;
                }
            }

            return ""; // Return empty string if not found
        }

        static List<string> GetAllLibraryPaths(string steamPath)
        {
            string libraryFoldersPath = Path.Combine(steamPath, "config", "libraryfolders.vdf");
            string content = File.ReadAllText(libraryFoldersPath);

            // Regex to match library folder entries
            Regex folderRegex = new(@"""(\d+)""\s*\{[^}]*""path""\s*""([^""]+)""", RegexOptions.Singleline);
            MatchCollection matches = folderRegex.Matches(content);

            List<string> libraryPaths = [];
            foreach (Match match in matches)
            {
                string path = match.Groups[2].Value.Replace("\\\\", "\\");
                if (Directory.Exists(Path.Combine(path, "steamapps", "common")))
                {
                    libraryPaths.Add(path);
                }
            }
            // Add the default Steam library path
            string defaultLibraryPath = Path.Combine(steamPath);
            if (!libraryPaths.Contains(defaultLibraryPath))
            {
                libraryPaths.Add(defaultLibraryPath);
            }

            return libraryPaths;
        }
        
        static string GetProtonVersionFromConfigVdf(string configVdfFile, string appId)
        {
            try
            {
                string fileContent = File.ReadAllText(configVdfFile);
                Match compatToolMatch = Regex.Match(fileContent, @"""CompatToolMapping""\s*{((?:\s*""\d+""[^{]+[^}]+})*)\s*}");

                if (compatToolMatch.Success)
                {
                    string compatToolMapping = compatToolMatch.Groups[1].Value;
                    string appIdPattern = $@"""{appId}""[^{{]*\{{[^}}]*""name""\s*""([^""]+)""";
                    Match appIdMatch = Regex.Match(compatToolMapping, appIdPattern);

                    if (appIdMatch.Success)
                    {
                        return appIdMatch.Groups[1].Value;
                    }
                }

                return "Proton version not found for the given appId";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        string userHomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userHomePath))
        {
            userHomePath = Environment.GetEnvironmentVariable("HOME");
        }
        if (!Directory.Exists(userHomePath))
        {
            throw new Exception("User HOME is not known");
        }

        string compatdataPath = "";
        if (!string.IsNullOrEmpty(pathToGameExe))
        {
            string[] pathComponents = pathToGameExe.Split(Path.DirectorySeparatorChar);
            int steamAppsIndex = pathComponents.GetIndex("steamapps");
            if (steamAppsIndex != -1)
            {
                string steamAppsPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathComponents, 0, steamAppsIndex + 1);
                compatdataPath = Path.Combine(steamAppsPath, "compatdata", steamAppId.ToString());
            }
        }
        string steamPath = Path.Combine(userHomePath, ".steam/steam");
        string geProtonPath = Path.Combine(userHomePath, ".steam/root/compatibilitytools.d/");
        // support flatpak
        if (!Directory.Exists(steamPath))
        {
            steamPath = Path.Combine(userHomePath, ".var/app/com.valvesoftware.Steam/data/Steam/");
            geProtonPath = Path.Combine(userHomePath, ".var/app/com.valvesoftware.Steam/data/Steam/compatibilitytools.d/");
        }

        string sniperappid = "1628350";
        string sniperruntimepath = Path.Combine(GetLibraryPath(steamPath, sniperappid), "steamapps", "common", "SteamLinuxRuntime_sniper");

        string protonVersion = GetProtonVersionFromConfigVdf(Path.Combine(steamPath, "config", "config.vdf"), steamAppId.ToString());
        if (protonVersion == "Proton version not found for the given appId")
        {
            protonVersion = "proton_9";
        }
        string protonDir = null;
        bool isValveProton = protonVersion.StartsWith("proton_", StringComparison.OrdinalIgnoreCase);
        if (isValveProton)
        {
            int index = protonVersion.IndexOf("proton_", StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                protonVersion = protonVersion.Substring(index + "proton_".Length);
            }
            if (protonVersion == "experimental")
            {
                protonVersion = "-";
            }

            foreach (string path in GetAllLibraryPaths(steamPath))
            {
                foreach (string dir in Directory.EnumerateDirectories(Path.Combine(path, "steamapps", "common")))
                {
                    if (dir.Contains($"Proton {protonVersion}"))
                    {
                        protonDir = dir;
                        break;
                    }
                }
                if (protonDir != null)
                {
                    break;
                }
            }
        }
        else
        {
            protonDir = Path.Combine(geProtonPath, protonVersion);
        }
        if (protonDir == null)
        {
            throw new Exception("Game is not using Proton. Please change game properties in Steam to use the Proton compatibility layer.");
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = Path.Combine(sniperruntimepath, "_v2-entry-point"),
            Arguments = $" --verb=run -- \"{Path.Combine(protonDir, "proton")}\" run \"{pathToGameExe}\" {launchArguments}",
            WorkingDirectory = Path.GetDirectoryName(pathToGameExe) ?? "",
            UseShellExecute = false,
            Environment =
            {
                [NitroxUser.LAUNCHER_PATH_ENV_KEY] = NitroxUser.LauncherPath,
                ["SteamGameId"] = steamAppId.ToString(),
                ["SteamAppID"] = steamAppId.ToString(),
                ["STEAM_COMPAT_APP_ID"] = steamAppId.ToString(),
                ["WINEPREFIX"] = compatdataPath,
                ["STEAM_COMPAT_CLIENT_INSTALL_PATH"] = steamPath,
                ["STEAM_COMPAT_DATA_PATH"] = compatdataPath,
            }
        };
        return new ProcessEx(Process.Start(startInfo));
    }

    private DateTime GetSteamConsoleLogLastWrite(string exePath) => File.GetLastWriteTime(Path.Combine(Path.GetDirectoryName(exePath), "logs", "console_log.txt"));
}
