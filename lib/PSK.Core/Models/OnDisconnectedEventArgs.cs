using System;

namespace PSK.Core.Models
{
    public class OnDisconnectedEventArgs : EventArgs
    {
        public Guid ClientId { get; set; }
    }
}
