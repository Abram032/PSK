using System;

namespace PSK.Core.Models
{
    public class Request
    {
        public Guid ClientId { get; set; }
        public string Command { get; set; }
        public string Data { get; set; }
    }
}
