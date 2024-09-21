using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Api
{
    public interface IReflectorContext
    {
        bool DisconnectCallsign(string reflectorType, string callsign);

        bool BlockCallsign(string reflectorType, string callsign);
    }
}