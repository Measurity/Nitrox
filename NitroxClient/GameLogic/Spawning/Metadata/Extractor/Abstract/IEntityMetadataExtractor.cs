using NitroxModel.DataStructures.GameLogic.Entities.Metadata;

namespace NitroxClient.GameLogic.Spawning.Metadata.Extractor.Abstract;

public interface IEntityMetadataExtractor<in TIn, out TOut> where TOut : EntityMetadata
{
    public TOut Extract(TIn o);
}
