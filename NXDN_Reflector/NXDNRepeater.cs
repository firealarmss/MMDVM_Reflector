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

namespace NXDN_Reflector
{
    /// <summary>
    /// NXDN Repeater class
    /// </summary>
    public class NXDNRepeater
    {
        public IPEndPoint Address { get; }
        public string CallSign { get; }
        public bool IsTransmitting { get; private set; }

        /// <summary>
        /// Creates an instance of <see cref="NXDNRepeater"/>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="buffer"></param>
        public NXDNRepeater(IPEndPoint address, byte[] buffer)
        {
            Address = address;
            CallSign = System.Text.Encoding.ASCII.GetString(buffer, 5, 10).Trim();
            IsTransmitting = false;
        }

        /// <summary>
        /// Helper to set tranmission state to true
        /// </summary>
        public void StartTransmission()
        {
            IsTransmitting = true;
        }

        /// <summary>
        /// Helper to set tranmission state to false
        /// </summary>
        public void EndTransmission()
        {
            IsTransmitting = false;
        }

        /// <summary>
        /// Helper to see if two <see cref="IPEndPoint"/> are equal
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool IsSameAddress(IPEndPoint address)
        {
            return Address.Equals(address);
        }
    }

}
