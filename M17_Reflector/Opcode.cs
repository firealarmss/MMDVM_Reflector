using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M17_Reflector
{
    public enum Opcode
    {
        NET_CONN = 0x434F4E4E,
        NET_DISC = 0x44495343,
        NET_VOICE = 0x4D313720,
        NET_KA = 0x504F4E47
    }
}
