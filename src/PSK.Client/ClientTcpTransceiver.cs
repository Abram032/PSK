using PSK.Client.Enums;
using PSK.Protocols;
using PSK.Protocols.Tcp;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSK.Client
{
    public class ClientTcpTransceiver : BaseTransceiver, ITcpTransceiver
    {
        private Stopwatch stopwatch;
        private TcpClient client;

        public ClientTcpTransceiver() : base()
        {
            stopwatch = new Stopwatch();
        }
        public override void Start(object client = null)
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            if(client == null)
            {
                this.client = new TcpClient("localhost", 21021);
            }

            var reader = PipeReader.Create(this.client.GetStream());
            Task.Factory.StartNew(() => Receive(reader), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            Active = true;
        }

        public override void Stop()
        {
            Active = false;

            if (client.Connected)
            {
                client.GetStream().Close();
            }
            client.Close();
            cancellationTokenSource.Cancel();
        }

        public override void Dispose()
        {
            client.Dispose();
            cancellationTokenSource.Dispose();
        }

        public override async Task Transmit(string data)
        {
            try
            {
                ReadOnlyMemory<byte> response =
                    data.LastOrDefault().Equals('\n') ?
                    Encoding.ASCII.GetBytes($"{data}") : Encoding.ASCII.GetBytes($"{data}\n");
                stopwatch.Start();
                await client.GetStream().WriteAsync(response);
            }
            catch (Exception e)
            {
                LogException(e.Message);
                Stop();
                Dispose();
            }
        }

        protected override async Task ProcessCommandlessLine(Guid clientId, ReadOnlySequence<byte> line)
        {
            var stringBuilder = new StringBuilder();
            foreach (var segment in line)
            {
                stringBuilder.Append(Encoding.ASCII.GetString(segment.Span));
            }

            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine($"[{Protocol.Tcp}] Server ({stopwatch.ElapsedMilliseconds}ms) |> {stringBuilder}");
            stopwatch.Reset();
        }

        protected override async ValueTask ProcessMessage(Guid clientId, string command, string data)
        {
            stopwatch.Stop();
            var service = Enum.Parse(typeof(Service), command, true);
            switch(service)
            {
                case Service.Configure or Service.Chat:
                    Console.WriteLine();
                    Console.WriteLine($"[{Protocol.Tcp}] Server ({stopwatch.ElapsedMilliseconds}ms) |> {TryParseFromBase64(data)}");
                    return;
            }
            stopwatch.Reset();
        }
        private string TryParseFromBase64(string data)
        {
            var buffer = new Span<byte>(new byte[data.Length]);
            //data.PadRight(data.Length / 4 * 4 + (data.Length % 4 == 0 ? 0 : 4), '=')
            if (!Convert.TryFromBase64String(data, buffer, out _))
            {
                return data;
            }

            return Encoding.UTF8.GetString(buffer);
        }
    }
}
