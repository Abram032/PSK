using PSK.Core.Models;
using System;
using System.Collections.Generic;

namespace PSK.Core
{
    public interface IClientService
    {
        public int ClientCount { get; }

        bool ClientExists(Guid id);
        bool ClientAliasExists(string alias);
        Client GetClientById(Guid id);
        Client GetClientByAlias(string alias);
        bool SetClientAlias(Guid id, string alias);
        bool AddClient(Client client);
        bool RemoveClient(Guid id);
        void ClearClients();
        IEnumerable<string> GetClients();
    }
}
