using System;
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

            var sentAt = DateTime.Now;
            int i = 0;
            while(i++ < 100)
            {
                stream.Write(data, 0, data.Length);
                Console.WriteLine($"Sent: {message}");
            }          

            byte[] response = new byte[256];
            string responseStr = string.Empty;
            int bytes;
            do
            {
                bytes = stream.Read(response, 0, response.Length);
                responseStr += Encoding.ASCII.GetString(response, 0, bytes);
            }
            while (stream.DataAvailable);
            var receivedAt = DateTime.Now;

            var difference = receivedAt - sentAt;

            Console.WriteLine($"Received: {responseStr}");
            Console.WriteLine($"Time: {difference.Milliseconds} ms");
            client.GetStream().Close();
            client.Close();
            //client.Client.DisconnectAsync(new SocketAsyncEventArgs());
            //client.Dispose();
        }
    }
}
