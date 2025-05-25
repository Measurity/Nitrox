using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Unity;
using NitroxModel.Networking;
using NitroxModel.Server;

namespace Nitrox.Server.Subnautica.Database.Models;

/// <summary>
///     The player model that clients can assume on join. This data is kept even after server shuts down.
/// </summary>
/// <remarks>
///     Use <see cref="Session" /> and dependant tables if data should be discarded when player disconnects (or when
///     server stops).
/// </remarks>
[Table("Players")]
public record Player
{
    /// <summary>
    ///     Primary key in the database.
    /// </summary>
    public PeerId Id { get; set; }

    /// <summary>
    ///     Name of the player as it was provided by the player on join.
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    ///     If true, this player data should not be reused.
    /// </summary>
    /// <remarks>
    ///     This is set when the player is playing in hardcore mode and dies.
    /// </remarks>
    public bool Deleted { get; set; }

    /// <summary>
    ///     If true, player cannot chat with other players.
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    ///     Saved position to get back to. Used as spawn position or as fallback position when player dies.
    /// </summary>
    public NitroxVector3? SavedPosition { get; set; }

    public NitroxQuaternion SpawnRotation { get; set; }

    /// <summary>
    ///     Saved SubRootId to go back to when stuck or when spawning.
    /// </summary>
    public NitroxId SavedSubRootID { get; set; }

    /// <summary>
    ///     Permissions as granted by the server. Defaults to <see cref="Perms.DEFAULT" />.
    /// </summary>
    public Perms Permissions { get; set; }

    /// <summary>
    ///     The game mode this player is playing in. Can be different for other players in the same world.
    /// </summary>
    public SubnauticaGameMode GameMode { get; set; }

    /// <summary>
    ///     The real-world time when the player first started playing on the server.
    /// </summary>
    [Required]
    public DateTimeOffset Created { get; set; } = DateTimeOffset.Now;

    /// <summary>
    ///     Gets or sets the login key for this player data.
    /// </summary>
    /// <remarks>
    ///     This is given by the server to the client on new join.
    ///     The client should remember the key, so they can rejoin with the same player data.
    /// </remarks>
    [Required]
    public byte[] LoginKey { get; set; }

    // TODO: Store this
    // public List<NitroxTechType> UsedItems { get; set; } = [];
    // public Optional<NitroxId>[] QuickSlotsBindingIds { get; set; } = [];
    // public Dictionary<string, NitroxId> EquippedItems { get; set; } = [];
}
