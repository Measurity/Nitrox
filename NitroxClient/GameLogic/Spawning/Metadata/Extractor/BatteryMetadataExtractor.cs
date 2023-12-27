using NitroxClient.GameLogic.Spawning.Metadata.Extractor.Abstract;
using NitroxModel.DataStructures.GameLogic.Entities.Metadata;
using NitroxModel.DataStructures.Util;

namespace NitroxClient.GameLogic.Spawning.Metadata.Extractor;

public class BatteryMetadataExtractor : IEntityMetadataExtractor<Battery, BatteryMetadata>
{
    public BatteryMetadata Extract(Battery entity)
    {
        return new(entity._charge);
    }
}
