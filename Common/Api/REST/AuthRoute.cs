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

using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace Common.Api.REST.Routes
{
    public class AuthRoute : IRouteHandler
    {
        private readonly string _hashedPassword;
        private readonly Dictionary<string, string> _validTokens;

        public AuthRoute(string hashedPassword, Dictionary<string, string> validTokens)
        {
            _hashedPassword = hashedPassword;
            _validTokens = validTokens;
        }

        public bool RequiresAuth => false;
        public string Path => "/auth";

        public void Handle(HttpListenerContext context, string token)
        {
            var response = context.Response;
            using var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            string body = reader.ReadToEnd();
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

            if (data != null && data.TryGetValue("password", out string password))
            {
                bool isPasswordValid = PasswordHasher.VerifyPassword(password, _hashedPassword);
                //Console.WriteLine("Password Verification Result: " + isPasswordValid);

                if (isPasswordValid)
                {
                    string newToken = GenerateToken();
                    _validTokens[newToken] = newToken;
                    var responseText = JsonSerializer.Serialize(new { Message = "Authenticated", Token = newToken });
                    SendResponse(response, responseText);
                    return;
                }
            }

            //Console.WriteLine("Invalid Credentials Provided");
            var responseTextInvalid = JsonSerializer.Serialize(new { Message = "Invalid Credentials" });
            SendResponse(response, responseTextInvalid, HttpStatusCode.Unauthorized);
        }

        private string GenerateToken()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
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
