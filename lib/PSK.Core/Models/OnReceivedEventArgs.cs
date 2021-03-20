using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Core.Models
{
    public class OnReceivedEventArgs : EventArgs
    {
        public Guid ClientId { get; set; }
        public string Data { get; set; }
    }
}