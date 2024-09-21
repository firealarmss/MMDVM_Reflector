using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
