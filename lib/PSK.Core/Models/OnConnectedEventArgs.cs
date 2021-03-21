using System;

namespace PSK.Core.Models
{
    public class OnConnectedEventArgs : EventArgs
    {
        public Guid ClientId { get; set; }
        public object Client { get; set; }
    }
}
