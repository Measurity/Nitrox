namespace NitroxModel.Helper
{
    public interface IKVStore
    {
        T GetValue<T>(string key, T defaultValue);
        bool SetValue<T>(string key, T val);
        bool DeleteKey(string key);
        bool KeyExists(string key);
    }
}