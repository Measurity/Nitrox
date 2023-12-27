using NitroxClient.Communication;
using NitroxClient.GameLogic.FMOD;
using NitroxClient.Unity.Helper;
using NitroxModel.DataStructures.GameLogic.Entities.Metadata;
using NitroxModel.DataStructures.Unity;
using NitroxModel.Packets;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning.Metadata.Processor.Abstract;

public class VehicleMetadataProcessor : IEntityMetadataProcessor<ExosuitMetadata>, IEntityMetadataProcessor<SeamothMetadata>
{
    private readonly LiveMixinManager liveMixinManager;

    public VehicleMetadataProcessor(LiveMixinManager liveMixinManager)
    {
        this.liveMixinManager = liveMixinManager;
    }

    public void ProcessMetadata(GameObject gameObject, ExosuitMetadata metadata)
    {
        if (!gameObject.TryGetComponent(out Exosuit exosuit))
        {
            Log.ErrorOnce($"[{nameof(VehicleMetadataProcessor)}] Could not find {nameof(Exosuit)} on {gameObject}");
            return;
        }
        if (!gameObject.TryGetComponent(out SubName subName))
        {
            Log.ErrorOnce($"[{nameof(VehicleMetadataProcessor)}] Could not find {nameof(SubName)} on {gameObject}");
            return;
        }

        using (PacketSuppressor<EntityMetadataUpdate>.Suppress())
        {
            SetHealth(gameObject, metadata.Health);
            SetNameAndColors(subName, metadata.Name, metadata.Colors);
        }
    }

    public void ProcessMetadata(GameObject gameObject, SeamothMetadata metadata)
    {
        if (!gameObject.TryGetComponent(out SeaMoth seamoth))
        {
            Log.ErrorOnce($"[{nameof(VehicleMetadataProcessor)}] Could not find {nameof(SeaMoth)} on {gameObject}");
            return;
        }

        if (!gameObject.TryGetComponent(out SubName subName))
        {
            Log.ErrorOnce($"[{nameof(VehicleMetadataProcessor)}] Could not find {nameof(SubName)} on {gameObject}");
            return;
        }

        using (PacketSuppressor<EntityMetadataUpdate>.Suppress())
        {
            SetLights(seamoth, metadata.LightsOn);
            SetHealth(seamoth.gameObject, metadata.Health);
            SetNameAndColors(subName, metadata.Name, metadata.Colors);
        }
    }

    private void SetHealth(GameObject gameObject, float health)
    {
        LiveMixin liveMixin = gameObject.RequireComponentInChildren<LiveMixin>(true);
        liveMixinManager.SyncRemoteHealth(liveMixin, health);
    }

    private void SetNameAndColors(SubName subName, string text, NitroxVector3[] nitroxColor)
    {
        SubNameInputMetadataProcessor.SetNameAndColors(subName, text, nitroxColor);
    }

    private void SetLights(SeaMoth seamoth, bool lightsOn)
    {
        using (FMODSystem.SuppressSounds())
        {
            seamoth.toggleLights.SetLightsActive(lightsOn);
        }
    }
}
