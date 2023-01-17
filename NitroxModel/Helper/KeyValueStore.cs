using System.Runtime.InteropServices;
using NitroxModel.Platforms.OS.Shared;
using NitroxModel.Platforms.OS.Windows;

namespace NitroxModel.Helper
{
    /// <summary>
    /// Simple Key-Value store, that works cross-platform. <br/>
    /// On Windows: <br/>
    /// Backend is RegistryExBackingStore, which uses the registry. <br/>
    /// If you want to view/edit the KeyStore, open regedit and navigate to HKEY_CURRENT_USER\SOFTWARE\Nitrox\(keyname) <br/>
    /// On Linux: <br/>
    /// Backend is ConfigFileBackingStore, which uses a file. <br/>
    /// If you want to view/edit the KeyStore, open $HOME/.config/Nitrox/nitrox.cfg in your favourite text editor. <br/>
    /// </summary>
    public static class KeyValueStore
    {
        private static IKVStore backingStore;
        private static bool isSetup;
        /// <summary>
        ///     Gets a value for a key. <br/>
        ///     Returns null if: <br/>
        ///     - If the key doesn't exist <br/>
        ///     - If the conversion failed <br/>
        /// </summary>
        /// <param name="key">Key to get value of.</param>
        /// <returns>The value of the key or null.</returns>
        public static T GetValue<T>(string key, T defaultValue = default)
        {
            TrySetup();
            return backingStore.GetValue<T>(key, defaultValue);
        }

        /// <summary>
        ///     Sets a value for a key.
        /// </summary>
        /// <param name="key">Key to set value of.</param>
        /// <returns>True if the value got set AND saved successfully.</returns>
        public static bool SetValue<T>(string key, T val)
        {
            TrySetup();
            return backingStore.SetValue<T>(key, val);
        }

        /// <summary>
        ///     Deletes a key along with it's value.
        /// </summary>
        /// <param name="key">Key to delete.</param>
        /// <returns>True if the key was deleted.</returns>
        public static bool DeleteKey(string key)
        {
            TrySetup();
            return backingStore.DeleteKey(key);
        }

        /// <summary>
        ///     Check if a key exists.
        /// </summary>
        /// <param name="key">Key to check.</param>
        /// <returns>True if the key exists.</returns>
        public static bool KeyExists(string key)
        {
            TrySetup();
            return backingStore.KeyExists(key);
        }

        private static void TrySetup()
        {
            // setup shouldn't be run twice
            if (isSetup)
            {
                return;
            }

            // Use registry on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                backingStore = new RegistryExBackingStore();
            }
            else
            {
                // if platform isn't windows, it doesn't have a registry
                // use a config file for storage
                // this should work on most platforms
                backingStore = new ConfigFileBackingStore();
            }
            isSetup = true;
        }
    }
}