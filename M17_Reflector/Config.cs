using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M17_Reflector
{
    public class Config
    {
        public Config() { /* stub */ }

        public bool Enabled { get; set; }
        public int NetworkPort { get; set; }
        public string Reflector { get; set; }
        public bool NetworkDebug { get; set; }
    }
}
