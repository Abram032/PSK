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
            while(!cancellationToken.IsCancellationRequested)
            {
                byte[] bytes = new byte[256];
                TcpClient client = listener.AcceptTcpClient();
                string data = null;
                int len, nl;
                NetworkStream stream = client.GetStream();
                while ((len = stream.Read(bytes, 0, bytes.Length)) > 0)
                {
                    data += Encoding.ASCII.GetString(bytes, 0, len);
                    while ((nl = data.IndexOf('\n')) != -1)
                    {
                        string line = data.Substring(0, nl + 1);
                        data = data.Substring(nl + 1);
                        byte[] msg = Encoding.ASCII.GetBytes("YOLO");
                        stream.Write(msg, 0, msg.Length);
                    }
                }

                Console.WriteLine(data);
                client.Close();
            }
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
