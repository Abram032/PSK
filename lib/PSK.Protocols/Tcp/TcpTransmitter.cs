using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PSK.Core;
using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Protocols.Tcp
{
    public interface ITcpTransmitter : ITransmitter { }
    public class TcpTransmitter : ITcpTransmitter
    {
        private TcpClient _client;
        private readonly ILogger _logger;
        public TcpTransmitter(ILogger<TcpTransmitter> logger)
        {
            _logger = logger;
        }

        public void Start(object client)
        {
            _client = client as TcpClient;
            if(_client == null)
            {
                _logger.LogWarning($"{nameof(TcpTransmitter)} received invalid client type");
                return;
            }
        }

        public async Task Transmit(string data)
        {
            if (!_client.Connected)
            {
                _logger.LogWarning($"Unable to send data, client already disconnected");
                return;
            }

            ReadOnlyMemory<byte> response = Encoding.ASCII.GetBytes($"{data}");
            await _client.GetStream().WriteAsync(response);
        }

        public void Stop()
        {
            _client.GetStream().Close();
            _client.Close();
        }

        public void Dispose()
        {
            Stop();
            _client.Dispose();
        }
    }
}
