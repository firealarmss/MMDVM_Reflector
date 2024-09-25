using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace Common
{
    public class ReflectorStatus
    {
        public string Mode { get; set; }
        public string Status { get; set; }
        public int ConnectedPeers { get; set; }
        public int Port { get; set; }
        public List<CallsignEntry> Acl { get; set; }
    }
}
