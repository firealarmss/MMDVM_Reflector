/*
* MMDVM_Reflector - Common
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
using Serilog;

namespace Common
{
    /// <summary>
    /// Base networking for Reflectors
    /// </summary>
    public class BaseNetwork
    {
        protected UdpClient _udpClient;
        protected bool _debug;
        protected DigitalMode _mode;
        protected ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNetwork"/> class.
        /// </summary>
        /// <param name="port">The port to bind the UDP client to.</param>
        /// <param name="mode">The digital mode (e.g., P25, M17) for logging.</param>
        /// <param name="logger">The logger instance for logging events.</param>
        /// <param name="debug">Indicates whether to enable debug output.</param>
        public BaseNetwork(int port, DigitalMode mode, ILogger logger, bool debug = false)
        {
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            _debug = debug;
            _mode = mode;
            _logger = logger;
        }

        /// <summary>
        /// Opens a UDP connection on the specified port.
        /// </summary>
        /// <returns>True if the connection is successfully opened; otherwise, false.</returns>
        public virtual bool OpenConnection()
        {
            try
            {
                _logger.Information($"{_mode}: Connection opened on port {_udpClient.Client.LocalEndPoint}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"{_mode}: Failed to open connection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Receives data from the UDP client.
        /// </summary>
        /// <returns>A tuple containing the received data and the sender's endpoint.</returns>
        public virtual (byte[] data, IPEndPoint sender) ReceiveData()
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _udpClient.Receive(ref remoteEndPoint);

                if (_debug)
                {
                    _logger.Debug($"{_mode}: Received data from {remoteEndPoint}:\n{Utils.HexDump(data)}");
                }

                return (data, remoteEndPoint);
            }
            catch (Exception ex)
            {
                _logger.Error($"{_mode}: Error recieving from socket: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// Sends data to the specified endpoint.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="destination">The destination endpoint.</param>
        public virtual void SendData(byte[] data, IPEndPoint destination)
        {
            try
            {
                _udpClient.Send(data, data.Length, destination);

                if (_debug)
                {
                    _logger.Debug($"{_mode}: Send data to {destination}:\n{Utils.HexDump(data)}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{_mode}: Failed to send data: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the UDP connection.
        /// </summary>
        public virtual void CloseConnection()
        {
            _udpClient.Close();
            _logger.Information($"{_mode}: UDP connection closed.");
        }
    }
}
