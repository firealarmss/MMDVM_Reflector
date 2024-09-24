/*
* MMDVM_Reflector - YSF_Reflector
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

using Common;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

#nullable disable

namespace YSF_Reflector
{
    /// <summary>
    /// YSF Networking class
    /// </summary>
    public class NetworkManager
    {
        private UdpClient _udpClient;

        private int _port;
        private int _id;
        private string _name;
        private string _description;
        private bool _debug;

        /// <summary>
        /// Creates an instance of <see cref="NetworkManager"/>
        /// </summary>
        /// <param name="port"></param>
        /// <param name="debug"></param>
        public NetworkManager(int port, int id, string name, string description, bool debug)
        {
            _port = port;
            _id = id;
            _name = name;
            _description = description;
            _debug = debug;
        }

        /// <summary>
        /// Opens a UDP connection on the specified port
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Callback for UDP data received
        /// </summary>
        /// <returns></returns>
        public (byte[] data, IPEndPoint sender) ReceiveData()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = _udpClient.Receive(ref remoteEndPoint);
            if (_debug)
            {
                Console.WriteLine(Utils.HexDump(data));
                Console.WriteLine($"YSF: Received data from {remoteEndPoint.Address}:{remoteEndPoint.Port}");
            }
            return (data, remoteEndPoint);
        }

        /// <summary>
        /// Helper to send UDP data to a specified <see cref="IPEndPoint"/>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="destination"></param>
        public void SendData(byte[] data, IPEndPoint destination)
        {
            _udpClient.Send(data, data.Length, destination);
            if (_debug)
            {
                Console.WriteLine(Utils.HexDump(data));
                Console.WriteLine($"YSF: Sent data to {destination.Address}:{destination.Port}");
            }
        }

        /// <summary>
        /// Helper to send a YSF poll response
        /// </summary>
        /// <param name="destination"></param>
        public void SendPollResponse(IPEndPoint destination)
        {
            byte[] buffer = new byte[14];

            buffer[0] = (byte)'Y';
            buffer[1] = (byte)'S';
            buffer[2] = (byte)'F';
            buffer[3] = (byte)'P';

            buffer[4] = (byte)'R';
            buffer[5] = (byte)'E';
            buffer[6] = (byte)'F';
            buffer[7] = (byte)'L';
            buffer[8] = (byte)'E';
            buffer[9] = (byte)'C';
            buffer[10] = (byte)'T';
            buffer[11] = (byte)'O';
            buffer[12] = (byte)'R';
            buffer[13] = (byte)' ';

            if (_debug)
            {
                Console.WriteLine($"Sending YSF Network Poll: {BitConverter.ToString(buffer)}");
            }

            try
            {
                _udpClient.Send(buffer, buffer.Length, destination);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending YSF poll: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper to send YSFS response
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="count"></param>
        public void SendStatus(IPEndPoint destination, int count)
        {
            try
            {
                SendData(Encoding.ASCII.GetBytes(ComputeStatus(count)), destination);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending YSF status: {ex.Message}");
            }
        }

        /// <summary>
        /// Compute and format YSFS response
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public string ComputeStatus(int count)
        {
            uint hash = (uint)_id;

            if (hash == 0U)
            {
                for (int i = 0; i < _name.Length; i++)
                {
                    uint charValue = (uint)_name[i];
                    hash = unchecked(hash + charValue);
                    hash = unchecked(hash + (hash << 10));
                    hash = unchecked(hash ^ (hash >> 6));
                }

                hash = unchecked(hash + (hash << 3));
                hash = unchecked(hash ^ (hash >> 11));
                hash = unchecked(hash + (hash << 15));
            }

            string status = $"YSFS{hash % 100000U:D5}{PadOrTruncate(_name, 16)}{PadOrTruncate(_description, 14)}{count:D3}";
            return status;
        }

        /// <summary>
        /// Helper to pad or truncate a string
        /// </summary>
        /// <param name="input"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private string PadOrTruncate(string input, int length)
        {
            if (input.Length > length)
            {
                return input.Substring(0, length);
            }
            else
            {
                return input.PadRight(length, ' ');
            }
        }
    }
}
