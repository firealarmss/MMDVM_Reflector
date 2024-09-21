using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Common
{
    public class CallsignAcl
    {
        public List<CallsignEntry> Entries { get; set; } = new List<CallsignEntry>();

        public string AclPath { get; set; }

        public CallsignAcl(string configPath)
        {
            AclPath = configPath;
        }

        public void Load()
        {
            Console.WriteLine($"Loading ACL from: {AclPath}");
            try
            {
                if (!File.Exists(AclPath))
                {
                    Console.WriteLine($"File not found: {AclPath}");
                    return;
                }

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                using (var reader = new StreamReader(AclPath))
                {
                    var loadedEntries = deserializer.Deserialize<List<CallsignEntry>>(reader);
                    if (loadedEntries != null)
                    {
                        Entries = loadedEntries;
                        //Console.WriteLine("ACL loaded successfully.");
                    }
                    else
                    {
                        Console.WriteLine("No entries found in the ACL file.");
                        Entries = new List<CallsignEntry>();
                    }
                }
            }
            catch (YamlDotNet.Core.YamlException yamlEx)
            {
                Console.WriteLine($"YAML format error: {yamlEx.Message}");
            }
            catch (FileNotFoundException fnfEx)
            {
                Console.WriteLine($"File not found: {fnfEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during deserialization: {ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
        }


        public bool CheckCallsignAcl(string callsign)
        {
            try
            {
                CallsignEntry entry = Entries.Find(e => e.Callsign == callsign);
                return entry != null && entry.Allowed;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public bool CheckCallsignAcl(uint rid)
        {
            try
            {
                CallsignEntry entry = Entries.Find(e => e.Rid == rid);
                return entry != null && entry.Allowed;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public void Save()
        {
            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                using (var writer = new StreamWriter(AclPath))
                {
                    serializer.Serialize(writer, Entries);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during serialization: {ex.Message}");
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
