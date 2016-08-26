using System;
using System.Net;

namespace PokemonGo.RocketAPI
{
    public class WebProxy : IWebProxy
    {
        private readonly string _hostName;
        private readonly string _port;

        public WebProxy(string hostName, string port)
        {
            _hostName = hostName;
            _port = port;
        }

        public ICredentials Credentials { get; set; }

        public Uri GetProxy(Uri destination)
        {
            return new Uri($"http://{_hostName}:{_port}");
        }

        public bool IsBypassed(Uri host)
        {
            return false;
        }
    }
}