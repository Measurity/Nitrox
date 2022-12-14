﻿using System.Collections;
using NitroxClient.GameLogic.Spawning.Metadata;
using NitroxClient.MonoBehaviours;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning
{
    /**
     * Some entities may already exist in the world but the server knows about them (an example being
     * a server spawned prefab that had a lot of children game objects backed into it).  We don't want
     * to respawn these objects; instead, just update the nitrox id and apply any entity metadata.
     */
    public class ExistingGameObjectSpawner : IEntitySpawner
    {
        public IEnumerator SpawnAsync(Entity entity, Optional<GameObject> parent, EntityCell cellRoot, TaskResult<Optional<GameObject>> result)
        {
            if (!parent.HasValue)
            {
                result.Set(Optional.Empty);
                yield break;
            }

            if (parent.Value.transform.childCount - 1 < entity.ExistingGameObjectChildIndex.Value)
            {
                Log.Error($"Parent {parent.Value} did not have a child at index {entity.ExistingGameObjectChildIndex.Value}");
                result.Set(Optional.Empty);
                yield break;
            }

            GameObject gameObject = parent.Value.transform.GetChild(entity.ExistingGameObjectChildIndex.Value).gameObject;

            NitroxEntity.SetNewId(gameObject, entity.Id);

            Optional<EntityMetadataProcessor> metadataProcessor = EntityMetadataProcessor.FromMetaData(entity.Metadata);

            if (metadataProcessor.HasValue)
            {
                metadataProcessor.Value.ProcessMetadata(gameObject, entity.Metadata);
            }

            result.Set(Optional.Of(gameObject));
            yield break;
        }

        public bool SpawnsOwnChildren()
        {
            return false;
        }
    }
}
