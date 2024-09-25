/*
* MMDVM_Reflector - M17Reflector
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
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using YamlDotNet.Core.Tokens;

namespace M17_Reflector
{
    public class M17Reflector
    {
        public static string version = "01.00.00";
        private const string M17CHARACTERS = " ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-/.";

        private readonly ConcurrentDictionary<string, Peer> _peers = new ConcurrentDictionary<string, Peer>();
        private readonly Network _protocol;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private CancellationTokenSource _cancellationTokenSource;

        private Config _config;
        private CallsignAcl _callsignAcl;
        private Reporter _reporter;
        private readonly ILogger _logger;

        public M17Reflector(Config config, CallsignAcl callsignAcl, Reporter reporter, ILogger logger)
        {
            _config = config;
            _callsignAcl = callsignAcl;
            _reporter = reporter;
            _logger = logger;

            _protocol = new Network(config.NetworkPort, _config.NetworkDebug, _logger);

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Run()
        {
            _logger.Information("Starting M17Reflector");
            _logger.Information($"    Port: {_config.NetworkPort}");
            _logger.Information($"    Reflector: M17-{_config.Reflector}");
            _logger.Information($"    Debug: {_config.NetworkDebug}");

            if (!_protocol.OpenConnection())
            {
                _logger.Error("Failed to initialize protocol.");
                return;
            } else
            {
                _logger.Information($"M17Reflector version: {version} started.\n");
            }

            Task.Factory.StartNew(() => MainLoop(_cts.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            //_logger.Information("Stopping M17 Reflector...");
            _cancellationTokenSource.Cancel();
            _cts.Cancel();
            _protocol.CloseConnection();
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
                Mode = "M17",
                Status = status,
                Port = _config.NetworkPort,
                ConnectedPeers = _peers.Count,
                Acl = null
            };
        }

        public bool Disconnect(string callsign)
        {
            _logger.Information($"M17: Attempting to disconnect callsign {callsign}");

            var peerToRemove = _peers.Values.FirstOrDefault(p => p.Callsign.Equals(callsign, StringComparison.OrdinalIgnoreCase));

            if (peerToRemove != null)
            {
                _protocol.SendData(CreateNackPacket(), peerToRemove.Address);

                if (_peers.TryRemove(peerToRemove.Address.ToString(), out var removedPeer))
                {
                    _reporter.Send(new Report { Mode = DigitalMode.M17, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_peers) });

                    _logger.Information($"M17: Successfully disconnected {removedPeer.Callsign} from {removedPeer.Address}");

                    return true;
                }
                else
                {
                    _logger.Error($"M17: Failed to remove peer {callsign} from peers list.");
                    return false;
                }
            }
            else
            {
                _logger.Warning($"M17: Callsign {callsign} not found among connected peers.");
                return false;
            }
        }

        public bool Block(string callsign)
        {
            Console.WriteLine($"M17: Block {callsign}");
            return false;
        }

        public bool UnBlock(string callsign)
        {
            var entry = _callsignAcl.Entries.Find(e => e.Callsign == callsign);

            if (entry != null)
            {
                entry.Allowed = true;
                _logger.Information($"M17: Unblocked callsign {callsign}");
            }
            else
            {
                _logger.Warning($"M17: Callsign {callsign} not found in ACL. Nothing to unblock.");
                return false;
            }

            try
            {
                _callsignAcl.Save();
                _logger.Information("M17: ACL updated and saved successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"M17: Failed to save ACL: {ex.Message}");
                return false;
            }
        }

        private async Task MainLoop(CancellationToken token)
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var (packet, ip) = _protocol.ReceiveData();
                if (packet != null)
                {
                    HandlePacket(packet, ip);
                }

                await Task.Delay(10, token);
            }
        }

        private void HandlePacket(byte[] packet, IPEndPoint ip)
        {
            byte[] header = new byte[4];

            Array.Copy(packet, header, 4);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(header);

            int opcode = BitConverter.ToInt32(header, 0);

            switch ((Opcode)opcode)
            {
                case Opcode.NET_CONN:
                    HandleConnect(packet, ip);
                    break;
                case Opcode.NET_DISC:
                    HandleDisconnect(packet, ip);
                    break;
                case Opcode.NET_KA:
                    HandleKeepAlive(packet, ip);
                    break;
                case Opcode.NET_VOICE:
                    HandleVoiceData(packet, ip);
                    break;
                default:
                    _logger.Warning("Unknown packet type received");
                    break;
            }
        }

        private void HandleConnect(byte[] packet, IPEndPoint ip)
        {
            bool moduleAuthorized = true;
            bool userAuthorized = true;

            byte[] srcCallsign = new byte[6];
            byte[] dstCallsign = new byte[6];

            Array.Copy(packet, 4, srcCallsign, 0, 6);
            Callsign srcCs = new Callsign(srcCallsign);
            string srcId = srcCs.GetCS();

            if (packet.Length >= 16)
            {
                Array.Copy(packet, 10, dstCallsign, 0, 6);
                Callsign dstCs = new Callsign(dstCallsign);
                string dstId = dstCs.GetCS();

                _logger.Information($"M17: NET_CONN source: {srcId.Substring(0, 6)}, destination: {dstId}, module: {Encoding.ASCII.GetString(packet, 16, 1)}, IP: {ip}");
            }
            else
            {
                _logger.Information($"M17: NET_CONN source: {srcId.Substring(0, 6)}, module: {Encoding.ASCII.GetString(packet, 10, 1)}, IP: {ip}");
            }

            moduleAuthorized = _config.CheckModule(Encoding.ASCII.GetString(packet, 10, 1));
            userAuthorized = _callsignAcl.CheckCallsignAcl(srcId.Substring(0, 6));

            if (!_config.Acl || (moduleAuthorized && userAuthorized))
            {
                var peer = new Peer(ip, _logger);
                peer.Callsign = srcId.Substring(0, 6);
                peer.Module = Encoding.ASCII.GetString(packet, 10, 1);

                _peers.TryAdd(ip.ToString(), peer);
                _protocol.SendData(CreateAckPacket(), ip);

                _reporter.Send(new Report {Mode = DigitalMode.M17, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_peers) });
                _logger.Information($"M17: Added peer: {peer.Address}");
            }
            else
            {
                if (!moduleAuthorized)
                    _logger.Warning($"M17: NET_NACK address: {ip}, Reason: Module not enabled");

                if (!userAuthorized)
                    _logger.Warning($"M17: NET_NACK address: {ip}, Reason: ACL Rejction");

                _protocol.SendData(CreateNackPacket(), ip);
            }
        }

        private void HandleDisconnect(byte[] packet, IPEndPoint ip)
        {
            _logger.Information($"M17: NET_DISC from {ip}");
            if (_peers.TryRemove(ip.ToString(), out var peer))
            {
                _reporter.Send(new Report { Mode = DigitalMode.M17, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_peers) });
                _logger.Information($"M17: Removed peer: {peer.Address}");
            } else
            {
                _logger.Warning($"M17: NET_NACK address: {ip}, Reason: Peer not connected");
                _protocol.SendData(CreateNackPacket(), ip);
            }
        }

        private void HandleKeepAlive(byte[] packet, IPEndPoint ip)
        {
            //Console.WriteLine($"M17: PONG packet from {ip}");
            if (_peers.TryGetValue(ip.ToString(), out var peer))
            {
                peer.Refresh();

                _protocol.SendData(CreatePingPacket(), ip);
                //Console.WriteLine($"Peer {peer.Address} PING.");
            } else
            {
                _logger.Warning($"M17: NET_NACK address: {ip}, Reason: Peer not connected");
                _protocol.SendData(CreateNackPacket(), ip);
            }
        }

        private void HandleVoiceData(byte[] packet, IPEndPoint ip)
        {
            if (_peers.TryGetValue(ip.ToString(), out var peer))
            {
                byte[] callsign = new byte[6];
                byte[] streamid = new byte[2];

                Array.Copy(packet, 12, callsign, 0, 6);
                Callsign sourceCallsign = new Callsign(callsign);
                string srcid = sourceCallsign.GetCS();

                Array.Copy(packet, 6, callsign, 0, 6);
                Callsign destinationCallsign = new Callsign(callsign);
                string dstid = destinationCallsign.GetCS();

                Array.Copy(packet, 4, streamid, 0, 2);

                string reflector = dstid.Substring(4, 3);
                string module = dstid.Substring(8, 1);

                if (!peer.IsTransmitting)
                {
                    _logger.Information($"M17: Voice transmission, streamid: {BitConverter.ToString(streamid).Replace("-", string.Empty)} callsign: {srcid.Substring(0, 6)}, destination: {dstid}, from {ip}");
                    _reporter.Send(0, 0, srcid.Substring(0, 6), DigitalMode.M17, Common.Api.Type.CALL_START, string.Empty);
                    _reporter.Send(new Report { Mode = DigitalMode.M17, Type = Common.Api.Type.CONNECTION, Extra = PreparePeersListForReport(_peers) });
                }

                peer.StartTransmission(streamid);

                if (reflector != _config.Reflector)
                    _protocol.SendData(CreateNackPacket(), ip);

                BroadCastPacket(packet, ip, module);
            }
            else
            {
                _logger.Warning($"M17: NET_NACK address: {ip}, Reason: Peer not connected");
                _protocol.SendData(CreateNackPacket(), ip);
            }
        }

        private void BroadCastPacket(byte[] packet, IPEndPoint me = null, string module = "")
        {
            using (UdpClient udpClient = new UdpClient())
            {
                foreach (var peer in _peers)
                {
                    try
                    {
                        Peer m17Peer = peer.Value;

                        if (m17Peer.Module != module)
                        {
                            //Console.WriteLine("Not sending between modules");
                            continue;
                        }

                        IPEndPoint endpoint = m17Peer.Address;

                        if (me != null && endpoint.Address.Equals(me.Address) && endpoint.Port == me.Port)
                        {
                            continue;
                        }

                        if (endpoint != null)
                        {
                            udpClient.Send(packet, packet.Length, endpoint);
                        }
                        else
                        {
                            _logger.Error("Invalid endpoint for peer.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error broadcasting to peer: {ex.Message}");
                    }
                }
            }
        }

        private string PreparePeersListForReport(ConcurrentDictionary<string, Peer> peers)
        {
            var peersInfo = peers.Select(peer => new
            {
                CallSign = peer.Value.Callsign.Trim(),
                Module = peer.Value.Module,
                Address = peer.Key.ToString(),
                Transmitting = peer.Value.IsTransmitting
            });

            return JsonConvert.SerializeObject(peersInfo, Formatting.Indented);
        }

        private byte[] CreateAckPacket()
        {
            return Encoding.ASCII.GetBytes("ACKN");
        }

        private byte[] CreateNackPacket()
        {
            return Encoding.ASCII.GetBytes("NACK");
        }

        private byte[] CreatePingPacket()
        {
            return Encoding.ASCII.GetBytes("PING");
        }
    }
}
