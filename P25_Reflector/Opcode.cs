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
    public static class Opcode
    {
        public const byte NET_TAG_HEADER = 0x00;
        public const byte NET_TAG_DATA = 0x01;
        public const byte NET_TAG_LOST = 0x02;
        public const byte NET_TAG_EOT = 0x03;
        public const byte NET_TERM = 0x80;
        public const byte NET_POLL = 0xF0;
        public const byte NET_UNLINK = 0xF1;
    }
}
