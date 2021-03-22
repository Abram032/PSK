using System;

namespace PSK.Core.Models
{
    public class Request
    {
        //TODO: Extend with command proprety
        public Guid ClientId { get; set; }
        public string Data { get; set; }
    }
}
