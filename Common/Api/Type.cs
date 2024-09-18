using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Api
{
    public enum Type
    {
        CALL_START = 0x00,
        CALL_END = 0x01,
        CALL_ALERT = 0x02,
        ACK_RSP = 0x03,
        NEW_CONNECTION = 0x04,
        UNLINK = 0x05,
        CONNECTION = 0x06
    }
}
