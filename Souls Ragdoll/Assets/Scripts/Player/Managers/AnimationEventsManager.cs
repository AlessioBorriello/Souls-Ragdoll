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
            if(!playerManager.attackManager.attackingWithLeft) inventoryManager.currentRightSlotItemCollider.enabled = true;
            else inventoryManager.currentLeftSlotItemCollider.enabled = true;
        }

        public void DisableDamageCollider()
        {
            if (!playerManager.attackManager.attackingWithLeft) inventoryManager.currentRightSlotItemCollider.enabled = false;
            else inventoryManager.currentLeftSlotItemCollider.enabled = false;
        }
        #endregion
    }
}