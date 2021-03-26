using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Client
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            string server = "localhost";
            var client = new TcpClient(server, 21021);
            var stream = client.GetStream();

            string message = "ping Test\n";
            byte[] data = Encoding.ASCII.GetBytes(message);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var sentAt = DateTime.Now;
            int i = 0;
            stream.Write(data, 0, data.Length);
            while(i++ < 1005000000)
            {
                stream.Write(data, 0, data.Length);
                //Console.WriteLine($"Sent: {message}");
            }

            await Task.Delay(15000);

            byte[] response = new byte[256];
            string responseStr = string.Empty;
            int bytes;
            do
            {
                bytes = stream.Read(response, 0, response.Length);
                responseStr += Encoding.ASCII.GetString(response, 0, bytes);
            }
            while (stream.DataAvailable);
            stopwatch.Stop();

            Console.WriteLine($"Received: {responseStr}");
            Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds} ms");
            await Task.Delay(-1);
            client.GetStream().Close();
            client.Close();
            //client.Client.DisconnectAsync(new SocketAsyncEventArgs());
            //client.Dispose();
        }
    }
}
