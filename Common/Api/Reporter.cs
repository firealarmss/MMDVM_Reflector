using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace Common.Api
{
    public class Reporter
    {
        private string _ip;
        private int _port;
        private bool _enabled;

        private readonly HttpClient _httpClient;

        public Reporter()
        {
            _ip = "127.0.0.1";
            _port = 3000;
            _enabled = false;

            if (_enabled)
            {
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri($"http://{_ip}:{_port}")
                };

                Console.WriteLine($"Started Reporter at http://{_ip}:{_port}\n");
            }
        }

        public Reporter(string ip, int port, bool enabled)
        {
            _ip = ip;
            _port = port;
            _enabled = enabled;

            if (_enabled)
            {
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri($"http://{_ip}:{_port}")
                };

                Console.WriteLine($"Started Reporter at http://{_ip}:{_port}\n");
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
                    Console.WriteLine("REPORTER: Report sent");
#endif
                }
                else
                {
                    Console.WriteLine($"REPORTER: Failed to send: {response.StatusCode}");
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

            var utcNow = DateTime.UtcNow;

            TimeZoneInfo cdtZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            DateTime cdtTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, cdtZone);

            Report reportData = new Report();
            reportData.SrcId = srcId;
            reportData.DstId = dstId;
            reportData.Peer = peer;
            reportData.Mode = mode;
            reportData.Type = type;
            reportData.DateTime = cdtTime;
            reportData.Extra = extra;

            Task.Run(() => SendReportAsync(reportData));
        }
    }
}
