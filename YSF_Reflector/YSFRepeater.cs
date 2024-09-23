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

using System.Net;

namespace YSF_Reflector
{
    /// <summary>
    /// YSF Repeater class
    /// </summary>
    public class YSFRepeater
    {
        public IPEndPoint Address { get; private set; }
        public string CallSign { get; private set; }
        private DateTime _lastActive;
        public bool IsTransmitting { get; private set; }

        /// <summary>
        /// Creates an instance of <see cref="YSFRepeater"/>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public YSFRepeater(IPEndPoint address, byte[] data)
        {
            Address = address;
            CallSign = System.Text.Encoding.ASCII.GetString(data, 4, YSFReflector.YSF_CALLSIGN_LENGTH).Trim();
            _lastActive = DateTime.Now;
            IsTransmitting = false;
        }

        /// <summary>
        /// Refresh activity
        /// </summary>
        public void Refresh()
        {
            _lastActive = DateTime.Now;
        }

        /// <summary>
        /// Helper to check if two address are equal
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool IsSameAddress(IPEndPoint address)
        {
            return Address.Equals(address);
        }

        /// <summary>
        /// Helper to check if the repeater is no longer active
        /// </summary>
        /// <returns></returns>
        public bool IsExpired()
        {
            return (DateTime.Now - _lastActive).TotalSeconds > 15;
        }

        /// <summary>
        /// Helper to set the transmission state to true
        /// </summary>
        public void StartTransmission()
        {
            IsTransmitting = true;
        }

        /// <summary>
        /// Helper to set the transmission state to false
        /// </summary>
        public void EndTransmission()
        {
            IsTransmitting = false;
        }
    }
}
