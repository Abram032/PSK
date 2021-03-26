using PSK.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PSK.Server.Core
{
    public interface IClientService
    {
        bool ClientExists(Guid id);
        bool ClientAliasExists(string alias);
        Client GetClientById(Guid id);
        Client GetClientByAlias(string alias);
        bool SetClientAlias(Guid id, string alias);
        bool AddClient(Client client);
        bool RemoveClient(Guid id);
    }
}
