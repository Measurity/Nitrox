using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nitrox.Server.Subnautica.Models.Configuration;
using Nitrox.Server.Subnautica.Models.GameLogic;
using Nitrox.Server.Subnautica.Models.Resources.Parsers;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.DataStructures.GameLogic.Entities.Metadata;
using NitroxModel.DataStructures.Unity;
using NitroxModel.DataStructures.Util;

namespace Nitrox.Server.Subnautica.Services;

internal sealed class EscapePodService(EntityRegistry entityRegistry, RandomStartResource randomStart, IOptions<SubnauticaServerOptions> optionsProvider) : IHostedService
{
    private const int PLAYERS_PER_ESCAPEPOD = 50;

    private readonly EntityRegistry entityRegistry = entityRegistry;
    private readonly ThreadSafeDictionary<ushort, EscapePodWorldEntity> escapePodsByPlayerId = new();
    private readonly IOptions<SubnauticaServerOptions> optionsProvider = optionsProvider;
    private EscapePodWorldEntity podForNextPlayer;

    public NitroxId AssignPlayerToEscapePod(ushort playerId, out Optional<EscapePodWorldEntity> newlyCreatedPod)
    {
        newlyCreatedPod = Optional.Empty;

        if (escapePodsByPlayerId.TryGetValue(playerId, out EscapePodWorldEntity podEntity))
        {
            return podEntity.Id;
        }

        if (IsPodFull(podForNextPlayer))
        {
            newlyCreatedPod = Optional.Of(CreateNewEscapePod());
            podForNextPlayer = newlyCreatedPod.Value;
        }

        podForNextPlayer.Players.Add(playerId);
        escapePodsByPlayerId[playerId] = podForNextPlayer;

        return podForNextPlayer.Id;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        List<EscapePodWorldEntity> escapePods = entityRegistry.GetEntities<EscapePodWorldEntity>();

        InitializePodForNextPlayer(escapePods);
        InitializeEscapePodsByPlayerId(escapePods);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool IsPodFull(EscapePodWorldEntity pod) => pod.Players.Count >= PLAYERS_PER_ESCAPEPOD;

    private EscapePodWorldEntity CreateNewEscapePod()
    {
        EscapePodWorldEntity escapePod = new(GetStartPosition(), new NitroxId(), new EscapePodMetadata(false, false));

        escapePod.ChildEntities.Add(new PrefabChildEntity(new NitroxId(), "5c06baec-0539-4f26-817d-78443548cc52", new NitroxTechType("Radio"), 0, null, escapePod.Id));
        escapePod.ChildEntities.Add(new PrefabChildEntity(new NitroxId(), "c0175cf7-0b6a-4a1d-938f-dad0dbb6fa06", new NitroxTechType("MedicalCabinet"), 0, null, escapePod.Id));
        escapePod.ChildEntities.Add(new PrefabChildEntity(new NitroxId(), "9f16d82b-11f4-4eeb-aedf-f2fa2bfca8e3", new NitroxTechType("Fabricator"), 0, null, escapePod.Id));
        escapePod.ChildEntities.Add(new InventoryEntity(0, new NitroxId(), new NitroxTechType("SmallStorage"), null, escapePod.Id, []));

        entityRegistry.AddOrUpdate(escapePod);

        return escapePod;
    }

    private NitroxVector3 GetStartPosition()
    {
        List<EscapePodWorldEntity> escapePods = entityRegistry.GetEntities<EscapePodWorldEntity>();

        Random rnd = new(optionsProvider.Value.Seed.GetHashCode());
        NitroxVector3 position = randomStart.RandomStartGenerator.GenerateRandomStartPosition(rnd);

        if (escapePods.Count == 0)
        {
            return position;
        }

        foreach (EscapePodWorldEntity escapePodModel in escapePods)
        {
            if (position == NitroxVector3.Zero)
            {
                break;
            }

            if (escapePodModel.Transform.Position != position)
            {
                return position;
            }
        }

        float xNormed = (float)rnd.NextDouble();
        float zNormed = (float)rnd.NextDouble();

        if (xNormed < 0.3f)
        {
            xNormed = 0.3f;
        }
        else if (xNormed > 0.7f)
        {
            xNormed = 0.7f;
        }

        if (zNormed < 0.3f)
        {
            zNormed = 0.3f;
        }
        else if (zNormed > 0.7f)
        {
            zNormed = 0.7f;
        }

        NitroxVector3 lastEscapePodPosition = escapePods[escapePods.Count - 1].Transform.Position;

        float x = xNormed * 100 - 50;
        float z = zNormed * 100 - 50;

        return new NitroxVector3(lastEscapePodPosition.X + x, 0, lastEscapePodPosition.Z + z);
    }

    private void InitializePodForNextPlayer(List<EscapePodWorldEntity> escapePods)
    {
        foreach (EscapePodWorldEntity pod in escapePods)
        {
            if (!IsPodFull(pod))
            {
                podForNextPlayer = pod;
                return;
            }
        }

        podForNextPlayer = CreateNewEscapePod();
    }

    private void InitializeEscapePodsByPlayerId(List<EscapePodWorldEntity> escapePods)
    {
        escapePodsByPlayerId.Clear();
        foreach (EscapePodWorldEntity pod in escapePods)
        {
            foreach (ushort playerId in pod.Players)
            {
                escapePodsByPlayerId[playerId] = pod;
            }
        }
    }
}
