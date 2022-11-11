using AlessioBorriello;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class AnimationEventsManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private PlayerInventoryManager inventoryManager;

        private void Start()
        {
            playerManager = GetComponentInParent<PlayerManager>();
            inventoryManager = GetComponentInParent<PlayerInventoryManager>();
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
            playerManager.ragdollManager.AddForceToPlayer(playerManager.groundNormal * playerManager.playerData.rollJumpForce, ForceMode.Impulse);
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
            if (!playerManager.attackManager.attackingWithLeft)
            {
                Collider collider = inventoryManager.currentRightSlotItemCollider;
                if(collider != null) collider.enabled = true;
            }
            else
            {
                Collider collider = inventoryManager.currentLeftSlotItemCollider;
                if (collider != null) collider.enabled = true;
            }
        }

        public void DisableDamageCollider()
        {
            if (!playerManager.attackManager.attackingWithLeft)
            {
                Collider collider = inventoryManager.currentRightSlotItemCollider;
                if (collider != null) collider.enabled = false;
            }
            else
            {
                Collider collider = inventoryManager.currentLeftSlotItemCollider;
                if (collider != null) collider.enabled = false;
            }
        }
        #endregion
    }
}