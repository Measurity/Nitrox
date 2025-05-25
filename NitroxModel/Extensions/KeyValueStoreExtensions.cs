using System.IO;
using NitroxModel.Helper;

namespace NitroxModel.Extensions;

public static class KeyValueStoreExtensions
{
    public static string GetServerSavesPath(this IKeyValueStore store)
    {
        if (store == null)
        {
            return Path.Combine(NitroxUser.AppDataPath, "saves");
        }
        return store.GetValue("ServerSavesPath", Path.Combine(NitroxUser.AppDataPath, "saves"));
    }
}
