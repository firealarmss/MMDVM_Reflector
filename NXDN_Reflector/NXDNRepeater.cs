﻿/*
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
    public class NXDNRepeater
    {
        public IPEndPoint Address { get; }
        public string CallSign { get; }
        public bool IsTransmitting { get; private set; }

        public NXDNRepeater(IPEndPoint address, byte[] buffer)
        {
            Address = address;
            CallSign = System.Text.Encoding.ASCII.GetString(buffer, 5, 10).Trim();
            IsTransmitting = false;
        }

        public void StartTransmission()
        {
            IsTransmitting = true;
        }

        public void EndTransmission()
        {
            IsTransmitting = false;
        }

        public bool IsSameAddress(IPEndPoint address)
        {
            return Address.Equals(address);
        }
    }

}
