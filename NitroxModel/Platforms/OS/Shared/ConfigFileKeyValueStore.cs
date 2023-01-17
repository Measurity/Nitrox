using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using NitroxModel.Helper;

namespace NitroxModel.Platforms.OS.Shared;

public class ConfigFileKeyValueStore : IKeyValueStore
{
    private bool hasLoaded = false;
    private readonly Dictionary<string, object> keyValuePairs = new();
    public string FolderPath { get; }
    public string FilePath => Path.Combine(FolderPath, "nitrox.cfg");

    public ConfigFileKeyValueStore()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // we would include a platform in the error message, but .NET provides no facilities to get a platform, only to check if we're running on it
            throw new NotSupportedException("Unsupported platform for ConfigFileBackingStore; your platform was not detected to be OSPlatform.Linux");
        }

        // LocalApplicationData's default is $HOME/.config under linux and XDG_CONFIG_HOME if set
        // What is the difference between .config and .local/share?
        // .config should contain all config files.
        // .local/share should contain data that isn't config files (binary blobs, downloaded data, server saves).
        // .cache should house all cache files (files that can be safely deleted to free up space)
        string localShare = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrEmpty(localShare))
        {
            throw new Exception("Could not determine where to save configs. Check HOME and XDG_CONFIG_HOME variables.");
        }
        FolderPath = Path.Combine(localShare, "Nitrox");
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        if (!hasLoaded)
        {
            LoadConfig();
        }

        bool succeeded = keyValuePairs.TryGetValue(key, out object obj);
        if (!succeeded)
        {
            return defaultValue;
        }
        if (obj is JsonElement element)
        {
            // System.Text.Json stores objects as JsonElement
            try
            {
                return element.Deserialize<T>();
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
        // if a value has been added at runtime and not deserialized, it should be casted directly
        try
        {
            return (T)obj;
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    public bool SetValue<T>(string key, T value)
    {
        keyValuePairs[key] = value;
        SaveConfig();
        return true;
    }

    private void SaveConfig()
    {
        // Create directories if they don't already exist
        Directory.CreateDirectory(FolderPath);

        string serialized = JsonSerializer.Serialize(keyValuePairs, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, serialized);
    }

    public bool LoadConfig()
    {
        Dictionary<string, string> deserialized;
        try
        {
            deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath));
        }
        catch (Exception)
        {
            return false;
        }
        if (deserialized == null)
        {
            return false;
        }

        foreach (KeyValuePair<string, string> item in deserialized)
        {
            keyValuePairs.Add(item.Key, item.Value);
        }
        return true;
    }

    public bool DeleteKey(string key)
    {
        if (!keyValuePairs.Remove(key))
        {
            return false;
        }
        SaveConfig();
        return true;
    }

    public bool KeyExists(string key) => keyValuePairs.ContainsKey(key);
}
