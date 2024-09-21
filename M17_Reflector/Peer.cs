using Serilog;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace M17_Reflector
{
    public class Peer
    {
        public IPEndPoint Address { get; }
        public string Module { get; set; }
        public bool IsTransmitting { get; private set; }
        public byte[] StreamId { get; private set; }

        private ILogger _logger { get; set; }

        private DateTime _lastActive;
        private CancellationTokenSource _transmissionCts;

        public Peer(IPEndPoint address, ILogger logger)
        {
            Address = address;
            _logger = logger;

            _lastActive = DateTime.Now;
            _transmissionCts = new CancellationTokenSource();
            IsTransmitting = false;
        }

        public void Refresh()
        {
            _lastActive = DateTime.Now;
        }

        public bool IsExpired() => (DateTime.Now - _lastActive).TotalSeconds > 30;

        public void StartTransmission(byte[] streamId)
        {
            if (IsTransmitting && StreamId != null && AreStreamIdsEqual(StreamId, streamId))
            {
                RefreshTransmissionTimeout();
                return;
            }

            IsTransmitting = true;
            StreamId = streamId;

            RefreshTransmissionTimeout();
        }

        private void StopTransmission()
        {
            if (IsTransmitting)
            {
                _logger.Information($"M17: Transmission ended for peer {Address} with StreamId: {BitConverter.ToString(StreamId).Replace("-", string.Empty)}");
            }

            IsTransmitting = false;
            StreamId = null;
        }

        private void RefreshTransmissionTimeout()
        {
            _transmissionCts.Cancel();

            _transmissionCts = new CancellationTokenSource();
            var token = _transmissionCts.Token;

            Task.Delay(1000, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    StopTransmission();
                }
            }, TaskScheduler.Default);
        }

        private bool AreStreamIdsEqual(byte[] id1, byte[] id2)
        {
            if (id1.Length != id2.Length) return false;
            for (int i = 0; i < id1.Length; i++)
            {
                if (id1[i] != id2[i]) return false;
            }
            return true;
        }
    }
}
