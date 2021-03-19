using System;
using System.Net.Sockets;
using System.Text;

namespace PSK.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string server = "localhost";
            var client = new TcpClient("localhost", 21020);
            var stream = client.GetStream();

            string message = "ping TEST\n";
            byte[] data = Encoding.ASCII.GetBytes(message);

            stream.Write(data, 0, data.Length);
            Console.Write("Wysłane: {0}", message);

            byte[] response = new byte[256];
            string responseStr = string.Empty;
            int bytes;
            do
            {
                bytes = stream.Read(response, 0, response.Length);
                responseStr += Encoding.ASCII.GetString(response, 0, bytes);
            }
            while (stream.DataAvailable);

            Console.WriteLine("Pobrane: {0}", responseStr);
            Console.Read();
        }
    }
}
