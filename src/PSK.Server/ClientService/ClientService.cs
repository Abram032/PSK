using PSK.Core;
using PSK.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PSK.Server
{
    public class ClientService : IClientService
    {
        private ConcurrentDictionary<Guid, Client> clients;
        private ConcurrentDictionary<string, Guid> clientsAliases;

        public int ClientCount { get => clients.Count; }

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

        public void ClearClients()
        {
            foreach(var client in clients)
            {
                client.Value.Transceiver.Stop();
                client.Value.Transceiver.Dispose();
            }
            clients.Clear();
            clientsAliases.Clear();
        }

        public IEnumerable<string> GetClients()
        {
            return clientsAliases.Keys;
        }
    }
}
