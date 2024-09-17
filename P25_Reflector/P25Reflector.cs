using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

#nullable disable

namespace P25_Reflector
{
    public class P25Reflector
    {
        public static string version = "01.00.00";

        private Config _config;
        private List<P25Peer> _peers;
        private NetworkManager _networkManager;
        private CancellationTokenSource _cancellationTokenSource;

        public P25Reflector(Config config)
        {
            _config = config;
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

            Console.WriteLine($"P25Reflector version: {version} started.");

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
                    if (!repeater.State.Seen64)
                    {
                        repeater.State.Lcf = buffer[1];
                        repeater.State.Seen64 = true;
                    }
                    break;

                case 0x65:
                    if (!repeater.State.Seen65)
                    {
                        repeater.State.DstId = (uint)((buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
                        repeater.State.Seen65 = true;
                    }
                    break;

                case 0x66:
                    if (repeater.State.Seen64 && repeater.State.Seen65 && !repeater.State.Displayed)
                    {
                        repeater.State.SrcId = (uint)((buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
                        repeater.State.Displayed = true;

                        Console.WriteLine($"P25: NET transmssion, srcId: {repeater.State.SrcId}, dstId: {repeater.State.DstId}, Client: {repeater.CallSign.Trim()}");
                    }
                    break;

                case Opcode.NET_TERM:
                    Console.WriteLine($"P25: NET end of transmission, srcId: {repeater.State.SrcId}, dstId: {repeater.State.DstId}, Client: {repeater.CallSign.Trim()}");
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
