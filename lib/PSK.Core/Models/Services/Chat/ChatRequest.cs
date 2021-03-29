using System;
using System.Collections.Generic;
using System.Text;

namespace PSK.Core.Models.Services.Chat
{
    public class ChatRequest
    {
        public ChatCommand Command { get; set; }
        public string Alias { get; set; }
        public ChatMessage Message { get; set; }
    }
}
