using System;
using System.Net;

namespace M17_Reflector
{
    public class Peer
    {
        public IPEndPoint Address { get; }
        public string Module { get; set; }

        private DateTime _lastActive;

        public Peer(IPEndPoint address)
        {
            Address = address;
            _lastActive = DateTime.Now;
        }

        public void Refresh()
        {
            _lastActive = DateTime.Now;
        }

        public bool IsExpired() => (DateTime.Now - _lastActive).TotalSeconds > 30;
    }
}
