using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlessioBorriello
{
    public class PlayerCombatManager : NetworkBehaviour
    {
        private PlayerManager playerManager;
        private PlayerWeaponManager weaponManager;
        private PlayerShieldManager shieldManager;

        public bool diedFromCriticalDamage = false;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            weaponManager = playerManager.GetWeaponManager();
            shieldManager = playerManager.GetShieldManager();
        }

        public void HandleCombat()
        {
            if (playerManager.disableActions) return;

            //Attacks
            weaponManager.HandleAttacks();
            //Shields
            shieldManager.HandleBlocks();
        }

        [ServerRpc(RequireOwnership = false)]
        public void GotBackstabbedServerRpc(Vector3 backstabbedPosition, Quaternion backstabbedRotation, string backstabbedAnimation, float damage, ulong id)
        {
            GotBackstabbedClientRpc(backstabbedPosition, backstabbedRotation, backstabbedAnimation, damage, id);
        }

        [ClientRpc]
        private void GotBackstabbedClientRpc(Vector3 backstabbedPosition, Quaternion backstabbedRotation, string backstabbedAnimation, float damage, ulong id)
        {
            if (playerManager.OwnerClientId != id) return;

            //Check if backstab actually occurred, else return
            GotBackstabbed(backstabbedPosition, backstabbedRotation, backstabbedAnimation, damage);
        }

        private void GotBackstabbed(Vector3 backstabbedPosition, Quaternion backstabbedRotation, string backstabbedAnimation, float damage)
        {
            if(!IsOwner) return;

            //Play animation
            playerManager.GetAnimationManager().PlayTargetAnimation(backstabbedAnimation, .1f, true);

            //Stop player
            playerManager.GetLocomotionManager().SetMovementSpeedMultiplier(1);

            //Position player
            playerManager.GetPhysicalHips().transform.position = backstabbedPosition;
            playerManager.GetAnimatedPlayer().transform.rotation = backstabbedRotation;

            //Take damage
            StartCoroutine(playerManager.GetStatsManager().TakeCriticalDamage((int)damage, .5f));
        }

        public PlayerWeaponManager GetWeaponManager()
        {
            return weaponManager;
        }

        public PlayerShieldManager GetShieldManager()
        {
            return shieldManager;
        }

    }
}
