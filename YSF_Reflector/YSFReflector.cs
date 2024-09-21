/*
* MMDVM_Reflector - YSF_Reflector
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

using Common;
using Common.Api;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using System.Numerics;

#nullable disable

namespace YSF_Reflector
{
    public class YSFReflector
    {
        public const int YSF_CALLSIGN_LENGTH = 10;
        public static string version = "01.00.00";

        private Config _config;
        private Reporter _reporter;
        private ILogger _logger;

        private List<YSFRepeater> _repeaters;
        private NetworkManager _networkManager;

        private CancellationTokenSource _cancellationTokenSource;

        public YSFReflector(Config config, CallsignAcl callsignAcl, Reporter reporter, ILogger logger)
        {
            _config = config;
            _reporter = reporter;
            _logger = logger;   

            _repeaters = new List<YSFRepeater>();
            _networkManager = new NetworkManager(_config.NetworkPort, _config.NetworkDebug);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Run()
        {
            _logger.Information("Starting YSFReflector");
            _logger.Information($"    Port: {_config.NetworkPort}");
            _logger.Information($"    Debug: {_config.NetworkDebug}");

            if (!_networkManager.OpenConnection())
            {
                _logger.Error("YSFReflector network open failed.");
                return;
            }

            _logger.Information($"YSFReflector version: {version} started.\n");

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
                    _reporter.Send(new Report { Mode = Common.DigitalMode.YSF, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters), DateTime = DateTime.Now });
                    _logger.Information($"YSF: New connection: {repeater.CallSign}; Address: {senderAddress}");
                }
                repeater.Refresh();
                _networkManager.SendPollResponse(senderAddress);
            }
            else if (buffer.Length >= 4 && System.Text.Encoding.ASCII.GetString(buffer, 0, 4) == "YSFU" && repeater != null)
            {
                // "YSFU" unlink
                _logger.Information($"YSF: Removing {repeater.CallSign}; YSF_UNLINK received.");
                _repeaters.Remove(repeater);
                _reporter.Send(new Report { Mode = Common.DigitalMode.YSF, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters), DateTime = DateTime.Now });
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
                _logger.Information($"YSF: NET transmssion, srcId: {srcCallsign}, dstId: {dstCallsign}, Callsign: {tagCallsign}");
                repeater.StartTransmission();
                _reporter.Send(uint.Parse(srcCallsign), uint.Parse(dstCallsign), tagCallsign, Common.DigitalMode.YSF, Common.Api.Type.CALL_START, string.Empty);
                _reporter.Send(new Report { Mode = Common.DigitalMode.YSF, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters), DateTime = DateTime.Now });
            }

            if ((buffer[34] & 0x01) == 0x01)
            {
                _logger.Information($"YSF: NET end of transmission, srcId: {srcCallsign}, dstId: {dstCallsign}, Callsign: {tagCallsign}");
                repeater.EndTransmission();
                _reporter.Send(uint.Parse(srcCallsign), uint.Parse(dstCallsign), tagCallsign, Common.DigitalMode.YSF, Common.Api.Type.CALL_END, string.Empty);
                _reporter.Send(new Report { Mode = Common.DigitalMode.YSF, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters), DateTime = DateTime.Now });
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

        private string PreparePeersListForReport(List<YSFRepeater> repeaters)
        {
            var peersInfo = repeaters.Select(repeater => new
            {
                CallSign = repeater.CallSign.Trim(),
                Address = repeater.Address.ToString(),
                Transmitting = repeater.IsTransmitting
            });

            return JsonConvert.SerializeObject(peersInfo, Formatting.Indented);
        }

        private void CleanUpRepeaters()
        {
            foreach (var repeater in _repeaters)
            {
                if (repeater.IsExpired())
                {
                    _logger.Warning($"YSF: Removing repeater {repeater.CallSign} due to inactivity.");
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
