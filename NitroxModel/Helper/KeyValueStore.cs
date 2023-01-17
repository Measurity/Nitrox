using System;
using System.Runtime.InteropServices;
using NitroxModel.Platforms.OS.Shared;
using NitroxModel.Platforms.OS.Windows;

namespace NitroxModel.Helper;

/// <summary>
///     Simple Key-Value store that works cross-platform. <br />
/// </summary>
/// <remarks>
///     <para>
///         On <b>Windows</b>:<br />
///         Backend is <see cref="RegistryKeyValueStore" />, which uses the registry.
///         If you want to view/edit the KeyStore, open regedit and navigate to HKEY_CURRENT_USER\SOFTWARE\Nitrox\(keyname)
///     </para>
///     <para>
///         On <b>Linux</b>:<br />
///         Backend is <see cref="ConfigFileKeyValueStore" />, which uses a file.
///         If you want to view/edit the KeyStore, open $HOME/.config/Nitrox/nitrox.cfg in your favourite text editor.
///     </para>
/// </remarks>
public class KeyValueStore : IKeyValueStore
{
    public static IKeyValueStore Instance { get; } = GetKeyValueStoreForPlatform();

    private KeyValueStore()
    {
    }

    public T GetValue<T>(string key, T defaultValue = default) => throw new NotSupportedException();

    public bool SetValue<T>(string key, T value) => throw new NotSupportedException();

    public bool DeleteKey(string key) => throw new NotSupportedException();

    public bool KeyExists(string key) => throw new NotSupportedException();

    private static IKeyValueStore GetKeyValueStoreForPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Use registry on Windows
            return new RegistryKeyValueStore();
        }

        // if platform isn't Windows, it doesn't have a registry
        // use a config file for storage this should work on most platforms
        return new ConfigFileKeyValueStore();
    }
}
