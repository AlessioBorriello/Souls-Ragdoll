using AlessioBorriello;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class AnimationEventsManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private PlayerLocomotionManager locomotionManager;
        private PlayerInventoryManager inventoryManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerCombatManager combatManager;
        private PlayerWeaponManager weaponManager;

        private void Start()
        {
            playerManager = GetComponentInParent<PlayerManager>();
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

        #region Collider stuff
        public void EnableDamageCollider()
        {
            ColliderControl colliderControl = GetDamageColliderControl();
            if (colliderControl != null) colliderControl.ToggleCollider(true);
        }

        public void DisableDamageCollider()
        {
            ColliderControl colliderControl = GetDamageColliderControl();
            if (colliderControl != null)
            {
                colliderControl.ToggleCollider(false);
                colliderControl.EmptyHitList();
            }
        }

        private ColliderControl GetDamageColliderControl()
        {
            if (!weaponManager.IsAttackingWithLeft()) return inventoryManager.GetCurrentItemColliderControl(false);
            else return inventoryManager.GetCurrentItemColliderControl(true);
        }
        #endregion
    }
}