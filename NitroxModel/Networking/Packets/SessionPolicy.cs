﻿using System;
using NitroxModel.Core;
using NitroxModel.DataStructures;
using NitroxModel.Networking.Session;

namespace NitroxModel.Networking.Packets;

[Serializable]
public record SessionPolicy : Packet
{
    public SessionPolicy(SessionId sessionId, bool disableConsole, int maxConnections, bool requiresServerPassword)
    {
        SessionId = sessionId;
        RequiresServerPassword = requiresServerPassword;
        AuthenticationAuthority = MultiplayerSessionAuthenticationAuthority.SERVER;
        DisableConsole = disableConsole;
        MaxConnections = maxConnections;

        Version ver = NitroxEnvironment.Version;
        // only the major and minor version number is required
        NitroxVersionAllowed = new NitroxVersion(ver.Major, ver.Minor);
    }

    /// <remarks>Used for deserialization</remarks>
    public SessionPolicy(SessionId sessionId, bool requiresServerPassword, MultiplayerSessionAuthenticationAuthority authenticationAuthority,
                                    bool disableConsole, int maxConnections, NitroxVersion nitroxVersionAllowed)
    {
        SessionId = sessionId;
        RequiresServerPassword = requiresServerPassword;
        AuthenticationAuthority = authenticationAuthority;
        DisableConsole = disableConsole;
        MaxConnections = maxConnections;
        NitroxVersionAllowed = nitroxVersionAllowed;
    }

    public bool RequiresServerPassword { get; }
    public MultiplayerSessionAuthenticationAuthority AuthenticationAuthority { get; }
    public bool DisableConsole { get; }
    public int MaxConnections { get; }
    public NitroxVersion NitroxVersionAllowed { get; }
    public SessionId SessionId { get; }

    public override string ToString()
    {
        return
            $"{nameof(RequiresServerPassword)}: {RequiresServerPassword}, {nameof(DisableConsole)}: {DisableConsole}, {nameof(MaxConnections)}: {MaxConnections}, {nameof(NitroxVersionAllowed)}: {NitroxVersionAllowed}";
    }
}
