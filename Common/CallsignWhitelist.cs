using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class CallsignWhitelist
    {
        public CallsignWhitelist() { /* stub */ }

        public List<CallsignEntry> entries;
    }

    public class CallsignEntry
    {
        public bool Allowed { get; set; }
        public string Callsign { get; set; }
        public uint Rid { get; set; }
    }
}
