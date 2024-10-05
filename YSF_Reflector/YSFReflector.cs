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
using System.Text;

#nullable disable

namespace YSF_Reflector
{
    /// <summary>
    /// YSF Reflector class
    /// </summary>
    public class YSFReflector
    {
        public const int YSF_CALLSIGN_LENGTH = 10;
        public static string version = "01.00.00";

        private Config _config;
        private Reporter _reporter;
        private ILogger _logger;

        private List<YSFRepeater> _repeaters;
        private Network _networkManager;

        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Creates an instance of <see cref="YSFReflector"/>
        /// </summary>
        /// <param name="config"></param>
        /// <param name="callsignAcl"></param>
        /// <param name="reporter"></param>
        /// <param name="logger"></param>
        public YSFReflector(Config config, CallsignAcl callsignAcl, Reporter reporter, ILogger logger)
        {
            _config = config;
            _reporter = reporter;
            _logger = logger;   

            _repeaters = new List<YSFRepeater>();
            _networkManager = new Network(_config.NetworkPort, _config.Id, _config.Name, _config.Description, _config.NetworkDebug, _logger);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Fires up the reflector
        /// </summary>
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

        /// <summary>
        /// Gracefully stop the reflector instance
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _networkManager.CloseConnection();
        }

        /// <summary>
        /// Helper to get status of reflector
        /// </summary>
        /// <returns></returns>
        public ReflectorStatus Status()
        {
            string status = "Error";

            if (!_cancellationTokenSource.Token.IsCancellationRequested)
                status = "Running";
            else
                status = "Stopped";

            return new ReflectorStatus
            {
                Mode = "YSF",
                Status = status,
                Port = _config.NetworkPort,
                ConnectedPeers = _repeaters.Count,
                Acl = null
            };
        }

        /// <summary>
        /// Helper to disconnect a current <see cref="YSFRepeater"/> based off its callsign
        /// </summary>
        /// <param name="callsign"></param>
        /// <returns></returns>
        public bool Disconnect(string callsign)
        {
            _logger.Information($"YSF: Attempting to disconnect callsign {callsign}");

            var peerToRemove = _repeaters.FirstOrDefault(p => p.CallSign.Equals(callsign, StringComparison.OrdinalIgnoreCase));

            if (peerToRemove != null)
            {
                if (_repeaters.Remove(peerToRemove))
                {
                    _reporter.Send(new Report { Mode = DigitalMode.YSF, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters) });

                    _logger.Information($"YSF: Successfully disconnected {peerToRemove.CallSign} from {peerToRemove.Address}");

                    return true;
                }
                else
                {
                    _logger.Error($"YSF: Failed to remove repeater {callsign} from repeaters list.");
                    return false;
                }
            }
            else
            {
                _logger.Warning($"YSF: Callsign {callsign} not found among connected repeaters.");
                return false;
            }
        }

        /// <summary>
        /// Helper to block the specified callsign
        /// </summary>
        /// <param name="callsign"></param>
        /// <returns></returns>
        public bool Block(string callsign)
        {
            Console.WriteLine($"YSF: Block {callsign}");
            return false;
        }

        /// <summary>
        /// Helper to un block the specified callsign
        /// </summary>
        /// <param name="callsign"></param>
        /// <returns></returns>
        public bool UnBlock(string callsign)
        {
            Console.WriteLine($"YSF: Unblock {callsign}");
            return false;
        }

        /// <summary>
        /// Main loop to receive UDP data
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task ReceiveLoop(CancellationToken token)
        {
            try
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
            catch (Exception ex)
            {
                _logger.Error(ex, "YSF Reflector died!! error in recieve loop");
            }
        }

        /// <summary>
        /// Loop to detect an inactive <see cref="YSFRepeater"/> that did not properly tear down
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task CleanupLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                CleanUpRepeaters();
                await Task.Delay(15000, token);
            }
        }

        /// <summary>
        /// Callback to handle all incoming UDP data
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="senderAddress"></param>
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
            else if (buffer.Length >= 4 && System.Text.Encoding.ASCII.GetString(buffer, 0, 4) == "YSFS")
            {
                // "YSFS" status. I think this is only for the reflector registry?
                _networkManager.SendStatus(senderAddress, _repeaters.Count);
            }
            else if (buffer.Length >= 4 && System.Text.Encoding.ASCII.GetString(buffer, 0, 4) == "YSFV")
            {
                // "YSFV" version. I think this is only for the reflector registry?
                _networkManager.SendData(Encoding.ASCII.GetBytes($"YSFVMMDVM_Reflector {version}") ,senderAddress);
            }

            throw new Exception("test");
        }

        /// <summary>
        /// Helper to parse YSF data
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="repeater"></param>
        /// <param name="senderAddress"></param>
        private void HandleYSFData(byte[] buffer, YSFRepeater repeater, IPEndPoint senderAddress)
        {
            try
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
                    _reporter.Send(0, 0, tagCallsign, Common.DigitalMode.YSF, Common.Api.Type.CALL_START, string.Empty);
                    _reporter.Send(new Report { Mode = Common.DigitalMode.YSF, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters), DateTime = DateTime.Now });
                }

                if ((buffer[34] & 0x01) == 0x01)
                {
                    _logger.Information($"YSF: NET end of transmission, srcId: {srcCallsign}, dstId: {dstCallsign}, Callsign: {tagCallsign}");
                    repeater.EndTransmission();
                    _reporter.Send(0, 0, tagCallsign, Common.DigitalMode.YSF, Common.Api.Type.CALL_END, string.Empty);
                    _reporter.Send(new Report { Mode = Common.DigitalMode.YSF, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters), DateTime = DateTime.Now });
                }

                RelayToAllRepeaters(buffer, senderAddress);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());   
            }
        }

        /// <summary>
        /// Helper to parse YSF callsign
        /// </summary>
        /// <param name="callsignBytes"></param>
        /// <returns></returns>
        private string ParseCallsign(byte[] callsignBytes)
        {
            string callsign = System.Text.Encoding.ASCII.GetString(callsignBytes).Trim();
            return string.IsNullOrWhiteSpace(callsign) ? "?Unknown" : callsign;
        }

        /// <summary>
        /// Broadcast a message to all repeaters except ourself
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="senderAddress"></param>
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

        /// <summary>
        /// Helper to make a JSON list of current <see cref="YSFRepeater"/>
        /// </summary>
        /// <param name="repeaters"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Helper to clean inactive repeaters
        /// </summary>
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

        /// <summary>
        /// Helper to find a <see cref="YSFRepeater"/> based on its <see cref="IPEndPoint"/>
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private YSFRepeater FindRepeater(IPEndPoint address)
        {
            return _repeaters.Find(r => r.IsSameAddress(address));
        }
    }
}
