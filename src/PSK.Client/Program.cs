using PSK.Client.Enums;
using PSK.Core;
using PSK.Core.Models;
using PSK.Protocols.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PSK.Client
{
    class Program
    {
        public static Protocol? CurrentProtocol { get; set; }
        public static Service? CurrentService { get; set; }
        public static Dictionary<Protocol, ITransceiver> Transceivers { get; private set; }
        public static async Task Main(string[] args)
        {
            Transceivers = new Dictionary<Protocol, ITransceiver>
            {
                { Protocol.Tcp, new ClientTcpTransceiver() }
            };

            Console.WriteLine("Use '/help' for more information.");
            while (true)
            {
                Console.Write($"({CurrentProtocol} | {CurrentService}) > ");

                var input = Console.ReadLine();
                await ParseCommandAsync(input);
            }

            await Task.Delay(-1);
        }

        private static async Task ParseCommandAsync(string input)
        {
            input = input.Trim();

            if(input.Length == 0)
            {
                return;
            }
            else if(!input.StartsWith('/') && CurrentProtocol.HasValue && CurrentService.HasValue)
            {
                if(!Transceivers.TryGetValue(CurrentProtocol.Value, out var transceiver))
                {
                    Console.WriteLine("Transceiver not found!");
                    return;
                }

                if(!transceiver.Active)
                {
                    Console.WriteLine("Activate transceiver before sending a request!");
                    return;
                }
                await SendMessage(transceiver, input);
                return;
            }
            else if (!input.StartsWith('/'))
            {
                Console.WriteLine("Unknown command.");
                return;
            }

            var arguments = input.Split(' ');
            if(!Enum.TryParse(typeof(ClientCommand), arguments.FirstOrDefault().Replace("/", ""), true, out var command))
            {
                Console.WriteLine("Unknown command.");
                return;
            }

            switch((ClientCommand)command)
            {
                case ClientCommand.Use:
                    UseCommand(arguments.Skip(1));
                    break;
                case ClientCommand.Service:
                    ServiceCommand(arguments.Skip(1));
                    break;
                case ClientCommand.Help:
                    HelpCommand(arguments.Skip(1));
                    break;
                case ClientCommand.Start:
                    StartCommand();
                    break;
                case ClientCommand.Stop:
                    StopCommand();
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }

        private static async Task SendMessage(ITransceiver transceiver, string input)
        {
            switch(CurrentService.Value)
            {
                case Service.Chat:
                    input = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
                    break;
            }
            await transceiver.Transmit($"{CurrentService.Value} {input}");
        }

        private static void StopCommand()
        {
            if(!CurrentProtocol.HasValue)
            {
                Console.WriteLine("Set protocol first before starting client.");
                return;
            }

            if(!Transceivers.TryGetValue(CurrentProtocol.Value, out var transceiver))
            {
                Console.WriteLine("Transceiver not found!");
                return;
            }

            if(!transceiver.Active)
            {
                Console.WriteLine("Transceiver is not active!");
                return;
            }

            transceiver.Stop();
        }

        private static void StartCommand()
        {
            if (!CurrentProtocol.HasValue)
            {
                Console.WriteLine("Set protocol first before starting client.");
                return;
            }

            if (!Transceivers.TryGetValue(CurrentProtocol.Value, out var transceiver))
            {
                Console.WriteLine("Transceiver not found!");
                return;
            }

            if (transceiver.Active)
            {
                Console.WriteLine("Transceiver is already active!");
                return;
            }

            transceiver.Start();
        }

        private static void UseCommand(IEnumerable<string> arguments)
        {
            if(arguments.Count() == 0 || arguments.Count() > 1 ||
                !Enum.TryParse(typeof(Protocol), arguments.FirstOrDefault(), true, out var protocol) ||
                !Transceivers.ContainsKey((Protocol)protocol))
            {
                Console.WriteLine("Unknown protocol.");
                return;
            }

            CurrentProtocol = (Protocol)protocol;
        }

        private static void ServiceCommand(IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0 || arguments.Count() > 1 ||
                !Enum.TryParse(typeof(Service), arguments.FirstOrDefault(), true, out var service))
            {
                Console.WriteLine("Unknown service.");
                return;
            }

            CurrentService = (Service)service;
        }

        private static void HelpCommand(IEnumerable<string> arguments)
        {

        }
    }
}