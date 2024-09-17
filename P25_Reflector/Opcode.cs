using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P25_Reflector
{
    public static class Opcode
    {
        public const byte NET_TAG_HEADER = 0x00;
        public const byte NET_TAG_DATA = 0x01;
        public const byte NET_TAG_LOST = 0x02;
        public const byte NET_TAG_EOT = 0x03;
        public const byte NET_TERM = 0x80;
        public const byte NET_POLL = 0xF0;
        public const byte NET_UNLINK = 0xF1;
    }
}
