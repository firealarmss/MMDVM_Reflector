using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Common
{
    public class CallsignAcl
    {
        public List<CallsignEntry> entries;

        public CallsignAcl() { /* stub */ }

        public static CallsignAcl Load(string configPath)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            using (var reader = new StreamReader(configPath))
            {
                return deserializer.Deserialize<CallsignAcl>(reader);
            }
        }

        public bool CheckCallsignAcl(string callsign)
        {
            try
            {
                CallsignEntry entry = entries.Find(e => e.Callsign == callsign);

                return entry != null && entry.Allowed;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool CheckCallsignAcl(uint rid)
        {
            try
            {
                CallsignEntry entry = entries.Find(e => e.Rid == rid);

                return entry != null && entry.Allowed;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

    public class CallsignEntry
    {
        public bool Allowed { get; set; }
        public string Callsign { get; set; }
        public uint Rid { get; set; }
    }
}
