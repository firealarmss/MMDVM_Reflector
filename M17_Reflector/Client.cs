using System.Net;

namespace M17_Reflector
{
    public class Client
    {
        public IPEndPoint Address { get; }

        public Client(IPEndPoint address)
        {
            Address = address;
        }
    }
}
