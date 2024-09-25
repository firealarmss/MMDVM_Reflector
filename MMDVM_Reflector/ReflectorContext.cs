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
            switch (reflectorType.ToLower())
            {
                case "p25":
                    return P25Reflector?.Disconnect(callsign) ?? false;
                case "ysf":
                    return YSFReflector?.Disconnect(callsign) ?? false;
                case "nxdn":
                    return NXDNReflector?.Disconnect(callsign) ?? false;
                case "m17":
                    return M17Reflector?.Disconnect(callsign) ?? false;
                default:
                    return false;
            }
        }

        public bool BlockCallsign(string reflectorType, string callsign)
        {
            switch (reflectorType.ToLower())
            {
                case "p25":
                    return P25Reflector?.Block(callsign) ?? false;
                case "ysf":
                    return YSFReflector?.Block(callsign) ?? false;
                case "nxdn":
                    return NXDNReflector?.Block(callsign) ?? false;
                case "m17":
                    return M17Reflector?.Block(callsign) ?? false;
                default:
                    return false;
            }
        }

        public bool UnBlockCallsign(string reflectorType, string callsign)
        {
            switch (reflectorType.ToLower())
            {
                case "p25":
                    return P25Reflector?.UnBlock(callsign) ?? false;
                case "ysf":
                    return YSFReflector?.UnBlock(callsign) ?? false;
                case "nxdn":
                    return NXDNReflector?.UnBlock(callsign) ?? false;
                case "m17":
                    return M17Reflector?.UnBlock(callsign) ?? false;
                default:
                    return false;
            }
        }

        public ReflectorStatus GetReflectorStatus(string reflectorType)
        {
            switch (reflectorType.ToLower())
            {
                case "p25":
                    return P25Reflector?.Status();
                case "ysf":
                    return YSFReflector?.Status();
                case "nxdn":
                    return NXDNReflector?.Status();
                case "m17":
                    return M17Reflector?.Status();
                default:
                    return new ReflectorStatus { Mode = "Unkown", Status = "Error"};
            }
        }
    }
}