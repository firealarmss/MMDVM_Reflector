using System;
using System.Net;

namespace YSF_Reflector
{
    public class YSFRepeater
    {
        public IPEndPoint Address { get; private set; }
        public string CallSign { get; private set; }
        private DateTime _lastActive;
        public bool IsTransmitting { get; private set; }

        public YSFRepeater(IPEndPoint address, byte[] data)
        {
            Address = address;
            CallSign = System.Text.Encoding.ASCII.GetString(data, 4, YSFReflector.YSF_CALLSIGN_LENGTH).Trim();
            _lastActive = DateTime.Now;
            IsTransmitting = false;
        }

        public void Refresh()
        {
            _lastActive = DateTime.Now;
        }

        public bool IsSameAddress(IPEndPoint address)
        {
            return Address.Equals(address);
        }

        public bool IsExpired()
        {
            return (DateTime.Now - _lastActive).TotalSeconds > 5;
        }

        public void StartTransmission()
        {
            IsTransmitting = true;
        }

        public void EndTransmission()
        {
            IsTransmitting = false;
        }
    }
}
