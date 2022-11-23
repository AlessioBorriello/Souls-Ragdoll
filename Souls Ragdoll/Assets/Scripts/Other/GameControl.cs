using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{
    public class GameControl : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        private List<PlayerManager> playersConnected = new List<PlayerManager>();

        private void Start()
        {
            Application.targetFrameRate = 60;
        }

        public void RespawnPlayer(ulong deadPlayerID)
        {
            //NetworkManager.DisconnectClient(deadPlayerID);
            //Debug.Log("Respawn player " + deadPlayer.GetComponent<PlayerManager>().OwnerClientId);
            //deadPlayer.GetComponent<NetworkObject>().Despawn();
            //GameObject newPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            //newPlayer.GetComponent<NetworkObject>().Spawn();
        }

        public void AddPlayerToList(PlayerManager player)
        {
            playersConnected.Add(player);
        }

    }
}