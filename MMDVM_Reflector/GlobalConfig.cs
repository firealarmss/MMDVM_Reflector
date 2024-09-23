/*
* MMDVM_Reflector
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
* 
* Copyright (C) 2024 Caleb, KO4UYJ
* 
*/

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#nullable disable

namespace MMDVM_Reflector
{
    /// <summary>
    /// Global configuration object for YAML
    /// </summary>
    public class GlobalConfig
    {
        public ReporterConfig Reporter { get; set; }
        public RestApiConfig Rest { get; set; }
        public ReflectorConfig Reflectors { get; set; }
        public LoggerConfig Logger { get; set; }
        public string AclPath { get; set; }
        
        /// <summary>
        /// Load YAML config file
        /// </summary>
        /// <param name="configPath"></param>
        /// <returns></returns>
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

    /// <summary>
    /// Logger configuration
    /// </summary>
    public class LoggerConfig
    {
        public string Path { get; set; }
    }

    /// <summary>
    /// Reflectors configuration
    /// </summary>
    public class ReflectorConfig
    {
        public P25_Reflector.Config P25 { get; set; }
        public NXDN_Reflector.Config Nxdn { get; set; }
        public YSF_Reflector.Config Ysf { get; set; }
        public M17_Reflector.Config M17 { get; set; }
    }

    /// <summary>
    /// RESTful API configuration
    /// </summary>
    public class RestApiConfig
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Reporter configuration
    /// </summary>
    public class ReporterConfig
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public bool Enabled { get; set; }
    }
}
