using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace P25_Reflector
{
    public class NetworkManager
    {
        private UdpClient _udpClient;
        private bool _debug;

        public NetworkManager(int port, bool debug)
        {
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            _debug = debug;
        }

        public bool OpenConnection()
        {
            try
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public (byte[] data, IPEndPoint sender) ReceiveData()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = _udpClient.Receive(ref remoteEndPoint);
            if (_debug)
            {
                Console.WriteLine("P25: Received data: " + BitConverter.ToString(data));
            }
            return (data, remoteEndPoint);
        }

        public void SendData(byte[] data, IPEndPoint destination)
        {
            _udpClient.Send(data, data.Length, destination);
            if (_debug)
            {
                Console.WriteLine("P25: Sent data: " + BitConverter.ToString(data));
            }
        }
    }
}
