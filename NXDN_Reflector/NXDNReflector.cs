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

using Common;
using Common.Api;
using Newtonsoft.Json;
using Serilog;
using System.Net;

#nullable disable

namespace NXDN_Reflector
{
    /// <summary>
    /// NXDN Reflector class
    /// </summary>
    public class NXDNReflector
    {
        public static string version = "01.00.00";

        private Config _config;
        private CallsignAcl _acl;
        private Reporter _reporter;
        private ILogger _logger;

        private List<NXDNRepeater> _repeaters;
        private Network _networkManager;

        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Creates an instance of <see cref="NXDNReflector"/>
        /// </summary>
        /// <param name="config"></param>
        /// <param name="callsignAcl"></param>
        /// <param name="reporter"></param>
        /// <param name="logger"></param>
        public NXDNReflector(Config config, CallsignAcl callsignAcl, Reporter reporter, ILogger logger)
        {
            _config = config;
            _acl = callsignAcl;
            _reporter = reporter;
            _logger = logger;

            _repeaters = new List<NXDNRepeater>();
            _networkManager = new Network(_config.NetworkPort, _config.NetworkDebug, _logger);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Fires up the reflector
        /// </summary>
        public void Run()
        {
            _logger.Information("Starting NXDNReflector");
            _logger.Information($"    Port: {_config.NetworkPort}");
            _logger.Information($"    Debug: {_config.NetworkDebug}");

            if (!_networkManager.OpenConnection())
            {
                _logger.Error("NXDNReflector network open failed.");
                return;
            }

            _logger.Information($"NXDNReflector version: {version} started.\n");

            Task.Factory.StartNew(() => ReceiveLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
                Mode = "NXDN",
                Status = status,
                Port = _config.NetworkPort,
                ConnectedPeers = _repeaters.Count,
                Acl = null
            };
        }

        /// <summary>
        /// Helper to disconnect a current <see cref="P25Peer"/> based off its callsign
        /// </summary>
        /// <param name="callsign"></param>
        /// <returns></returns>
        public bool Disconnect(string callsign)
        {
            _logger.Information($"NXDN: Attempting to disconnect callsign {callsign}");

            var peerToRemove = _repeaters.FirstOrDefault(p => p.CallSign.Equals(callsign, StringComparison.OrdinalIgnoreCase));

            if (peerToRemove != null)
            {
                if (_repeaters.Remove(peerToRemove))
                {
                    _reporter.Send(new Report { Mode = DigitalMode.NXDN, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters) });

                    _logger.Information($"NXDN: Successfully disconnected {peerToRemove.CallSign} from {peerToRemove.Address}");

                    return true;
                }
                else
                {
                    _logger.Error($"NXDN: Failed to remove repeater {callsign} from repeaters list.");
                    return false;
                }
            }
            else
            {
                _logger.Warning($"NXDN: Callsign {callsign} not found among connected repeaters.");
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
            Console.WriteLine($"NXDN: Block {callsign}");
            return false;
        }

        /// <summary>
        /// Helper to un block the specified callsign
        /// </summary>
        /// <param name="callsign"></param>
        /// <returns></returns>
        public bool UnBlock(string callsign)
        {
            var entry = _acl.Entries.Find(e => e.Callsign == callsign);

            if (entry != null)
            {
                entry.Allowed = true;
                _logger.Information($"NXDN: Unblocked callsign {callsign}");
            }
            else
            {
                _logger.Warning($"NXDN: Callsign {callsign} not found in ACL. Nothing to unblock.");
                return false;
            }

            try
            {
                _acl.Save();
                _logger.Information("NXDN: ACL updated and saved successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"NXDN: Failed to save ACL: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Main loop to receive UDP data
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Callback to handle all incoming UDP data
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="senderAddress"></param>
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

                        if (!_config.Acl || _acl.CheckCallsignAcl(repeater.CallSign))
                        {
                            _repeaters.Add(repeater);
                            _reporter.Send(new Report { Mode = Common.DigitalMode.NXDN, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters), DateTime = DateTime.Now });
                            _logger.Information($"NXDN: Added repeater: {repeater.CallSign} from {senderAddress}");
                        } else
                        {
                            _logger.Information($"NXDN: Connection ACL Rejection {senderAddress}");
                        }
                    }

                    _networkManager.SendPollResponse(buffer, senderAddress);
                }
            }
            else if (_networkManager.CompareBuffer(buffer, "NXDNU", 5) && buffer.Length == 17)
            {
                ushort receivedGroupId = (ushort)((buffer[15] << 8) | buffer[16]);
                if (receivedGroupId == _config.TargetGroup && repeater != null)
                {
                    _logger.Information($"NXDN: Removing repeater: {repeater.CallSign} from {senderAddress}");
                    _repeaters.Remove(repeater);
                    _reporter.Send(new Report { Mode = Common.DigitalMode.NXDN, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters), DateTime = DateTime.Now });
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

        /// <summary>
        /// Helper to handle a transmission
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="repeater"></param>
        /// <param name="senderAddress"></param>
        private void HandleDataTransmission(byte[] buffer, NXDNRepeater repeater, IPEndPoint senderAddress)
        {
            ushort srcId = (ushort)((buffer[5] << 8) | buffer[6]);
            ushort dstId = (ushort)((buffer[7] << 8) | buffer[8]);
            bool isGroupCall = (buffer[9] & 0x01) == 0x01;

            if (isGroupCall && dstId == _config.TargetGroup)
            {
                if (!repeater.IsTransmitting)
                {
                    _logger.Information($"NXDN: Transmission started from {repeater.CallSign}, srcId: {srcId}, dstId: {dstId}");
                    _reporter.Send(srcId, dstId, repeater.CallSign, Common.DigitalMode.NXDN, Common.Api.Type.CALL_START, string.Empty);
                    repeater.StartTransmission();
                    _reporter.Send(new Report { Mode = Common.DigitalMode.NXDN, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters), DateTime = DateTime.Now });
                }

                RelayToAllRepeaters(buffer, senderAddress);

                if ((buffer[9] & 0x08) == 0x08)
                {
                    _logger.Information($"NXDN: End of transmission from {repeater.CallSign}, srcId: {srcId}");
                    _reporter.Send(srcId, dstId, repeater.CallSign, Common.DigitalMode.NXDN, Common.Api.Type.CALL_END, string.Empty);
                    repeater.EndTransmission();
                    _reporter.Send(new Report { Mode = Common.DigitalMode.NXDN, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_repeaters), DateTime = DateTime.Now });
                }
            }
        }

        /// <summary>
        /// Helper to make a JSON list of current <see cref="P25Peer"/>
        /// </summary>
        /// <param name="peers"></param>
        /// <returns></returns>
        private string PreparePeersListForReport(List<NXDNRepeater> repeaters)
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
        /// Helper to find a <see cref="P25Peer"/> based on its <see cref="IPEndPoint"/>
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private NXDNRepeater FindRepeater(IPEndPoint address)
        {
            return _repeaters.Find(r => r.IsSameAddress(address));
        }
    }
}
