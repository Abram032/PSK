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
        //TODO: Rebuild and allow to transmit and receive requests, rename to ITcpTransreceiver
        public TcpTransmitter(ILogger<TcpTransmitter> logger)
        {
            _logger = logger;
        }

        public void Start(object client)
        {
            //TODO: If possible move to constructor
            _client = client as TcpClient;
            if(_client == null)
            {
                _logger.LogWarning($"{nameof(TcpTransmitter)} received invalid client type");
                return;
            }
        }

        public async Task Transmit(string data)
        {
            try
            {
                ReadOnlyMemory<byte> response = Encoding.ASCII.GetBytes($"{data}");
                await _client.GetStream().WriteAsync(response);
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
                Dispose();
            }
        }

        public void Stop()
        {
            if(_client.Connected)
            {
                _client.GetStream().Close();
            }
            _client.Close();
        }

        public void Dispose()
        {
            Stop();
            _client.Dispose();
        }
    }
}
