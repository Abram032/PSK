using System;
using System.Collections.Generic;
using System.Text;

namespace PSK.Protocols.Tcp
{
    public class TcpReceiverOptions
    {
        public int ListenPort { get; set; }
        public int ReceiveTimeout { get; set; }
        public int SendTimeout { get; set; }
    }
}
