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

        public int ClientCount { get => clients.Count; }

        public ClientService()
        {
            clients = new ConcurrentDictionary<Guid, Client>();
        }

        public bool ClientExists(Guid id) => clients.ContainsKey(id);


        public bool AddClient(Client client)
        {
            if (!clients.TryAdd(client.Id, client))
                return false;

            return true;
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

            return true;
        }

        public void ClearClients()
        {
            foreach (var client in clients)
            {
                client.Value.Transceiver.Stop();
                client.Value.Transceiver.Dispose();
            }
            clients.Clear();
        }
    }
}
