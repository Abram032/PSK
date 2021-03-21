using System;

namespace PSK.Core.Models
{
    public class OnReceivedEventArgs : EventArgs
    {
        public Guid ClientId { get; set; }
        public string Data { get; set; }
    }
}
