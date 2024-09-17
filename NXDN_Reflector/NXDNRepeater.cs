using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NXDN_Reflector
{
    public class NXDNRepeater
    {
        public IPEndPoint Address { get; }
        public string CallSign { get; }
        public bool IsTransmitting { get; private set; }

        public NXDNRepeater(IPEndPoint address, byte[] buffer)
        {
            Address = address;
            CallSign = System.Text.Encoding.ASCII.GetString(buffer, 5, 10).Trim();
            IsTransmitting = false;
        }

        public void StartTransmission()
        {
            IsTransmitting = true;
        }

        public void EndTransmission()
        {
            IsTransmitting = false;
        }

        public bool IsSameAddress(IPEndPoint address)
        {
            return Address.Equals(address);
        }
    }

}
