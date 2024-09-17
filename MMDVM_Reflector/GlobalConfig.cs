using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#nullable disable

namespace MMDVM_Reflector
{
    public class GlobalConfig
    {
        public ReflectorConfig Reflectors { get; set; }

        public static GlobalConfig Load(string configPath)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            using (var reader = new StreamReader(configPath))
            {
                return deserializer.Deserialize<GlobalConfig>(reader);
            }
        }
    }

    public class ReflectorConfig
    {
        public P25_Reflector.Config P25 { get; set; }
        public NXDN_Reflector.Config Nxdn { get; set; }
        public YSF_Reflector.Config Ysf { get; set; }
    }
}
