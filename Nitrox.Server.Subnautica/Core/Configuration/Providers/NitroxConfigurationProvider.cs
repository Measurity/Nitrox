using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using NitroxModel.Serialization;

namespace Nitrox.Server.Subnautica.Core.Configuration.Providers;

internal sealed class NitroxConfigurationProvider(NitroxConfigurationSource source) : FileConfigurationProvider(source)
{
    private readonly NitroxConfigurationSource source = source;

    public override void Load(Stream stream)
    {
        if (!string.IsNullOrWhiteSpace(source.Section))
        {
            foreach (KeyValuePair<string, string> pair in NitroxConfig.Parse(stream))
            {
                Data.Add($"{source.Section}:{pair.Key}", pair.Value);
            }
        }
        else
        {
            Data = NitroxConfig.Parse(stream);
        }
    }
}
