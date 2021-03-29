using PSK.Core.Models;
using System;
using System.Collections.Generic;

namespace PSK.Core
{
    public interface IClientService
    {
        public int ClientCount { get; }

        bool ClientExists(Guid id);
        Client GetClientById(Guid id);
        bool AddClient(Client client);
        bool RemoveClient(Guid id);
        void ClearClients();
    }
}
