using System.Collections.Generic;
using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Nitrox.Server.Subnautica.Models.Resources.Core;
using WorldEntityInfo = UWE.WorldEntityInfo;

namespace Nitrox.Server.Subnautica.Models.Resources.Parsers;

internal class WorldEntitiesResource(SubnauticaAssetsManager assetsManager, IOptions<ServerStartOptions> options) : IGameResource
{
    private readonly SubnauticaAssetsManager assetsManager = assetsManager;
    private readonly IOptions<ServerStartOptions> startOptions = options;
    private Task<Dictionary<string, WorldEntityInfo>>? worldEntitiesByClassId;
    public Dictionary<string, WorldEntityInfo> WorldEntitiesByClassId => GetWorldEntitiesByClassIdAsync().GetAwaiter().GetResult();

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        worldEntitiesByClassId = GetWorldEntitiesByClassIdAsync(cancellationToken);
        return Task.CompletedTask;
    }

    private async Task<Dictionary<string, WorldEntityInfo>> GetWorldEntitiesByClassIdAsync(CancellationToken cancellationToken = default)
    {
        if (worldEntitiesByClassId != null)
        {
            await worldEntitiesByClassId;
        }

        Dictionary<string, WorldEntityInfo> result = [];

        cancellationToken.ThrowIfCancellationRequested();
        AssetsFileInstance assetFile = assetsManager.LoadAssetsFile(Path.Combine(startOptions.Value.GetSubnauticaResourcesPath(), "resources.assets"), true);
        AssetFileInfo assetFileInfo = assetFile.file.GetAssetInfo(assetsManager, "WorldEntityData", AssetClassID.MonoBehaviour);
        AssetTypeValueField assetValue = assetsManager.GetBaseField(assetFile, assetFileInfo);

        foreach (AssetTypeValueField info in assetValue["infos"])
        {
            cancellationToken.ThrowIfCancellationRequested();
            WorldEntityInfo entityData = new()
            {
                classId = info["classId"].AsString,
                techType = (TechType)info["techType"].AsInt,
                slotType = (EntitySlot.Type)info["slotType"].AsInt,
                prefabZUp = info["prefabZUp"].AsBool,
                cellLevel = (LargeWorldEntity.CellLevel)info["cellLevel"].AsInt,
                localScale = info["localScale"].ToVector3()
            };

            result.Add(entityData.classId, entityData);
        }

        Validate.IsTrue(result.Count > 0);
        return result;
    }
}
