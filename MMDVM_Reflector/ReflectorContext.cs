/*
* MMDVM_Reflector - MMDVM_Reflector
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

using Common.Api;
using P25_Reflector;
using YSF_Reflector;
using NXDN_Reflector;
using M17_Reflector;
using Common;

namespace MMDVM_Reflector
{
    public class ReflectorContext : IReflectorContext
    {
        public P25Reflector P25Reflector { get; private set; }
        public YSFReflector YSFReflector { get; private set; }
        public NXDNReflector NXDNReflector { get; private set; }
        public M17Reflector M17Reflector { get; private set; }

        public ReflectorContext(P25Reflector p25, YSFReflector ysf, NXDNReflector nxdn, M17Reflector m17)
        {
            P25Reflector = p25;
            YSFReflector = ysf;
            NXDNReflector = nxdn;
            M17Reflector = m17;
        }

        public bool DisconnectCallsign(string reflectorType, string callsign)
        {
            return reflectorType.ToLower() switch
            {
                "p25" => P25Reflector?.Disconnect(callsign) ?? false,
                "ysf" => YSFReflector?.Disconnect(callsign) ?? false,
                "nxdn" => NXDNReflector?.Disconnect(callsign) ?? false,
                "m17" => M17Reflector?.Disconnect(callsign) ?? false,
                _ => false,
            };
        }

        public bool BlockCallsign(string reflectorType, string callsign)
        {
            return reflectorType.ToLower() switch
            {
                "p25" => P25Reflector?.Block(callsign) ?? false,
                "ysf" => YSFReflector?.Block(callsign) ?? false,
                "nxdn" => NXDNReflector?.Block(callsign) ?? false,
                "m17" => M17Reflector?.Block(callsign) ?? false,
                _ => false,
            };
        }

        public bool UnBlockCallsign(string reflectorType, string callsign)
        {
            return reflectorType.ToLower() switch
            {
                "p25" => P25Reflector?.UnBlock(callsign) ?? false,
                "ysf" => YSFReflector?.UnBlock(callsign) ?? false,
                "nxdn" => NXDNReflector?.UnBlock(callsign) ?? false,
                "m17" => M17Reflector?.UnBlock(callsign) ?? false,
                _ => false,
            };
        }

        public ReflectorStatus GetReflectorStatus(string reflectorType)
        {
            return reflectorType.ToLower() switch
            {
                "p25" => P25Reflector?.Status(),
                "ysf" => YSFReflector?.Status(),
                "nxdn" => NXDNReflector?.Status(),
                "m17" => M17Reflector?.Status(),
                _ => new ReflectorStatus { Mode = "Unknown", Status = "Error" },
            };
        }
    }
}