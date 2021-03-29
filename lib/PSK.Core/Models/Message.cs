using System;
using System.Collections.Generic;

namespace PSK.Core.Models
{
    public class Message
    {
        public Guid ClientId { get; set; }
        public Service? Service { get; set; }
        public bool Succeded { get; set; }
        public string Error { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Data { get; set; }
    }
}
