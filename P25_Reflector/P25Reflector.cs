/*
* MMDVM_Reflector - P25_Reflector
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

namespace P25_Reflector
{
    public class P25Reflector
    {
        public static string version = "01.00.00";

        private Config _config;
        private CallsignAcl _acl;
        private Reporter _reporter;
        private ILogger _logger;

        private List<P25Peer> _peers;
        private NetworkManager _networkManager;

        private CancellationTokenSource _cancellationTokenSource;

        public P25Reflector(Config config, CallsignAcl callsignAcl, Reporter reporter, ILogger logger)
        {
            _config = config;
            _acl = callsignAcl;
            _reporter = reporter;
            _logger = logger;

            _peers = new List<P25Peer>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Run()
        {
            _logger.Information("Starting P25Reflector");
            _logger.Information($"    Port: {_config.NetworkPort}");
            _logger.Information($"    Debug: {_config.NetworkDebug}");

            _networkManager = new NetworkManager(_config.NetworkPort, _config.NetworkDebug);
            if (!_networkManager.OpenConnection())
            {
                _logger.Error("P25Reflector network open failed");
                return;
            }

            _logger.Information($"P25Reflector version: {version} started.\n");

            Task.Factory.StartNew(() => ReceiveLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Task.Factory.StartNew(() => CleanupLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        public bool Disconnect(string callsign)
        {
            _logger.Information($"P25: Attempting to disconnect callsign {callsign}");

            var peerToRemove = _peers.FirstOrDefault(p => p.CallSign.Trim().Equals(callsign, StringComparison.OrdinalIgnoreCase));

            if (peerToRemove != null)
            {
                if (_peers.Remove(peerToRemove))
                {
                    _reporter.Send(new Report { Mode = DigitalMode.P25, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_peers) });

                    _logger.Information($"P25: Successfully disconnected {peerToRemove.CallSign} from {peerToRemove.Address}");

                    return true;
                }
                else
                {
                    _logger.Error($"P25: Failed to remove repeater {callsign.Trim()} from repeaters list.");
                    return false;
                }
            }
            else
            {
                _logger.Warning($"P25: Callsign {callsign.Trim()} not found among connected repeaters.");
                return false;
            }
        }

        public bool Block(string callsign)
        {
            var entry = _acl.Entries.Find(e => e.Callsign == callsign);

            Disconnect(callsign);

            if (entry != null)
            {
                entry.Allowed = false;
                Console.WriteLine($"P25: Blocked existing callsign {callsign}");
            }
            else
            {
                entry = new CallsignEntry { Callsign = callsign, Allowed = false };
                _acl.Entries.Add(entry);
                Console.WriteLine($"P25: Blocked new callsign {callsign}");
            }

            try
            {
                _acl.Save();
                Console.WriteLine("ACL updated and saved successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save ACL: {ex.Message}");
                return false;
            }
        }

        public bool UnBlock(string callsign)
        {
            var entry = _acl.Entries.Find(e => e.Callsign == callsign);

            if (entry != null)
            {
                entry.Allowed = true;
                _logger.Information($"P25: Unblocked callsign {callsign}");
            }
            else
            {
                _logger.Warning($"P25: Callsign {callsign} not found in ACL. Nothing to unblock.");
                return false;
            }

            try
            {
                _acl.Save();
                _logger.Information("ACL updated and saved successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save ACL: {ex.Message}");
                return false;
            }
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
                await Task.Delay(9000, token);
            }
        }

        private void HandleIncomingData(byte[] buffer, IPEndPoint senderAddress)
        {
            P25Peer repeater = FindRepeater(senderAddress);
            byte opcode = buffer[0];

            switch (opcode)
            {
                case Opcode.NET_POLL:
                    if (repeater == null)
                    {
                        repeater = new P25Peer(senderAddress, buffer);

                        if (!_config.Acl || _acl.CheckCallsignAcl(repeater.CallSign.Trim()))
                        {
                            _peers.Add(repeater);

                            _reporter.Send(0, 0, string.Empty, DigitalMode.P25, Common.Api.Type.CONNECTION, PreparePeersListForReport(_peers));
                            _logger.Information($"P25: New connection: {repeater.CallSign.Trim()}; Address: {senderAddress}");
                        } else
                        {
                            _logger.Warning($"P25: NACK: {repeater.CallSign.Trim()}; Address: {senderAddress}; Reason: ACL Rejection");
                        }
                    }
                    else
                    {
                        if (_acl.CheckCallsignAcl(repeater.CallSign.Trim()) && _config.Acl)
                            repeater.Refresh();
                    }

                    _networkManager.SendData(buffer, senderAddress);
                    break;

                case Opcode.NET_UNLINK:
                    if (repeater != null)
                    {
                        _logger.Information($"P25: Removing {repeater.CallSign.Trim()}; NET_UNLINK received.");
                        _peers.Remove(repeater);
                        _reporter.Send(0, 0, string.Empty, DigitalMode.P25, Common.Api.Type.CONNECTION, PreparePeersListForReport(_peers));
                    }
                    break;

                case 0x64:
                    if (repeater == null) return;

                    if (!repeater.State.Seen64)
                    {
                        repeater.State.Lcf = buffer[1];
                        repeater.State.Seen64 = true;
                    }
                    break;

                case 0x65:
                    if (repeater == null) return;

                    if (!repeater.State.Seen65)
                    {
                        repeater.State.DstId = (uint)((buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
                        repeater.State.Seen65 = true;
                    }
                    break;

                case 0x66:
                    if (repeater == null) return;

                    if (repeater.State.Seen64 && repeater.State.Seen65 && !repeater.State.Displayed)
                    {
                        repeater.State.SrcId = (uint)((buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
                        repeater.State.Displayed = true;

                        _reporter.Send(new Report { DstId = repeater.State.DstId, SrcId = repeater.State.SrcId, Peer = repeater.CallSign, Mode = Common.DigitalMode.P25, Type = Common.Api.Type.CALL_START, DateTime = DateTime.Now });
                        _reporter.Send(0, 0, string.Empty, DigitalMode.P25, Common.Api.Type.CONNECTION, PreparePeersListForReport(_peers));

                        _logger.Information($"P25: NET transmssion, srcId: {repeater.State.SrcId}, dstId: {repeater.State.DstId}, Peer: {repeater.CallSign.Trim()}");
                    }
                    break;

                case Opcode.NET_TERM:
                    if (repeater == null) return;

                    if (repeater.State.SrcId <= 0 || repeater.State.DstId <= 0)
                        return;

                    _reporter.Send(new Report { DstId = repeater.State.DstId, SrcId = repeater.State.SrcId, Peer = repeater.CallSign, Mode = Common.DigitalMode.P25, Type = Common.Api.Type.CALL_END, DateTime = DateTime.Now });

                    _logger.Information($"P25: NET end of transmission, srcId: {repeater.State.SrcId}, dstId: {repeater.State.DstId}, Peer: {repeater.CallSign.Trim()}");
                    repeater.State.Reset();

                    _reporter.Send(0, 0, string.Empty, DigitalMode.P25, Common.Api.Type.CONNECTION, PreparePeersListForReport(_peers));
                    break;

                default:
                    if (repeater != null)
                    {
                        if (_acl.CheckCallsignAcl(repeater.CallSign.Trim()) || !_config.Acl)
                        {
                            repeater.Refresh();
                            Console.WriteLine("send");
                            RelayToAllRepeaters(buffer, senderAddress);
                        } else
                        {
                            Console.WriteLine("ACL: " + _config.Acl);
                            Console.WriteLine("CHECK: " + _acl.CheckCallsignAcl(repeater.CallSign.Trim()));
                        }
                    }
                    break;
            }
        }

        private void RelayToAllRepeaters(byte[] buffer, IPEndPoint senderAddress)
        {
            foreach (var repeater in _peers)
            {
                if (!repeater.IsSameAddress(senderAddress))
                {
                    _networkManager.SendData(buffer, repeater.Address);
                }
            }
        }

        private void CleanUpRepeaters()
        {
            foreach (var repeater in _peers)
            {
                if (repeater.IsExpired())
                {
                    _logger.Warning($"P25: Removing peer {repeater.CallSign.Trim()} due to inactivity.");
                    _peers.Remove(repeater);

                    _reporter.Send(0, 0, string.Empty, DigitalMode.P25, Common.Api.Type.CONNECTION, PreparePeersListForReport(_peers));
                    break;
                }
            }
        }

        private string PreparePeersListForReport(List<P25Peer> peers)
        {
            var peersInfo = peers.Select(peer => new
            {
                CallSign = peer.CallSign.Trim(),
                Address = peer.Address.ToString(),
                TransmissionState = new
                {
                    Seen64 = peer.State.Seen64,
                    Seen65 = peer.State.Seen65,
                    Displayed = peer.State.Displayed,
                    Lcf = peer.State.Lcf,
                    SrcId = peer.State.SrcId,
                    DstId = peer.State.DstId
                }
            });

            return JsonConvert.SerializeObject(peersInfo, Formatting.Indented);
        }

        private P25Peer FindRepeater(IPEndPoint address)
        {
            return _peers.Find(r => r.IsSameAddress(address));
        }
    }
}
