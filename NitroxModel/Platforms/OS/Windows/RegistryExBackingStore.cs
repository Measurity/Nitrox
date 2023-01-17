using System;
using NitroxModel.Helper;
using NitroxModel.Platforms.OS.Windows.Internal;

namespace NitroxModel.Platforms.OS.Windows
{
    public class RegistryExBackingStore : IKVStore
    {
        public T GetValue<T>(string key, T defaultValue)
        {
            return RegistryEx.Read(KeyToRegistryPath(key), defaultValue);
        }
        public bool SetValue<T>(string key, T val)
        {
            try
            {
                RegistryEx.Write(KeyToRegistryPath(key), val);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool DeleteKey(string key)
        {
            return RegistryEx.Delete(KeyToRegistryPath(key));
        }
        public bool KeyExists(string key)
        {
            return RegistryEx.Exists(KeyToRegistryPath(key));
        }
        public static string KeyToRegistryPath(string key)
        {
            return @$"SOFTWARE\Nitrox\{key}";
        }
        public RegistryExBackingStore()
        {

        }
    }
}