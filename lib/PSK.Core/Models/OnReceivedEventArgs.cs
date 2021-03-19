using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Core.Models
{
    public class OnReceivedEventArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }
        public string Data { get; set; }
    }
}
