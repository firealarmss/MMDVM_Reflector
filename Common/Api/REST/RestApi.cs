using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Api.REST.Routes;
using Serilog;

namespace Common.Api.REST
{
    public class RestApi
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly string _url;
        private readonly List<IRouteHandler> _routes;
        private readonly string _hashedPassword;
        private readonly Dictionary<string, string> _validTokens = new Dictionary<string, string>();
        private readonly IReflectorContext _reflectorContext;
        private readonly ILogger _logger;

        public RestApi(string password, IReflectorContext reflectorContext, ILogger logger, string url = "http://localhost:8080/")
        {
            _url = url;
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _hashedPassword = PasswordHasher.HashPassword(password);
            _reflectorContext = reflectorContext;
            _logger = logger;

            _routes = new List<IRouteHandler>
            {
                new AuthRoute(_hashedPassword, _validTokens),
                new ReflectorCommandRoute(_reflectorContext)
            };
        }

        public void Start()
        {
            if (!_listener.IsListening)
            {
                _listener.Start();
                _logger.Information($"REST API Server started at {_url}");
                Task.Run(() => HandleIncomingConnections(), _cts.Token);
            }
        }

        public void Stop()
        {
            if (_listener.IsListening)
            {
                _cts.Cancel();
                _listener.Stop();
                _logger.Information("REST API Server stopped.");
            }
        }

        private async Task HandleIncomingConnections()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    HandleRequest(context);
                }
                catch (HttpListenerException ex) when (_cts.Token.IsCancellationRequested)
                {
                    
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error handling incoming request: {ex.Message}");
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string token = request.Headers["Authorization"]?.Replace("Bearer ", "");

            var route = _routes.FirstOrDefault(r => request.Url.AbsolutePath.StartsWith(r.Path, StringComparison.OrdinalIgnoreCase));
            if (route != null)
            {
                if (route.RequiresAuth && !_validTokens.ContainsKey(token))
                {
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    var buffer = System.Text.Encoding.UTF8.GetBytes("Unauthorized");
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                    return;
                }

                route.Handle(context, token);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                var buffer = System.Text.Encoding.UTF8.GetBytes("Route Not Found");
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
        }
    }
}
