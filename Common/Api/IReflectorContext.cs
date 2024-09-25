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

namespace Common.Api
{
    /// <summary>
    /// Reflector context interface
    /// </summary>
    public interface IReflectorContext
    {
        /// <summary>
        /// Disconnect from reflector by callsign
        /// </summary>
        /// <param name="reflectorType"></param>
        /// <param name="callsign"></param>
        /// <returns></returns>
        bool DisconnectCallsign(string reflectorType, string callsign);

        /// <summary>
        /// Block from reflector by callsign
        /// </summary>
        /// <param name="reflectorType"></param>
        /// <param name="callsign"></param>
        /// <returns></returns>
        bool BlockCallsign(string reflectorType, string callsign);

        /// <summary>
        /// Unblock from reflector by callsign
        /// </summary>
        /// <param name="reflectorType"></param>
        /// <param name="callsign"></param>
        /// <returns></returns>
        bool UnBlockCallsign(string reflectorType, string callsign);

        /// <summary>
        /// Get reflectors status
        /// </summary>
        /// <param name="reflectorType"></param>
        /// <returns></returns>
        ReflectorStatus GetReflectorStatus (string reflectorType);
    }
}