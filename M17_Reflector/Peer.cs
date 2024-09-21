/*
* MMDVM_Reflector - Common
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
        public string Callsign { get; set; }
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

            Task.Delay(500, token).ContinueWith(t =>
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
