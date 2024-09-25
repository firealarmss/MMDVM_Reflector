using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace Common
{
    /// <summary>
    /// Reflectors current stats
    /// </summary>
    public class ReflectorStatus
    {
        /// <summary>
        /// Reflector mode in a string
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Reflector status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Total count of connected peers
        /// </summary>
        public int ConnectedPeers { get; set; }

        /// <summary>
        /// Reflectors bind port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Reflectors ACL
        /// </summary>
        public List<CallsignEntry> Acl { get; set; }
    }
}
