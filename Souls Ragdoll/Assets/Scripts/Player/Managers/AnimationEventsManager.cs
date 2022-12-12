using AlessioBorriello;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{
    public class AnimationEventsManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private PlayerNetworkManager networkManager;
        private PlayerLocomotionManager locomotionManager;
        private PlayerInventoryManager inventoryManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerCombatManager combatManager;
        private PlayerWeaponManager weaponManager;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
            locomotionManager = playerManager.GetLocomotionManager();
            inventoryManager = playerManager.GetInventoryManager();
            ragdollManager = playerManager.GetRagdollManager();
            combatManager = playerManager.GetCombatManager();
            weaponManager = combatManager.GetWeaponManager();
        }

        public void AddJumpForceOnRoll(float force)
        {
            ragdollManager.AddForceToPlayer(locomotionManager.GetGroundNormal() * force, ForceMode.Impulse);
        }

        public void SetPlayerStuckInAnimation(bool toggle)
        {
            playerManager.isStuckInAnimation = toggle;
        }

        public void SetCanRotate(bool toggle)
        {
            playerManager.canRotate = toggle;
        }

        public void ToggleIFrames(bool toggle)
        {
            playerManager.areIFramesActive = toggle;
        }

        public void CheckForCriticalDamageDeath()
        {
            if(combatManager.diedFromCriticalDamage)
            {
                combatManager.diedFromCriticalDamage = false;
                playerManager.Die();
                networkManager.DieServerRpc();
            }
        }

        #region Collider stuff
        public void ToggleDamageCollider(bool enable)
        {
            DamageColliderControl colliderControl = inventoryManager.GetCurrentItemDamageColliderControl(false);
            if (colliderControl != null) colliderControl.ToggleCollider(enable);
        }

        public void ToggleParriable(bool enable)
        {
            DamageColliderControl colliderControl = inventoryManager.GetCurrentItemDamageColliderControl(false);
            if (colliderControl != null) colliderControl.ToggleParriable(enable);
        }

        public void ToggleParry(bool enable)
        {
            playerManager.isParrying = enable;
        }
        #endregion
    }
}