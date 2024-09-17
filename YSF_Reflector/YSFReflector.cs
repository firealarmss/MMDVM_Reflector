using System.Net;

#nullable disable

namespace YSF_Reflector
{
    public class YSFReflector
    {
        public const int YSF_CALLSIGN_LENGTH = 10;
        public static string version = "01.00.00";

        private Config _config;
        private List<YSFRepeater> _repeaters;
        private NetworkManager _networkManager;
        private CancellationTokenSource _cancellationTokenSource;

        public YSFReflector(Config config)
        {
            _config = config;
            _repeaters = new List<YSFRepeater>();
            _networkManager = new NetworkManager(_config.NetworkPort, _config.NetworkDebug);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Run()
        {
            Console.WriteLine("Starting NXDNReflector");
            Console.WriteLine($"    Port: {_config.NetworkPort}");
            Console.WriteLine($"    Debug: {_config.NetworkDebug}");

            if (!_networkManager.OpenConnection())
            {
                Console.WriteLine("YSFReflector network open failed.");
                return;
            }

            Console.WriteLine($"YSFReflector version: {version} started.");

            Task.Factory.StartNew(() => ReceiveLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Task.Factory.StartNew(() => CleanupLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var (buffer, senderAddress) = _networkManager.ReceiveData();
                if (buffer != null)
                {
                    HandleIncomingData(buffer, senderAddress);
                }

                await Task.Delay(10, token);
            }
        }

        private async Task CleanupLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                CleanUpRepeaters();
                await Task.Delay(5000, token);
            }
        }

        private void HandleIncomingData(byte[] buffer, IPEndPoint senderAddress)
        {
            YSFRepeater repeater = FindRepeater(senderAddress);

            if (buffer.Length >= 4 && System.Text.Encoding.ASCII.GetString(buffer, 0, 4) == "YSFP")
            {
                // "YSFP" poll
                if (repeater == null)
                {
                    repeater = new YSFRepeater(senderAddress, buffer);
                    _repeaters.Add(repeater);
                    Console.WriteLine($"YSF: New connection: {repeater.CallSign}; Address: {senderAddress}");
                }
                repeater.Refresh();
                _networkManager.SendPollResponse(senderAddress);
            }
            else if (buffer.Length >= 4 && System.Text.Encoding.ASCII.GetString(buffer, 0, 4) == "YSFU" && repeater != null)
            {
                // "YSFU" unlink
                Console.WriteLine($"YSF: Removing {repeater.CallSign}; YSF_UNLINK received.");
                _repeaters.Remove(repeater);
            }
            else if (buffer.Length >= 4 && System.Text.Encoding.ASCII.GetString(buffer, 0, 4) == "YSFD" && repeater != null)
            {
                // "YSFD" data transmission
                HandleYSFData(buffer, repeater, senderAddress);
            }
        }

        private void HandleYSFData(byte[] buffer, YSFRepeater repeater, IPEndPoint senderAddress)
        {
            byte[] tag = new byte[YSF_CALLSIGN_LENGTH];
            byte[] src = new byte[YSF_CALLSIGN_LENGTH];
            byte[] dst = new byte[YSF_CALLSIGN_LENGTH];

            Buffer.BlockCopy(buffer, 4, tag, 0, YSF_CALLSIGN_LENGTH);
            Buffer.BlockCopy(buffer, 14, src, 0, YSF_CALLSIGN_LENGTH);
            Buffer.BlockCopy(buffer, 24, dst, 0, YSF_CALLSIGN_LENGTH);

            string tagCallsign = ParseCallsign(tag);
            string srcCallsign = ParseCallsign(src);
            string dstCallsign = ParseCallsign(dst);

            if (!repeater.IsTransmitting)
            {
                Console.WriteLine($"YSF: NET transmssion, srcId: {srcCallsign}, dstId: {dstCallsign}, Callsign: {tagCallsign}");
                repeater.StartTransmission();
            }

            if ((buffer[34] & 0x01) == 0x01)
            {
                Console.WriteLine($"YSF: NET end of transmission, srcId: {srcCallsign}, dstId: {dstCallsign}, Callsign: {tagCallsign}");
                repeater.EndTransmission();
            }

            RelayToAllRepeaters(buffer, senderAddress);
        }

        private string ParseCallsign(byte[] callsignBytes)
        {
            string callsign = System.Text.Encoding.ASCII.GetString(callsignBytes).Trim();
            return string.IsNullOrWhiteSpace(callsign) ? "?Unknown" : callsign;
        }

        private void RelayToAllRepeaters(byte[] buffer, IPEndPoint senderAddress)
        {
            foreach (var repeater in _repeaters)
            {
                if (!repeater.IsSameAddress(senderAddress))
                {
                    _networkManager.SendData(buffer, repeater.Address);
                }
            }
        }

        private void CleanUpRepeaters()
        {
            foreach (var repeater in _repeaters)
            {
                if (repeater.IsExpired())
                {
                    Console.WriteLine($"YSF: Removing repeater {repeater.CallSign} due to inactivity.");
                    _repeaters.Remove(repeater);
                    break;
                }
            }
        }

        private YSFRepeater FindRepeater(IPEndPoint address)
        {
            return _repeaters.Find(r => r.IsSameAddress(address));
        }
    }
}
