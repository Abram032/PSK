using PSK.Core;
using PSK.Core.Models.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Services.Chat
{
    public class ChatService : IChatService
    {
        private ConcurrentDictionary<string, ConcurrentQueue<string>> messages;

        private readonly IClientService _clientService;

        public ChatService(IClientService clientService)
        {
            _clientService = clientService;

            messages = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        }

        public async Task<string> ProcessRequest(Guid clientId, string request)
        {
            var data = Encoding.UTF8.GetString(Convert.FromBase64String(request));
            var command = data.Split(' ').FirstOrDefault();
            var alias = data.Split(' ').Skip(1).FirstOrDefault();
            var message = string.Join(' ', data.Split(' ').Skip(2));

            var commandEnum = Enum.Parse(typeof(ChatCommand), command, true);

            string response = null;
            switch (commandEnum)
            {
                case ChatCommand.GetUsers:
                    response = GetUsers();
                    break;
                case ChatCommand.Get:
                    response = Get(clientId);
                    break;
                case ChatCommand.Send:
                    response = Send(clientId, alias, message);
                    break;
                case ChatCommand.SetAlias:
                    response = SetAlias(clientId, alias);
                    break;
                default:
                    response = "Unknown command for Configure service";
                    break;
            }

            return $"chat {response}";
        }

        private string Send(Guid senderId, string alias, string message)
        {
            var receiver = _clientService.GetClientByAlias(alias);
            var sender = _clientService.GetClientById(senderId);
            if (receiver == null || sender == null)
            {
                return "Could not find user with given alias";
            }

            if(!messages.ContainsKey(alias) ||
                !messages.TryAdd(alias, new ConcurrentQueue<string>()) ||
                !messages.TryGetValue(alias, out var _messages))
            {
                return "Could not send message to user.";
            }

             _messages.Enqueue($"({sender.Alias}): {message}");
            return $"Message sent to {alias}";
        }

        private string Get(Guid clientId)
        {
            var client = _clientService.GetClientById(clientId);
            if (!messages.TryGetValue(client.Alias, out var _messages))
            {
                return "Could not receive messages.";
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Received {_messages.Count} messages:");
            while(!_messages.IsEmpty)
            {
                if(_messages.TryDequeue(out var _message))
                {
                    stringBuilder.AppendLine(_message);
                }
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
        }

        private string GetUsers()
        {
            var clients =_clientService.GetClients();

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Connected users ({clients.Count()}):");
            foreach(var client in clients)
            {
                stringBuilder.AppendLine(client);
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
        }

        private string SetAlias(Guid clientId, string alias)
        {
            if(_clientService.ClientAliasExists(alias))
            {
                return "Client alias already exists.";
            }

            if(!_clientService.SetClientAlias(clientId, alias))
            {
                return "Could not set client alias.";
            }

            return $"Alias set to {alias}";
        }
    }
}