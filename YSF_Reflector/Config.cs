using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YSF_Reflector
{
    public class Config
    {
        public Config() { /* stub */ }

        public bool Enabled { get; set; }
        public int NetworkPort { get; set; }
        public bool NetworkDebug { get; set; }
    }
}
