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

using System.Net;
using System.Text.Json;
using Common.Api.REST.Routes;

namespace Common.Api.REST
{
    internal class ReflectorStatusRoute : IRouteHandler
    {
        private readonly IReflectorContext _reflectorContext;

        public ReflectorStatusRoute(IReflectorContext reflectorContext)
        {
            _reflectorContext = reflectorContext;
        }

        public bool RequiresAuth => true;
        public string Path => "/reflector/";

        public void Handle(HttpListenerContext context, string token)
        {
            var response = context.Response;
            var request = context.Request;

            var segments = request.Url.AbsolutePath.Split('/');

            if (segments.Length == 3 && segments[2] == "status")
            {
                HandleAllModeStatus(response);
                return;
            } else if (segments.Length < 4 || segments[1] != "reflector" || segments[3] != "status")
            {
                SendResponse(response, "Invalid path", HttpStatusCode.NotFound);
                return;
            }

            string reflectorType = segments[2];

            if (request.HttpMethod == "GET")
            {
                HandleGetRequest(response, reflectorType);
            }
            else
            {
                SendResponse(response, "Unsupported method", HttpStatusCode.MethodNotAllowed);
            }
        }

        private void HandleGetRequest(HttpListenerResponse response, string reflectorType)
        {
            var status = _reflectorContext.GetReflectorStatus(reflectorType);

            if (status != null)
            {
                var responseText = JsonSerializer.Serialize(new { Status = status });
                SendResponse(response, responseText);
            }
            else
            {
                SendResponse(response, $"Reflector type '{reflectorType}' not found", HttpStatusCode.NotFound);
            }
        }

        private void HandleAllModeStatus(HttpListenerResponse response)
        {
            var modes = new List<string> { "P25", "NXDN", "YSF", "M17" };
            var statuses = new List<object>();

            foreach (var mode in modes)
            {
                var status = _reflectorContext.GetReflectorStatus(mode);
                if (status != null)
                {
                    statuses.Add(new { Mode = mode, Status = status });
                }
                else
                {
                    statuses.Add(new { Mode = mode, Status = "Not Found" });
                }
            }

            var responseText = JsonSerializer.Serialize(statuses);
            SendResponse(response, responseText);
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
