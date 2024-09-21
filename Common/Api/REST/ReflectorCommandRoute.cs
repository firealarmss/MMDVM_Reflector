using Common.Api;
using System.Net;
using System.Text.Json;

namespace Common.Api.REST.Routes
{
    public class ReflectorCommandRoute : IRouteHandler
    {
        private readonly IReflectorContext _reflectorContext;

        public ReflectorCommandRoute(IReflectorContext reflectorContext)
        {
            _reflectorContext = reflectorContext;
        }

        public bool RequiresAuth => true;
        public string Path => "/reflector/command";

        public void Handle(HttpListenerContext context, string token)
        {
            var response = context.Response;
            using var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            string body = reader.ReadToEnd();
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

            if (data != null && data.TryGetValue("action", out string action) &&
                data.TryGetValue("mode", out string reflectorType) &&
                data.TryGetValue("callsign", out string callsign))
            {
                bool result = action.ToLower() switch
                {
                    "disconnect" => _reflectorContext.DisconnectCallsign(reflectorType, callsign),
                    "block" => _reflectorContext.BlockCallsign(reflectorType, callsign),
                    _ => false
                };

                var responseText = JsonSerializer.Serialize(new { Success = result });
                SendResponse(response, responseText);
            }
            else
            {
                var responseText = JsonSerializer.Serialize(new { Message = "Invalid request data" });
                SendResponse(response, responseText, HttpStatusCode.BadRequest);
            }
        }

        private void SendResponse(HttpListenerResponse response, string responseText, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            response.StatusCode = (int)statusCode;
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseText);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}