/*
* MMDVM_Reflector - P25_Reflector
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

namespace P25_Reflector
{
    /// <summary>
    /// Class to keep track of p25 transmission state
    /// </summary>
    public class TransmissionState
    {
        public bool Seen64 { get; set; }
        public bool Seen65 { get; set; }
        public bool Displayed { get; set; }
        public byte Lcf { get; set; }
        public uint SrcId { get; set; }
        public uint DstId { get; set; }

        /// <summary>
        /// Creates an instance of <see cref="TransmissionState"/>
        /// </summary>
        public TransmissionState()
        {
            Reset();
        }

        /// <summary>
        /// Helper to rest values
        /// </summary>
        public void Reset()
        {
            Seen64 = false;
            Seen65 = false;
            Displayed = false;
            Lcf = 0;
            SrcId = 0;
            DstId = 0;
        }
    }
}
