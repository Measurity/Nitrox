using System;
using Microsoft.Extensions.Logging;
using Nitrox.Server.Subnautica.Services;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.Packets;

namespace Nitrox.Server.Subnautica.Models.Commands.Core;

internal sealed record PlayerToServerCommandContext : ICommandContext
{
    private readonly PlayerService playerService;
    public ILogger Logger { get; set; }
    public CommandOrigin Origin { get; init; } = CommandOrigin.PLAYER;
    public string OriginName => Player.Name;
    public ushort OriginId { get; init; }
    public Perms Permissions { get; init; }

    /// <summary>
    ///     Gets the player which issued the command.
    /// </summary>
    public NitroxServer.Player Player { get; init; }

    public PlayerToServerCommandContext(PlayerService playerService, NitroxServer.Player player)
    {
        ArgumentNullException.ThrowIfNull(playerService);
        ArgumentNullException.ThrowIfNull(player);
        this.playerService = playerService;
        Player = player;
        OriginId = player.Id;
        Permissions = player.Permissions;
    }

    public void Message(ushort id, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }
        if (OriginId == id)
        {
            Reply(message);
            return;
        }
        if (!playerService.TryGetPlayerById(id, out NitroxServer.Player player))
        {
            Logger.LogWarning("No player found with id {PlayerId}", id);
            return;
        }
        player.SendPacket(new ChatMessage(OriginId, message));
    }

    public void Reply(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }
        Player.SendPacket(new ChatMessage(ChatMessage.SERVER_ID, message));
    }

    public void MessageAll(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }
        playerService.SendPacketToOtherPlayers(new ChatMessage(ChatMessage.SERVER_ID, message), Player);
        Logger.LogInformation("Player {PlayerName} #{PlayerId} sent a message to everyone:{Message}", Player.Name, Player.Id, message);
    }
}
