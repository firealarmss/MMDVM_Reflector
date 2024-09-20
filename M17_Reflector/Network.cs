using Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace M17_Reflector
{
    public class Network
    {
        private readonly UdpClient _udpClient;
        private readonly int _port;

        public Network(int port)
        {
            _port = port;
            _udpClient = new UdpClient();
        }

        public bool Initialize()
        {
            try
            {
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
                //Console.WriteLine($"Listening on port {_port}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize UDP client: {ex.Message}");
                return false;
            }
        }

        public async Task<(byte[], IPEndPoint)> ReceivePacketAsync()
        {
            try
            {
                var result = await _udpClient.ReceiveAsync();
                //Console.WriteLine(Utils.HexDump(result.Buffer));
                return (result.Buffer, result.RemoteEndPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving packet: {ex.Message}");
                return (null, null);
            }
        }

        public void SendPacket(byte[] packet, IPEndPoint ip)
        {
            try
            {
                _udpClient.Send(packet, packet.Length, ip);

                //Console.WriteLine($"Sent response to {ip}");
                //Console.WriteLine(Utils.HexDump(packet));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send response: {ex.Message}");
            }
        }

        public void Close()
        {
            _udpClient.Close();
        }
    }
}
