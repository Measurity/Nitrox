using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using NitroxModel.Helper;

namespace NitroxModel.Platforms.OS.Shared
{
    public class ConfigFileBackingStore : IKVStore
    {
        private readonly Dictionary<string, object> keyValuePairs = new();
        public string FolderPath { get; }
        public string FilePath
        {
            get
            {
                return Path.Combine(FolderPath, "nitrox.cfg");
            }
        }
        public T GetValue<T>(string key, T defaultValue)
        {
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
            else
            {
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
        }
        public bool SetValue<T>(string key, T val)
        {
            keyValuePairs[key] = val;
            return SaveConfig();
        }
        public ConfigFileBackingStore()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
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
            else
            {
                // we would include a platform in the error message, but .NET provides no facilities to get a platform, only to check if we're running on it
                throw new NotSupportedException("Unsupported platform for ConfigFileBackingStore; your platform was not detected to be OSPlatform.Linux");
            }
            LoadConfig();
        }
        public bool SaveConfig()
        {
            // create directories if they don't already exist
            Directory.CreateDirectory(FolderPath);
            string serialized;
            try
            {
                serialized = JsonSerializer.Serialize(keyValuePairs, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                });
            }
            catch (Exception)
            {
                return false;
            }

            try
            {
                File.WriteAllText(FilePath, serialized);
            }
            catch (Exception)
            {
                return false;
            }

            return File.Exists(FilePath);
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
            return SaveConfig();
        }
        public bool KeyExists(string key)
        {
            return keyValuePairs.ContainsKey(key);
        }
    }
}