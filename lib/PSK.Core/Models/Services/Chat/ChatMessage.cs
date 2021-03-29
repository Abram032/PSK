using System;
using System.Collections.Generic;
using System.Text;

namespace PSK.Core.Models.Services.Chat
{
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public DateTime Timestamp { get; set; }
        public string Content { get; set; }
    }
}
