using NitroxClient.GameLogic.Spawning.Metadata.Extractor.Abstract;
using NitroxModel.DataStructures.GameLogic.Entities.Metadata;

namespace NitroxClient.GameLogic.Spawning.Metadata.Extractor;

public class ConstructorMetadataExtractor : IEntityMetadataExtractor<Constructor, ConstructorMetadata>
{
    public ConstructorMetadata Extract(Constructor entity)
    {
        return new(entity.deployed);
    }
}
