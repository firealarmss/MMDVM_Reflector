using System.Net;

namespace Common.Api.REST.Routes
{
    public interface IRouteHandler
    {
        bool RequiresAuth { get; }
        string Path { get; }
        void Handle(HttpListenerContext context, string token);
    }
}
