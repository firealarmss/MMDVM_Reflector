using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Api
{
    public class Report
    {
        public Report() { /* stub */ }

        public int SrcId { get; set; }
        public int DstId { get; set; }
        public string Peer { get; set; }
        public string Extra {  get; set; }
        public DigitalMode Mode { get; set; }
        public Type Type { get; set; }
        public DateTime DateTime { get; set; }
    }
}
