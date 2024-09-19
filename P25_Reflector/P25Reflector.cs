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

using Common.Api;
using System.Net;

#nullable disable

namespace P25_Reflector
{
    public class P25Reflector
    {
        public static string version = "01.00.00";

        private Config _config;
        private Reporter _reporter;

        private List<P25Peer> _peers;
        private NetworkManager _networkManager;

        private CancellationTokenSource _cancellationTokenSource;

        public P25Reflector(Config config, Reporter reporter)
        {
            _config = config;
            _reporter = reporter;
            _peers = new List<P25Peer>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Run()
        {
            Console.WriteLine("Starting P25Reflector");
            Console.WriteLine($"    Port: {_config.NetworkPort}");
            Console.WriteLine($"    Debug: {_config.NetworkDebug}");

            _networkManager = new NetworkManager(_config.NetworkPort, _config.NetworkDebug);
            if (!_networkManager.OpenConnection())
            {
                Console.WriteLine("P25Reflector network open failed");
                return;
            }

            Console.WriteLine($"P25Reflector version: {version} started.\n");

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
            P25Peer repeater = FindRepeater(senderAddress);
            byte opcode = buffer[0];

            switch (opcode)
            {
                case Opcode.NET_POLL:
                    if (repeater == null)
                    {
                        repeater = new P25Peer(senderAddress, buffer);
                        _peers.Add(repeater);
                        Console.WriteLine($"P25: New connection: {repeater.CallSign.Trim()}; Address: {senderAddress}");
                    }
                    else
                    {
                        repeater.Refresh();
                    }

                    _networkManager.SendData(buffer, senderAddress);
                    break;

                case Opcode.NET_UNLINK:
                    if (repeater != null)
                    {
                        Console.WriteLine($"P25: Removing {repeater.CallSign.Trim()}; NET_UNLINK received.");
                        _peers.Remove(repeater);
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

                        _reporter.Send(new Report { DstId = repeater.State.DstId, SrcId = repeater.State.SrcId, Mode = Common.DigitalMode.P25, Type = Common.Api.Type.CALL_START });

                        Console.WriteLine($"P25: NET transmssion, srcId: {repeater.State.SrcId}, dstId: {repeater.State.DstId}, Peer: {repeater.CallSign.Trim()}");
                    }
                    break;

                case Opcode.NET_TERM:
                    if (repeater == null) return;

                    _reporter.Send(new Report { DstId = repeater.State.DstId, SrcId = repeater.State.SrcId, Mode = Common.DigitalMode.P25, Type = Common.Api.Type.CALL_END });

                    Console.WriteLine($"P25: NET end of transmission, srcId: {repeater.State.SrcId}, dstId: {repeater.State.DstId}, Peer: {repeater.CallSign.Trim()}");
                    repeater.State.Reset();
                    break;

                default:
                    if (repeater != null)
                    {
                        repeater.Refresh();
                        RelayToAllRepeaters(buffer, senderAddress);
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
                // Console.WriteLine(repeater.Address);
                if (repeater.IsExpired())
                {
                    Console.WriteLine($"P25: Removing repeater {repeater.CallSign.Trim()} due to inactivity.");
                    _peers.Remove(repeater);
                    break;
                }
            }
        }

        private P25Peer FindRepeater(IPEndPoint address)
        {
            return _peers.Find(r => r.IsSameAddress(address));
        }
    }
}
