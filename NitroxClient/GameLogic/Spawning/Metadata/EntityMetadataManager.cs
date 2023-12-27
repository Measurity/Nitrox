using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NitroxClient.GameLogic.Spawning.Metadata.Extractor.Abstract;
using NitroxClient.GameLogic.Spawning.Metadata.Processor.Abstract;
using NitroxModel.Core;
using NitroxModel.DataStructures.GameLogic.Entities.Metadata;
using NitroxModel.DataStructures.Util;
using NitroxModel.Helper;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning.Metadata;

public class EntityMetadataManager
{
    private readonly Dictionary<Type, IEntityMetadataExtractor> extractors;
    private readonly TypeLookup<IEntityMetadataProcessor<EntityMetadata>> processors;

    public EntityMetadataManager(IEnumerable<IEntityMetadataExtractor> extractors)
    {
        this.extractors = extractors.ToDictionary(p => p.GetType().BaseType.GetGenericArguments()[0]);
        processors = TypeLookup<IEntityMetadataProcessor<EntityMetadata>>.Create<MetadataProcessorWrapper<EntityMetadata>>(Assembly.GetExecutingAssembly().GetTypes(), NitroxServiceLocator.LocateService);
    }

    public Optional<EntityMetadata> Extract(object o)
    {
        if (extractors.TryGetValue(o.GetType(), out IEntityMetadataExtractor extractor))
        {
            return extractor.From(o);
        }

        return Optional.Empty;
    }

    public Optional<EntityMetadata> Extract(GameObject o)
    {
        foreach (Component component in o.GetComponents<Component>())
        {
            if (extractors.TryGetValue(component.GetType(), out IEntityMetadataExtractor extractor))
            {
                return extractor.From(component);
            }
        }

        return Optional.Empty;
    }

    public Optional<IEntityMetadataProcessor<EntityMetadata>> FromMetaData(EntityMetadata metadata)
    {
        if (metadata != null && processors.TryGetValue(metadata.GetType(), out IEntityMetadataProcessor<EntityMetadata> processor))
        {
            return Optional.Of(processor);
        }

        return Optional.Empty;
    }

    public void ApplyMetadata(GameObject gameObject, EntityMetadata metadata)
    {
        Optional<IEntityMetadataProcessor<EntityMetadata>> metadataProcessor = FromMetaData(metadata);

        if (metadataProcessor.HasValue)
        {
            metadataProcessor.Value.ProcessMetadata(gameObject, metadata);
        }
    }

    private record MetadataProcessorWrapper<T>(IEntityMetadataProcessor<T> Inner) : IEntityMetadataProcessor<EntityMetadata> where T : EntityMetadata
    {
        public void ProcessMetadata(GameObject gameObject, EntityMetadata metadata)
        {
            Inner.ProcessMetadata(gameObject, (T)metadata);
        }
    }
}
