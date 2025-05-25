using NitroxModel.Serialization;
using NitroxModel.Server;

namespace NitroxModel.Extensions;

public static class SubnauticaConfigExtensions
{
    public static bool IsHardcore(this SubnauticaServerConfig config) => config.GameMode == SubnauticaGameMode.HARDCORE;
    public static bool IsPasswordRequired(this SubnauticaServerConfig config) => config.ServerPassword != "";
}
