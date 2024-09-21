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
                    "unblock" => _reflectorContext.UnBlockCallsign(reflectorType, callsign),
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