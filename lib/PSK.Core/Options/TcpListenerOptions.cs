using System;
using System.Collections.Generic;
using System.Text;

namespace PSK.Core.Options
{
    public class TcpListenerOptions
    {
        public int ListenPort { get; set; }
        public int ReceiveTimeout { get; set; }
        public int SendTimeout { get; set; }
    }
}
