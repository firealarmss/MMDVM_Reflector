using System.Net;

namespace P25_Reflector
{
    /// <summary>
    /// P25 Peer class
    /// </summary>
    public class P25Peer
    {
        public IPEndPoint Address { get; private set; }
        public string CallSign { get; private set; }
        private DateTime _lastActive;

        public TransmissionState State { get; private set; }

        /// <summary>
        /// Creates an instance of <see cref="P25Peer"/>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public P25Peer(IPEndPoint address, byte[] data)
        {
            Address = address;
            CallSign = System.Text.Encoding.ASCII.GetString(data, 1, 10);
            _lastActive = DateTime.Now;
            State = new TransmissionState();
        }

        /// <summary>
        /// Helper to reset inactivity monitor
        /// </summary>
        public void Refresh()
        {
            _lastActive = DateTime.Now;
        }

        /// <summary>
        /// Checks if two <see cref="IPEndPoint"/> are the same
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool IsSameAddress(IPEndPoint address)
        {
            return Address.Equals(address);
        }

        /// <summary>
        /// Helper to check if the peer is inactive
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool IsExpired(int timeout)
        {
            return (DateTime.Now - _lastActive).TotalSeconds > timeout;
        }
    }
}
