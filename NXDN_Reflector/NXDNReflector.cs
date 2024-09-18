/*
* MMDVM_Reflector - NXDN_Reflector
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
using System.Net;

#nullable disable

namespace NXDN_Reflector
{
    public class NXDNReflector
    {
        public static string version = "01.00.00";

        private Config _config;
        private Reporter _reporter;

        private List<NXDNRepeater> _repeaters;
        private NetworkManager _networkManager;

        private CancellationTokenSource _cancellationTokenSource;

        public NXDNReflector(Config config, Reporter reporter)
        {
            _config = config;
            _reporter = reporter;
            _repeaters = new List<NXDNRepeater>();
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
                Console.WriteLine("NXDNReflector network open failed.");
                return;
            }

            Console.WriteLine($"NXDNReflector version: {version} started.\n");

            Task.Factory.StartNew(() => ReceiveLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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

        private void HandleIncomingData(byte[] buffer, IPEndPoint senderAddress)
        {
            NXDNRepeater repeater = FindRepeater(senderAddress);

            if (_networkManager.CompareBuffer(buffer, "NXDNP", 5) && buffer.Length == 17)
            {
                ushort receivedGroupId = (ushort)((buffer[15] << 8) | buffer[16]);
                if (receivedGroupId == _config.TargetGroup)
                {
                    if (repeater == null)
                    {
                        repeater = new NXDNRepeater(senderAddress, buffer);
                        _repeaters.Add(repeater);
                        Console.WriteLine($"NXDN: Added repeater: {repeater.CallSign} from {senderAddress}");
                    }

                    _networkManager.SendPollResponse(buffer, senderAddress);
                }
            }
            else if (_networkManager.CompareBuffer(buffer, "NXDNU", 5) && buffer.Length == 17)
            {
                ushort receivedGroupId = (ushort)((buffer[15] << 8) | buffer[16]);
                if (receivedGroupId == _config.TargetGroup && repeater != null)
                {
                    Console.WriteLine($"NXDN: Removing repeater: {repeater.CallSign} from {senderAddress}");
                    _repeaters.Remove(repeater);
                }
            }
            else if (_networkManager.CompareBuffer(buffer, "NXDND", 5) && buffer.Length == 43)
            {
                if (repeater != null)
                {
                    HandleDataTransmission(buffer, repeater, senderAddress);
                }
            }
        }

        private void HandleDataTransmission(byte[] buffer, NXDNRepeater repeater, IPEndPoint senderAddress)
        {
            ushort srcId = (ushort)((buffer[5] << 8) | buffer[6]);
            ushort dstId = (ushort)((buffer[7] << 8) | buffer[8]);
            bool isGroupCall = (buffer[9] & 0x01) == 0x01;

            if (isGroupCall && dstId == _config.TargetGroup)
            {
                if (!repeater.IsTransmitting)
                {
                    Console.WriteLine($"NXDN: Transmission started from {repeater.CallSign}, srcId: {srcId}, dstId: {dstId}");
                    repeater.StartTransmission();
                }

                RelayToAllRepeaters(buffer, senderAddress);

                if ((buffer[9] & 0x08) == 0x08)
                {
                    Console.WriteLine($"NXDN: End of transmission from {repeater.CallSign}, srcId: {srcId}");
                    repeater.EndTransmission();
                }
            }
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

        private NXDNRepeater FindRepeater(IPEndPoint address)
        {
            return _repeaters.Find(r => r.IsSameAddress(address));
        }
    }
}
