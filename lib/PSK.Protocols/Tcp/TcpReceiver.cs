using Microsoft.Extensions.Configuration;
using PSK.Core;
using PSK.Core.Enums;
using PSK.Core.Models;
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
    public class TcpReceiver : IReceiver
    {
        private TcpListener listener;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;

        public TcpReceiver(IConfiguration configuration)
        {
            var tcpPort = 21020;
            listener = new TcpListener(IPAddress.Any, tcpPort);
            cancellationTokenSource = new CancellationTokenSource();
        }

        public event EventHandler<OnReceivedEventArgs> OnReceived;
        public event EventHandler<OnConnectedEventArgs> OnConnected;
        public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;

        public void Dispose()
        {
            Stop();
            cancellationTokenSource.Dispose();
        }

        public void Listen()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = listener.AcceptTcpClient();
                var clientId = Guid.NewGuid();
                OnConnected?.Invoke(this, new OnConnectedEventArgs
                {
                    ClientId = clientId,
                    Client = client,
                    ClientType = ClientType.TCP
                });

                Task.Factory.StartNew(() => Receive(clientId, client), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        public async Task Receive(Guid clientId, TcpClient client)
        {
            var sb = new StringBuilder();
            var stream = client.GetStream();
            var buffer = new byte[4000];
            while(client.Connected)
            {
                if(!stream.DataAvailable || !stream.CanRead)
                {
                    continue;
                }
                var dataLength = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                while (dataLength > 0)
                {
                    sb.Append(Encoding.ASCII.GetString(buffer, 0, dataLength));
                    if (sb[sb.Length - 1] == '\n')
                    {
                        sb.Remove(sb.Length - 1, 1); //Removing '/n' from the end

                        OnReceived?.Invoke(this, new OnReceivedEventArgs
                        {
                            ClientId = clientId,
                            Data = sb.ToString()
                        });
                        sb.Clear();
                    }
                    dataLength = 0;
                    Array.Clear(buffer, 0, buffer.Length);
                }
            }
            OnDisconnected?.Invoke(this, new OnDisconnectedEventArgs
            {
                ClientId = clientId
            });
        }

        public void Start()
        {
            listener.Start();
            cancellationToken = cancellationTokenSource.Token;
            Task.Factory.StartNew(() => Listen(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            listener.Stop();
        }
    }
}
