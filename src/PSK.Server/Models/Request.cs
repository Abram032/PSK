using System;

namespace PSK.Server.Models
{
    public class Request
    {
        public Guid ClientId { get; set; }
        public string Data { get; set; }
    }
}
