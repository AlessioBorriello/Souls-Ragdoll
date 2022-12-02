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

        private void Start()
        {
            playerManager = GetComponentInParent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
            locomotionManager = playerManager.GetLocomotionManager();
            inventoryManager = playerManager.GetInventoryManager();
            ragdollManager = playerManager.GetRagdollManager();
            combatManager = playerManager.GetCombatManager();
            weaponManager = combatManager.GetWeaponManager();
        }

        public void SetPlayerStuckInAnimation()
        {
            playerManager.playerIsStuckInAnimation = true;
        }

        public void SetPlayerNotStuckInAnimation()
        {
            playerManager.playerIsStuckInAnimation = false;
        }

        public void AddJumpForceOnRoll()
        {
            ragdollManager.AddForceToPlayer(locomotionManager.GetGroundNormal() * playerManager.playerData.rollJumpForce, ForceMode.Impulse);
        }

        public void EnableRotation()
        {
            playerManager.canRotate = true;
        }

        public void DisableRotation()
        {
            playerManager.canRotate = false;
        }

        public void ActivateIFrames()
        {
            playerManager.areIFramesActive = true;
        }

        public void DeactivateIFrames()
        {
            playerManager.areIFramesActive = false;
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
        public void EnableDamageCollider()
        {
            DamageColliderControl colliderControl = GetDamageColliderControl();
            if (colliderControl != null) colliderControl.ToggleCollider(true);
        }

        public void DisableDamageCollider()
        {
            DamageColliderControl colliderControl = GetDamageColliderControl();
            if (colliderControl != null) colliderControl.ToggleCollider(false);
        }

        public void EnableParry()
        {
            playerManager.isParrying = true;
        }

        public void DisableParry()
        {
            playerManager.isParrying = false;
        }

        private DamageColliderControl GetDamageColliderControl()
        {
            if (!weaponManager.IsAttackingWithLeft()) return inventoryManager.GetCurrentItemDamageColliderControl(false);
            else return inventoryManager.GetCurrentItemDamageColliderControl(true);
        }
        #endregion
    }
}