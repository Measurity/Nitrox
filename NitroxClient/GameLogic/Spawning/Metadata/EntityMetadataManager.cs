using System;
using System.Reflection;
using JetBrains.Annotations;
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
    private readonly TypeLookup<IEntityMetadataExtractor<object, EntityMetadata>> extractors;
    private readonly TypeLookup<IEntityMetadataProcessor<EntityMetadata>> processors;

    public EntityMetadataManager()
    {
        Type[] allAssemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
        extractors = TypeLookup<IEntityMetadataExtractor<object, EntityMetadata>>.Create<MetadataExtractorWrapper<object, EntityMetadata>>(allAssemblyTypes, NitroxServiceLocator.LocateService);
        processors = TypeLookup<IEntityMetadataProcessor<EntityMetadata>>.Create<MetadataProcessorWrapper<EntityMetadata>>(allAssemblyTypes, NitroxServiceLocator.LocateService);
    }

    public Optional<EntityMetadata> Extract(object o)
    {
        if (extractors.TryGetValue(o.GetType(), out IEntityMetadataExtractor<object, EntityMetadata> extractor))
        {
            return extractor.Extract(o);
        }

        return Optional.Empty;
    }

    public Optional<EntityMetadata> Extract(GameObject o)
    {
        foreach (Component component in o.GetComponents<Component>())
        {
            if (extractors.TryGetValue(component.GetType(), out IEntityMetadataExtractor<object, EntityMetadata> extractor))
            {
                return extractor.Extract(component);
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

    [UsedImplicitly]
    private record MetadataProcessorWrapper<T>(IEntityMetadataProcessor<T> Inner) : IEntityMetadataProcessor<EntityMetadata> where T : EntityMetadata
    {
        public void ProcessMetadata(GameObject gameObject, EntityMetadata metadata)
        {
            Inner.ProcessMetadata(gameObject, (T)metadata);
        }
    }

    [UsedImplicitly]
    private record MetadataExtractorWrapper<TIn, TOut>(IEntityMetadataExtractor<object, TOut> Inner) : IEntityMetadataExtractor<object, EntityMetadata> where TOut : EntityMetadata
    {
        public EntityMetadata Extract(object o)
        {
            return Inner.Extract((TIn)o);
        }
    }
}
