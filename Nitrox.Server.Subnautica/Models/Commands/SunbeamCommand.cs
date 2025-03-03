using System.ComponentModel;
using Nitrox.Server.Subnautica.Models.Commands.Core;
using Nitrox.Server.Subnautica.Services;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.Packets;

namespace Nitrox.Server.Subnautica.Models.Commands;

/// <summary>
///     We shouldn't let the server use this command because it needs some stuff to happen client-side like goals.
/// </summary>
[RequiresPermission(Perms.ADMIN)]
[RequiresOrigin(CommandOrigin.PLAYER)]
internal sealed class SunbeamCommand(StoryTimingService storyTimingService) : ICommandHandler<PlaySunbeamEvent.SunbeamEvent>
{
    private readonly StoryTimingService storyTimingService = storyTimingService;

    [Description("Start sunbeam events")]
    public void Execute(ICommandContext context, [Description("Which Sunbeam event to start")] PlaySunbeamEvent.SunbeamEvent sunbeamEvent) => storyTimingService.StartSunbeamEvent(sunbeamEvent.ToSubnauticaStoryKey());
}
