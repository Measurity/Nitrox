using Microsoft.Extensions.Logging;
using NitroxModel.DataStructures.GameLogic;

namespace Nitrox.Server.Subnautica.Models.Commands.Core;

/// <summary>
///     A context that can be provided to a command as it is called.
/// </summary>
public interface ICommandContext
{
    public ILogger Logger { get; set; }

    CommandOrigin Origin { get; init; }

    public string OriginName { get; }

    /// <summary>
    ///     The id of the user that issued this command.
    /// </summary>
    ushort OriginId { get; init; }

    /// <summary>
    ///     The permissions of the issuer as they were when the command was issued.
    /// </summary>
    Perms Permissions { get; init; }

    /// <summary>
    ///     Sends a message back to the command issuer.
    /// </summary>
    /// <param name="message">The message to send.</param>
    void Reply(string message);

    /// <summary>
    ///     Sends a message to the user id. Does nothing if user id is not found.
    /// </summary>
    /// <param name="id">The id of the receiving user.</param>
    /// <param name="message">The message to send.</param>
    void Message(ushort id, string message);

    /// <summary>
    ///     Sends a message to all other users.
    /// </summary>
    /// <param name="message">The message to send to all users.</param>
    void MessageAll(string message);
}
