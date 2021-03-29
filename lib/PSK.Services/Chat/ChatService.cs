using Newtonsoft.Json;
using PSK.Core;
using PSK.Core.Models;
using PSK.Core.Models.Services;
using PSK.Core.Models.Services.Chat;
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
        private ConcurrentDictionary<string, ConcurrentQueue<ChatMessage>> messages;

        private readonly IClientService _clientService;

        public ChatService(IClientService clientService)
        {
            _clientService = clientService;

            messages = new ConcurrentDictionary<string, ConcurrentQueue<ChatMessage>>();
        }

        public async Task<string> ProcessRequest(string data)
        {
            var request = JsonConvert.DeserializeObject<ChatRequest>(data);

            Message response;
            switch (request.Command)
            {
                case ChatCommand.Get:
                    response = Get(request);
                    break;
                case ChatCommand.Send:
                    response = Send(request);
                    break;
                default:
                    response = new Message()
                    {
                        Service = Service.Chat,
                        Succeded = false,
                        Error = "Unknown command for Chat service"
                    };
                    break;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
        }

        private Message Send(ChatRequest request)
        {
            var response = new Message()
            {
                Service = Service.Chat,
                Succeded = false,
            };

            if(!messages.ContainsKey(request.Message.Receiver) && !messages.TryAdd(request.Message.Receiver, new ConcurrentQueue<ChatMessage>()))
            {
                response.Error = "Could not send message to user with given alias";
                return response;
            }

            if (!messages.TryGetValue(request.Message.Receiver, out var _messages))
            {
                response.Error = "Could not send message to user with given alias";
                return response;
            }

             _messages.Enqueue(request.Message);
            response.Succeded = true;
            response.Data = $"Message sent to {request.Message.Receiver}";
            return response;
        }

        private Message Get(ChatRequest request)
        {
            var message = new Message()
            {
                Service = Service.Chat,
                Succeded = false,
            };

            if (!messages.TryGetValue(request.Alias, out var _messages))
            {
                message.Error = "Could not receive messages.";
                return message;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Received {_messages.Count} messages:");
            while(!_messages.IsEmpty)
            {
                if(_messages.TryDequeue(out var _message))
                {
                    stringBuilder.AppendLine($"{_message.Timestamp.ToShortTimeString()} ({_message.Sender}): {_message.Content}");
                }
            }

            message.Succeded = true;
            message.Data = stringBuilder.ToString();
            return message;
        }
    }
}