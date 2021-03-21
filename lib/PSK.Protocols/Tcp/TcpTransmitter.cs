using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PSK.Core;
using System.Net.Sockets;
using System.Text;

namespace PSK.Protocols.Tcp
{
    public class TcpTransmitter : ITransmitter
    {
        private readonly TcpClient _client;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        //TODO: IOptions needed
        //IConfiguration configuration, ILogger<TcpTransmitter> logger, 
        public TcpTransmitter(TcpClient client)
        {
            _client = client;
            //_configuration = configuration;
            //_logger = logger;
        }

        public void Transmit(string data)
        {
            if(!_client.Connected)
            {
                return;
            }

            var stream = _client.GetStream();

            byte[] response = Encoding.ASCII.GetBytes($"{data}");

            stream.Write(response, 0, response.Length);
        }

        public void Dispose()
        {
            _client.GetStream().Close();
            _client.Close();
            _client.Dispose();
        }
    }
}
