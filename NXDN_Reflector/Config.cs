namespace NXDN_Reflector
{
    public class Config
    {
        public Config() { /* stub */ }

        public bool Enabled { get; set; }
        public int NetworkPort { get; set; }
        public bool NetworkDebug { get; set; }
        public ushort TargetGroup { get; set; }
    }
}
