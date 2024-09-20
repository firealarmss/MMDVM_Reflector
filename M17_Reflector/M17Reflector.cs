using Common;
using Common.Api;
using Serilog;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace M17_Reflector
{
    public class M17Reflector
    {
        public static string version = "01.00.00";
        private const string M17CHARACTERS = " ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-/.";

        private readonly ConcurrentDictionary<string, Peer> _peers = new ConcurrentDictionary<string, Peer>();
        private readonly Network _protocol;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private Config _config;
        private Reporter _reporter;
        private readonly ILogger _logger;

        public M17Reflector(Config config, Reporter reporter, ILogger logger)
        {
            _config = config;
            _reporter = reporter;
            _logger = logger;

            _protocol = new Network(config.NetworkPort);
        }

        public void Run()
        {
            _logger.Information("Starting M17Reflector");
            _logger.Information($"    Port: {_config.NetworkPort}");
            _logger.Information($"    Reflector: M17-{_config.Reflector}");
            _logger.Information($"    Debug: {_config.NetworkDebug}");

            if (!_protocol.Initialize())
            {
                _logger.Error("Failed to initialize protocol.");
                return;
            } else
            {
                _logger.Information($"M17Reflector version: {version} started.\n");
            }

            Task.Run(() => MainLoop(), _cts.Token);
        }

        public void Stop()
        {
            _logger.Information("Stopping M17 Reflector...");
            _cts.Cancel();
            _protocol.Close();
        }

        private async Task MainLoop()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var (packet, ip) = await _protocol.ReceivePacketAsync();
                if (packet != null)
                {
                    HandlePacket(packet, ip);
                }
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
            bool isAuthorized = true;
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
            if (isAuthorized)
            {
                var peer = new Peer(ip);
                peer.Module = Encoding.ASCII.GetString(packet, 10, 1);

                _peers.TryAdd(ip.ToString(), peer);
                _protocol.SendPacket(CreateAckPacket(), ip);

                _logger.Information($"M17: Added peer: {peer.Address}");
            }
            else
            {
                _logger.Warning($"M17: NET_NACK address: {ip}, Reason: Unauthorized");
                _protocol.SendPacket(CreateNackPacket(), ip);
            }
        }

        private void HandleDisconnect(byte[] packet, IPEndPoint ip)
        {
            _logger.Information($"M17: NET_DISC from {ip}");
            if (_peers.TryRemove(ip.ToString(), out var peer))
            {
                _logger.Information($"M17: Removed peer: {peer.Address}");
            } else
            {
                _logger.Warning($"M17: NET_NACK address: {ip}, Reason: Unauthorized");
                _protocol.SendPacket(CreateNackPacket(), ip);
            }
        }

        private void HandleKeepAlive(byte[] packet, IPEndPoint ip)
        {
            //Console.WriteLine($"M17: PONG packet from {ip}");
            if (_peers.TryGetValue(ip.ToString(), out var peer))
            {
                peer.Refresh();

                _protocol.SendPacket(CreatePingPacket(), ip);
                //Console.WriteLine($"Peer {peer.Address} PING.");
            } else
            {
                _logger.Warning($"M17: NET_NACK address: {ip}, Reason: Unauthorized");
                _protocol.SendPacket(CreateNackPacket(), ip);
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

                peer.StreamId = streamid;

                if (reflector != _config.Reflector)
                    _protocol.SendPacket(CreateNackPacket(), ip);

                BroadCastPacket(packet, ip, module);

                _logger.Information($"M17: Voice transmission, streamid: {BitConverter.ToString(streamid).Replace("-", string.Empty)} callsign: {srcid.Substring(0, 6)}, destination: {dstid}, from {ip}");
            }
            else
            {
                _logger.Warning($"M17: NET_NACK address: {ip}, Reason: Unauthorized");
                _protocol.SendPacket(CreateNackPacket(), ip);
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
