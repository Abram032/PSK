using Newtonsoft.Json;
using PSK.Client.Enums;
using PSK.Core;
using PSK.Core.Models;
using PSK.Core.Models.Services.Chat;
using PSK.Core.Models.Services.Configure;
using PSK.Core.Models.Services.File;
using PSK.Protocols.Tcp;
using System;
using System.Collections.Generic;
using System.IO;
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
            string data = null;
            switch(CurrentService.Value)
            {
                case Service.Chat:
                    data = BuildChatRequest(input);
                    break;
                case Service.Ping:
                    data = BuildPingRequest(input);
                    break;
                case Service.Configure:
                    data = BuildConfigureRequest(input);
                    break;
                case Service.File:
                    data = BuildFileRequest(input);
                    break;
            }

            if (!string.IsNullOrEmpty(data))
            {
                await transceiver.Transmit(data);
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

        private static string BuildChatRequest(string input)
        {
            var command = (ChatCommand)Enum.Parse(typeof(ChatCommand), input.Split(' ').FirstOrDefault(), true);
            var request = new ChatRequest();

            switch(command)
            {
                case ChatCommand.Get:
                    request.Command = command;
                    request.Alias = input.Split(' ').Skip(1).FirstOrDefault();
                    break;
                case ChatCommand.Send:
                    request.Command = command;
                    request.Message = new ChatMessage
                    {
                        Timestamp = DateTime.Now,
                        Sender = input.Split(' ').Skip(1).FirstOrDefault(),
                        Receiver = input.Split(' ').Skip(2).FirstOrDefault(),
                        Content = string.Join(' ', input.Split(' ').Skip(3))
                    };
                    break;
                default:
                    Console.WriteLine("Invalid request.");
                    return null;
            }

            var message = new Message()
            {
                Service = Service.Chat,
                Data = JsonConvert.SerializeObject(request)
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
        }

        private static string BuildPingRequest(string input)
        {
            return $"ping {input}";
        }

        private static string BuildConfigureRequest(string input)
        {
            var command = (ConfigureCommand)Enum.Parse(typeof(ConfigureCommand), input.Split(' ').FirstOrDefault(), true);
            var request = new ConfigureRequest
            {
                Command = command,
                ServiceOptions = input.Split(' ').Skip(1).FirstOrDefault()
            };

            switch (command)
            {
                case ConfigureCommand.Get:
                    break;
                case ConfigureCommand.Update:
                    var options = new Dictionary<string, string>();
                    foreach (var pair in input.Split('-').AsEnumerable().Skip(1).Select(o => o.Trim().Split(' ')))
                    {
                        options.Add(pair.FirstOrDefault(), pair.LastOrDefault());
                    }
                    request.Options = options;
                    break;
                default:
                    Console.WriteLine("Invalid request.");
                    return null;
            }

            var message = new Message()
            {
                Service = Service.Configure,
                Data = JsonConvert.SerializeObject(request)
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
        }

        private static string BuildFileRequest(string input)
        {
            var command = (FileCommand)Enum.Parse(typeof(FileCommand), input.Split(' ').FirstOrDefault(), true);
            var request = new FileRequest
            {
                Command = command
            };

            switch (command)
            {
                case FileCommand.List:
                    break;
                case FileCommand.Get:
                    request.FileName = input.Split(' ').Skip(1).FirstOrDefault();
                    break;
                case FileCommand.Delete:
                    request.FileName = input.Split(' ').Skip(1).FirstOrDefault();
                    break;
                case FileCommand.Put:
                    request.FileName = input.Split(' ').Skip(1).FirstOrDefault();
                    var filePath = input.Split(' ').Skip(2).FirstOrDefault();
                    if(!File.Exists(filePath))
                    {
                        Console.WriteLine("File doesn't exist!");
                    }
                    var bytes = File.ReadAllBytes(filePath);
                    var data = Convert.ToBase64String(bytes);
                    request.Data = data;
                    break;
                default:
                    Console.WriteLine("Invalid request.");
                    return null;
            }

            var message = new Message()
            {
                Service = Service.File,
                Data = JsonConvert.SerializeObject(request)
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
        }
    }
}