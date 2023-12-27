using NitroxClient.GameLogic.Spawning.Metadata.Processor.Abstract;
using NitroxModel.DataStructures.GameLogic.Entities.Metadata;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning.Metadata.Processor;

public class StarshipDoorMetadataProcessor : IEntityMetadataProcessor<StarshipDoorMetadata>
{
    public void ProcessMetadata(GameObject gameObject, StarshipDoorMetadata metadata)
    {
        StarshipDoor starshipDoor = gameObject.GetComponent<StarshipDoor>();
        starshipDoor.doorOpen = metadata.DoorOpen;
        starshipDoor.doorLocked = metadata.DoorLocked;
        if (metadata.DoorLocked)
        {
            starshipDoor.LockDoor();
        }
        else
        {
            starshipDoor.UnlockDoor();
        }
    }
}
