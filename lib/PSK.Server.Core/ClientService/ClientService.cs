using Microsoft.Extensions.Options;
using PSK.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace PSK.Server.Core
{
    public class ClientService : IClientService
    {
        private ConcurrentDictionary<Guid, Client> clients;
        private ConcurrentDictionary<string, Guid> clientsAliases;

        public ClientService()
        {
            clients = new ConcurrentDictionary<Guid, Client>();
            clientsAliases = new ConcurrentDictionary<string, Guid>();
        }

        public bool ClientExists(Guid id) => clients.ContainsKey(id);

        public bool ClientAliasExists(string alias) => clientsAliases.ContainsKey(alias);

        public bool AddClient(Client client)
        {
            if (!clients.TryAdd(client.Id, client))
                return false;

            if (client.Alias != null && !clientsAliases.TryAdd(client.Alias, client.Id))
                return false;

            return true;
        }

        public bool SetClientAlias(Guid id, string alias)
        {
            if (!clients.TryGetValue(id, out var client))
                return false;

            client.Alias = alias;

            return true;
        }

        public Client GetClientByAlias(string alias)
        {
            if (!clientsAliases.TryGetValue(alias, out var id))
                return null;

            if (!clients.TryGetValue(id, out var client))
                return null;

            return client;
        }

        public Client GetClientById(Guid id)
        {
            if (!clients.TryGetValue(id, out var client))
                return null;

            return client;
        }

        public bool RemoveClient(Guid id)
        {
            if (!clients.TryRemove(id, out var client))
                return false;

            if (client.Alias != null && !clientsAliases.TryRemove(client.Alias, out _))
                return false;

            return true;
        }
    }
}
