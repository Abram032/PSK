using System;

namespace PSK.Core.Models
{
    public class Client
    {
        public Guid Id { get; set; }
        public ITransceiver Transceiver { get; set; }
    }
}
