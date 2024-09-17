/*
* MMDVM_Reflector - NXDN_Reflector
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
* 
* Copyright (C) 2024 Caleb, KO4UYJ
* 
*/

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
