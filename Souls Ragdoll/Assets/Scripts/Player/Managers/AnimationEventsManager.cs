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

        #region Combos
        public void OpenComboChance()
        {
            playerManager.attackManager.canCombo = true;
        }

        public void CloseComboChance()
        {
            playerManager.attackManager.canCombo = false;
        }
        #endregion

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