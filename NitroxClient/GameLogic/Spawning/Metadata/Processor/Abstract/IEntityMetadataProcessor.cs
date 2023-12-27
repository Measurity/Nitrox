using NitroxModel.DataStructures.GameLogic.Entities.Metadata;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning.Metadata.Processor.Abstract;

public interface IEntityMetadataProcessor<in TMetadata> where TMetadata : EntityMetadata
{
    void ProcessMetadata(GameObject gameObject, TMetadata metadata);
}
