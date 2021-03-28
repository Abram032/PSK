using PSK.Client.Enums;
using PSK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
                { Protocol.Tcp, new TcpTransceiver() }
            };

            Console.WriteLine("Use '/help' for more information.");
            while (true)
            {
                Console.Write($"({CurrentProtocol} | {CurrentService}) > ");

                var input = Console.ReadLine();
                ParseCommand(input);
            }

            await Task.Delay(-1);
        }

        private static void ParseCommand(string input)
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

                transceiver.Transmit($"{CurrentService.Value} {input}\n");
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