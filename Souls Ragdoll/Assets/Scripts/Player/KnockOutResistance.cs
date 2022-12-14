using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class KnockOutResistance : MonoBehaviour
    {
        private PlayerManager playerManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerNetworkManager networkManager;

        [HideInInspector] public float knockOutResistance = 200f; //Strenght of collision before the player is knocked out
        public BodyParts bodyPart;

        private void Awake()
        {
            playerManager = GetComponentInParent<PlayerManager>(); //Get player manager
            ragdollManager = playerManager.GetRagdollManager();
            networkManager = playerManager.GetNetworkManager();
            SetResistances();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Player") || playerManager.isKnockedOut) return;

            if (collision.impulse.magnitude > knockOutResistance)
            {
                if(playerManager.isClient) Debug.Log($"Collision of: {this.name} with {collision.collider.name}, force {collision.impulse.magnitude} (Res: {knockOutResistance})");
                ragdollManager.KnockOut();
                networkManager.KnockOutServerRpc();
            }
        }

        private void SetResistances()
        {
            switch(bodyPart)
            {
                case BodyParts.Hip: knockOutResistance = playerManager.playerData.hipResistance; break;

                case BodyParts.Legl: knockOutResistance = playerManager.playerData.legResistance; break;
                case BodyParts.Legr: knockOutResistance = playerManager.playerData.legResistance; break;

                case BodyParts.Shinl: knockOutResistance = playerManager.playerData.shinResistance; break;
                case BodyParts.Shinr: knockOutResistance = playerManager.playerData.shinResistance; break;

                case BodyParts.Footl: knockOutResistance = playerManager.playerData.footResistance; break;
                case BodyParts.Footr: knockOutResistance = playerManager.playerData.footResistance; break;

                case BodyParts.Torso: knockOutResistance = playerManager.playerData.torsoResistance; break;

                case BodyParts.Arml: knockOutResistance = playerManager.playerData.armResistance; break;
                case BodyParts.Armr: knockOutResistance = playerManager.playerData.armResistance; break;

                case BodyParts.Forearml: knockOutResistance = playerManager.playerData.forearmResistance; break;
                case BodyParts.Forearmr: knockOutResistance = playerManager.playerData.forearmResistance; break;

                case BodyParts.Handl: knockOutResistance = playerManager.playerData.handResistance; break;
                case BodyParts.Handr: knockOutResistance = playerManager.playerData.handResistance; break;

                case BodyParts.Neck: knockOutResistance = playerManager.playerData.neckResistance; break;

                case BodyParts.Head: knockOutResistance = playerManager.playerData.headResistance; break;
                
                default: knockOutResistance = 100; break;
            }
        }

    }
}