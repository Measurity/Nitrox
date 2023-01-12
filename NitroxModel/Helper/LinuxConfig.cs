using System;
using System.IO;
using NitroxModel.Serialization;

namespace NitroxModel.Helper
{
    /// <summary>
    /// Config for Linux platforms. Linux does not use the registry.
    /// </summary>
    public class LinuxConfig : NitroxConfig<LinuxConfig>
    {
        private static string ConfigPath => (Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".config"));
        public override string FileName => "nitrox.cfg";

        [PropertyDescription("Preferred game path for Subnautica")]
        public string SubnauticaGamePath { get; set; }
        public void Save() {
            this.Serialize(ConfigPath);
        }
        public static LinuxConfig Load() {
            return LinuxConfig.Load(ConfigPath);
        }
    }
}