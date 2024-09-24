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

namespace M17_Reflector
{
    public class Config
    {
        public List<AllowedModulesConfig> Modules { get; set; }

        public Config() { /* stub */ }

        public bool Enabled { get; set; }
        public bool Acl {  get; set; }
        public int NetworkPort { get; set; }
        public string Reflector { get; set; }
        public bool NetworkDebug { get; set; }

        public bool CheckModule(string module)
        {
            try
            {
                AllowedModulesConfig mod = Modules.Find(m => m.Module == module);

                return mod != null && mod.Enabled;
            }
            catch (Exception ex)
            {
                var ex2 = ex; // yeah i did that
                return false;
            }
        }
    }

    public class AllowedModulesConfig
    {
        public bool Enabled { get; set; }
        public string Module { get; set; }
    }
}
