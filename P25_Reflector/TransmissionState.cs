using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P25_Reflector
{
    public class TransmissionState
    {
        public bool Seen64 { get; set; }
        public bool Seen65 { get; set; }
        public bool Displayed { get; set; }
        public byte Lcf { get; set; }
        public uint SrcId { get; set; }
        public uint DstId { get; set; }

        public TransmissionState()
        {
            Reset();
        }

        public void Reset()
        {
            Seen64 = false;
            Seen65 = false;
            Displayed = false;
            Lcf = 0;
            SrcId = 0;
            DstId = 0;
        }
    }
}
