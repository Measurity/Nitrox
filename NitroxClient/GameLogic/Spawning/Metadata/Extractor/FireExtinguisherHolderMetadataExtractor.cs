using NitroxClient.GameLogic.Spawning.Metadata.Extractor.Abstract;
using NitroxModel.DataStructures.GameLogic.Entities.Metadata;

namespace NitroxClient.GameLogic.Spawning.Metadata.Extractor;

public class FireExtinguisherHolderMetadataExtractor : IEntityMetadataExtractor<FireExtinguisherHolder, FireExtinguisherHolderMetadata>
{
    public FireExtinguisherHolderMetadata Extract(FireExtinguisherHolder entity)
    {
        return new(entity.hasTank, entity.fuel);
    }
}
