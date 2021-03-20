using PSK.Core.Enums;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace PSK.Core.Models
{
    public class OnConnectedEventArgs : EventArgs
    {
        public Guid ClientId { get; set; }
        public object Client { get; set; }
        public ClientType ClientType { get; set; }
    }
}
