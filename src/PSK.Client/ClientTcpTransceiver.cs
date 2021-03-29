using Newtonsoft.Json;
using PSK.Client.Enums;
using PSK.Core.Models;
using PSK.Protocols;
using PSK.Protocols.Tcp;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        protected override ValueTask ProcessCommandlessLine(Guid clientId, ReadOnlySequence<byte> line)
        {
            var stringBuilder = new StringBuilder();
            foreach (var segment in line)
            {
                stringBuilder.Append(Encoding.ASCII.GetString(segment.Span));
            }
            if(!TryDecodeAndParse(stringBuilder.ToString(), out var message))
            {
                message = new Message()
                { 
                    Succeded = true,
                    Data = stringBuilder.ToString()
                };
            }
            message.ClientId = clientId;
            return ProcessMessage(message);
        }

        private bool TryDecodeAndParse(string data, out Message message)
        {
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(data));
                message = JsonConvert.DeserializeObject<Message>(json);
                return true;
            }
            catch
            {
                message = null;
                return false;
            }
        }

        protected override async ValueTask ProcessMessage(Message message)
        {
            stopwatch.Stop();
            var time = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            Console.WriteLine();
            if (!message.Succeded)
            {
                Console.WriteLine($"[{Protocol.Tcp}] Server ({time}ms) |> {message.Error}");
                return;
            }
            switch (message.Service)
            {
                case Service.File:
                    if(message.Headers == null || !message.Headers.ContainsKey("Filename"))
                    {
                        break;
                    }
                    var filename = message.Headers["Filename"];
                    var downloadPath = "download";
                    var basePath = Path.Combine(Environment.CurrentDirectory, downloadPath);
                    Directory.CreateDirectory(basePath);
                    var filePath = Path.Combine(basePath, filename);
                    var bytes = Convert.FromBase64String(message.Data);
                    await File.WriteAllBytesAsync(filePath, bytes);
                    Console.WriteLine($"[{Protocol.Tcp}] Server ({time}ms) |> File {filename} downloaded!");
                    return;
            }

            Console.WriteLine($"[{Protocol.Tcp}] Server ({time}ms) |> {message.Data}");
            return;
        }
    }
}
