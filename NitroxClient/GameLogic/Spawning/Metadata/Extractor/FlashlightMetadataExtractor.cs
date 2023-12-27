using NitroxClient.GameLogic.Spawning.Metadata.Extractor.Abstract;
using NitroxClient.Unity.Helper;
using NitroxModel.DataStructures.GameLogic.Entities.Metadata;

namespace NitroxClient.GameLogic.Spawning.Metadata.Extractor;

public class FlashlightMetadataExtractor : IEntityMetadataExtractor<FlashLight, FlashlightMetadata>
{
    public FlashlightMetadata Extract(FlashLight entity)
    {
        ToggleLights lights = entity.RequireComponent<ToggleLights>();

        return new(lights.lightsActive);
    }
}
