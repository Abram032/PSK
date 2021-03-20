using PSK.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Protocols.Tcp
{
    public class TcpTransmitter : ITransmitter
    {
        private TcpClient _client;
        public TcpTransmitter(TcpClient client)
        {
            _client = client;
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
            _client.Close();
            _client.Dispose();
        }
    }
}
