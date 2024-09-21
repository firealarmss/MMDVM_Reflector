using Common.Api;
using P25_Reflector;
using YSF_Reflector;
using NXDN_Reflector;
using M17_Reflector;

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
    }
}