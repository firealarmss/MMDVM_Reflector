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

using Newtonsoft.Json;
using Serilog;
using System.Text;

namespace Common.Api
{
    public class Reporter
    {
        private string _ip;
        private int _port;
        private bool _enabled;
        private ILogger _logger;

        private readonly HttpClient _httpClient;

        public Reporter() { /* sub */ }

        public Reporter(string ip, int port, bool enabled, ILogger logger)
        {
            _ip = ip;
            _port = port;
            _enabled = enabled;
            _logger = logger;

            if (_enabled)
            {
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri($"http://{_ip}:{_port}")
                };

                _logger.Information($"Started Reporter at http://{_ip}:{_port}");
            }
        }

        public async Task SendReportAsync(object reportData)
        {
            if (!_enabled)
                return;

            var json = JsonConvert.SerializeObject(reportData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("/", content);
                if (response.IsSuccessStatusCode)
                {
#if DEBUG
                    //Console.WriteLine("REPORTER: Report sent");
#endif
                }
                else
                {
                    _logger.Error($"REPORTER: Failed to send: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"REPORTER: Failed to send: {ex.Message}");
            }
        }

        public void Send(Report report)
        {
            if (!_enabled) return;

            Task.Run(() => SendReportAsync(report));
        }

        public void Send(uint srcId, uint dstId, string peer, DigitalMode mode, Type type, string extra)
        {
            if (!_enabled) return;

            Report reportData = new Report();
            reportData.SrcId = srcId;
            reportData.DstId = dstId;
            reportData.Peer = peer;
            reportData.Mode = mode;
            reportData.Type = type;
            reportData.DateTime = DateTime.Now;
            reportData.Extra = extra;

            Task.Run(() => SendReportAsync(reportData));
        }
    }
}
