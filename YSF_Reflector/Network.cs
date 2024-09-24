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
using Common.Api;
using Serilog;
using System.Net;
using System.Text;

namespace YSF_Reflector
{
    /// <summary>
    /// YSF Networking class
    /// </summary>
    public class Network : BaseNetwork
    {
        private int _id;
        private string _name;
        private string _description;

        /// <summary>
        /// Creates an instance of <see cref="Network"/> for YSF communication.
        /// </summary>
        /// <param name="port">The port to bind the UDP client to.</param>
        /// <param name="id">Unique identifier for the YSF reflector.</param>
        /// <param name="name">Name of the reflector.</param>
        /// <param name="description">Description of the reflector.</param>
        /// <param name="debug">Enable or disable debug mode.</param>
        /// <param name="logger">Logger instance for logging.</param>
        public Network(int port, int id, string name, string description, bool debug, ILogger logger)
            : base(port, DigitalMode.YSF, logger, debug)
        {
            _id = id;
            _name = name;
            _description = description;
        }

        /// <summary>
        /// Helper to send a YSF poll response.
        /// </summary>
        /// <param name="destination">The destination endpoint.</param>
        public void SendPollResponse(IPEndPoint destination)
        {
            byte[] buffer = Encoding.ASCII.GetBytes("YSFPREFLECTOR ");

            if (_debug)
                _logger.Debug($"Sending YSF Network Poll: {BitConverter.ToString(buffer)}");
            
            SendData(buffer, destination);
        }

        /// <summary>
        /// Sends YSF status response.
        /// </summary>
        /// <param name="destination">The destination endpoint.</param>
        /// <param name="count">The count to include in the status.</param>
        public void SendStatus(IPEndPoint destination, int count)
        {
            try
            {
                string status = ComputeStatus(count);
                SendData(Encoding.ASCII.GetBytes(status), destination);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error sending YSF status: {ex.Message}");
            }
        }

        /// <summary>
        /// Computes and formats the YSF status response.
        /// </summary>
        /// <param name="count">The number of connected clients.</param>
        /// <returns>Formatted status string.</returns>
        private string ComputeStatus(int count)
        {
            uint hash = (uint)_id;

            if (hash == 0U)
            {
                foreach (char c in _name)
                {
                    uint charValue = (uint)c;
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
        /// Helper method to pad or truncate a string to a specific length.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="length">The desired length.</param>
        /// <returns>Padded or truncated string.</returns>
        private string PadOrTruncate(string input, int length)
        {
            return input.Length > length ? input.Substring(0, length) : input.PadRight(length, ' ');
        }
    }
}
