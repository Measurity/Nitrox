using NitroxClient.GameLogic.Spawning.Metadata.Extractor.Abstract;
using NitroxModel.DataStructures.GameLogic.Entities.Metadata;

namespace NitroxClient.GameLogic.Spawning.Metadata.Extractor;

public class BeaconMetadataExtractor : IEntityMetadataExtractor<Beacon, BeaconMetadata>
{
    public BeaconMetadata Extract(Beacon beacon)
    {
        return new(beacon.beaconLabel.GetLabel());
    }
}
