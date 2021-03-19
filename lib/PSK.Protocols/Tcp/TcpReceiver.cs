using PSK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSK.Protocols.Tcp
{
    public class TcpReceiver : IReceiver, IDisposable
    {
        private TcpListener listener;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;
        private bool isListening = false;
        public TcpReceiver()
        {
            var tcpPort = 21020;
            listener = new TcpListener(IPAddress.Any, tcpPort);
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            Stop();
            cancellationTokenSource.Dispose();
        }

        public async Task Receive()
        {
            var sb = new StringBuilder();
            var client = listener.AcceptTcpClient();
            var stream = client.GetStream();
            var buffer = new byte[4000];
            while(!cancellationToken.IsCancellationRequested)
            {
                var dataLength = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                while (dataLength > 0)
                {
                    sb.Append(Encoding.ASCII.GetString(buffer, 0, dataLength));
                    if(sb[sb.Length - 1] == '\n')
                    {
                        sb.Remove(sb.Length - 1, 1); //Removing '/n' from the end
                        var request = sb.ToString().Split(' ');
                        var command = request[0];
                        var data = request[1];

                        byte[] msg = Encoding.ASCII.GetBytes($"Received: {command} {data}");
                        stream.Write(msg, 0, msg.Length);

                        sb.Clear();
                    }
                    dataLength = 0;
                    Array.Clear(buffer, 0, buffer.Length);
                }
            }
            client.Close();
        }

        public void Start()
        {
            listener.Start();
            cancellationToken = cancellationTokenSource.Token;
            Task.Factory.StartNew(() => Receive(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            isListening = true;
        }

        public void Stop()
        {
            if(!isListening)
            {
                return;
            }
            isListening = false;
            cancellationTokenSource.Cancel();
            listener.Stop();
        }
    }
}
