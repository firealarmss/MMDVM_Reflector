using System;
using System.Net;
using System.Net.Sockets;

#nullable disable

namespace NXDN_Reflector
{
    public class NetworkManager
    {
        private UdpClient _udpClient;
        private int _port;
        private bool _debug;

        public NetworkManager(int port, bool debug)
        {
            _port = port;
            _debug = debug;
        }

        public bool OpenConnection()
        {
            try
            {
                _udpClient = new UdpClient(_port);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open UDP connection: {ex.Message}");
                return false;
            }
        }

        public (byte[] data, IPEndPoint sender) ReceiveData()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = _udpClient.Receive(ref remoteEndPoint);
            if (_debug)
            {
                Console.WriteLine($"NXDN: Received data from {remoteEndPoint.Address}:{remoteEndPoint.Port}");
            }
            return (data, remoteEndPoint);
        }

        public void SendData(byte[] data, IPEndPoint destination)
        {
            _udpClient.Send(data, data.Length, destination);
            if (_debug)
            {
                Console.WriteLine($"NXDN: Sent data to {destination.Address}:{destination.Port}");
            }
        }

        public void SendPollResponse(byte[] data, IPEndPoint destination)
        {
            _udpClient.Send(data, data.Length, destination);
            if (_debug)
            {
                Console.WriteLine($"NXDN: Poll response sent to {destination.Address}:{destination.Port}");
            }
        }

        public bool CompareBuffer(byte[] buffer, string text, int length)
        {
            return buffer.Length >= length && System.Text.Encoding.ASCII.GetString(buffer, 0, length) == text;
        }
    }
}
