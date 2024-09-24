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

using Common;
using Serilog;
using System.Net;

namespace NXDN_Reflector
{
    /// <summary>
    /// NXDN Networking class
    /// </summary>
    public class Network : BaseNetwork
    {
        /// <summary>
        /// Creates an instance of <see cref="Network"/> for NXDN communication.
        /// </summary>
        /// <param name="port">The port to bind the UDP client to.</param>
        /// <param name="debug">Enable or disable debug mode.</param>
        /// <param name="logger">Logger instance for logging.</param>
        public Network(int port, bool debug, ILogger logger)
            : base(port, DigitalMode.NXDN, logger, debug)
        {
        }

        /// <summary>
        /// Sends a NXDN poll response.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="destination">The destination endpoint.</param>
        public void SendPollResponse(byte[] data, IPEndPoint destination)
        {
            if (_debug)
                _logger.Debug($"NXDN: Poll response sent to {destination.Address}:{destination.Port}");
            SendData(data, destination);
        }

        /// <summary>
        /// Compares a buffer with a specific text up to a given length.
        /// </summary>
        /// <param name="buffer">The buffer to compare.</param>
        /// <param name="text">The text to compare with.</param>
        /// <param name="length">The length to compare.</param>
        /// <returns>True if the buffer matches the text; otherwise, false.</returns>
        public bool CompareBuffer(byte[] buffer, string text, int length)
        {
            return buffer.Length >= length && System.Text.Encoding.ASCII.GetString(buffer, 0, length) == text;
        }
    }
}
