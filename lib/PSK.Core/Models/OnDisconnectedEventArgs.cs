using PSK.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PSK.Core.Models
{
    public class OnDisconnectedEventArgs
    {
        public Guid ClientId { get; set; }
    }
}
