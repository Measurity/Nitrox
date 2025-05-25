using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Options;
using Nitrox.Server.Subnautica.Models.Commands.Core;
using Nitrox.Server.Subnautica.Models.Configuration;
using Nitrox.Server.Subnautica.Models.Respositories;
using NitroxModel.Dto;

namespace Nitrox.Server.Subnautica.Models.Commands;

[Alias("list", "p")]
internal class PlayersCommand(IOptions<SubnauticaServerOptions> serverOptionsProvider, PlayerRepository playerRepository) : ICommandHandler
{
    private readonly PlayerRepository playerRepository = playerRepository;
    private readonly IOptions<SubnauticaServerOptions> serverOptionsProvider = serverOptionsProvider;

    [Description("Shows who's online")]
    public async Task Execute(ICommandContext context)
    {
        SubnauticaServerOptions options = serverOptionsProvider.Value;

        ConnectedPlayerDto[] players = await playerRepository.GetConnectedPlayersAsync();
        if (players.Length < 1)
        {
            await context.ReplyAsync("There are no players");
            return;
        }
        string playerNamesWithIds = string.Join(", ", players.OrderBy(p => p.Name).Select(p => $"ID {p.Id}, Session: #{p.SessionId}, Name: '{p.Name}'"));
        await context.ReplyAsync($"List of players ({players.Length}/{options.MaxConnections}):{Environment.NewLine}{playerNamesWithIds}");
    }
}
