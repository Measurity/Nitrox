using NitroxModel.Dto;

namespace Nitrox.Server.Subnautica.Extensions.Dto;

public static class ConnectedPlayerDtoExtensions
{
    public static ConnectedPlayerDto ToConnectedPlayerDto(this Database.Models.Session session)
    {
        if (session.Player is null)
        {
            return null;
        }
        return new ConnectedPlayerDto
        {
            Id = session.Player.Id,
            SessionId = session.Id,
            Name = session.Player.Name,
            Permissions = session.Player.Permissions,
        };
    }
}
