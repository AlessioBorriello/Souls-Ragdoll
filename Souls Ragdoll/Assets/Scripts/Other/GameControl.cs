using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{
    public class GameControl : NetworkBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private List<Transform> spawnPoints;

        public NetworkVariable<int> spawnIndex = new NetworkVariable<int>(0);
        public NetworkVariable<int> playersConnected = new NetworkVariable<int>(0);

        private void Start()
        {
            Application.targetFrameRate = 60;

            NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
            {
                if (NetworkManager.Singleton.IsServer) Debug.Log($"Client {id} connected...");
            };

            NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
            {
                if (NetworkManager.Singleton.IsServer) Debug.Log($"Client {id} disconnected...");


            };
        }

        private void Update()
        {
            UpdateConnectedPlayersNumber();
        }

        private void UpdateConnectedPlayersNumber()
        {
            if (!IsServer) return;
            playersConnected.Value = NetworkManager.Singleton.ConnectedClients.Count;
        }

        private int GetSpawnIndex()
        {
            int index = spawnIndex.Value;
            if (!NetworkManager.Singleton.IsServer) IncreaseSpawnIndexServerRpc();
            else spawnIndex.Value = (spawnIndex.Value + 1) % spawnPoints.Count;
            return index;
        }

        [ServerRpc(RequireOwnership = false)]
        private void IncreaseSpawnIndexServerRpc()
        {
            spawnIndex.Value = (spawnIndex.Value + 1) % spawnPoints.Count;
        }

        public Transform GetSpawnPoint()
        {
            return spawnPoints[GetSpawnIndex()];
        }

    }
}