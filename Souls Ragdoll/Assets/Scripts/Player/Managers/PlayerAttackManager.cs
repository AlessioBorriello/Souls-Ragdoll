using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerAttackManager : MonoBehaviour
    {
        private PlayerManager playerManager;

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();
        }

        public void HandleAttacks()
        {

            if(playerManager.disablePlayerInteraction) return;

            //Right bumper
            if (playerManager.inputManager.rbInputPressed)
            {
                HandEquippableItem item = playerManager.inventoryManager.currentRightSlotItem;
                if(item is WeaponItem) HandleLightAttack( (WeaponItem)item );
            }
            //Right trigger
            if (playerManager.inputManager.rtInputPressed)
            {
                HandEquippableItem item = playerManager.inventoryManager.currentRightSlotItem;
                if (item is WeaponItem) HandleHeavyAttack((WeaponItem)item);
            }
        }

        private void HandleLightAttack(WeaponItem weapon)
        {
            playerManager.animationManager.PlayTargetAnimation(weapon.OneHandLightAttackOne, .2f);
        }

        private void HandleHeavyAttack(WeaponItem weapon)
        {
            playerManager.animationManager.PlayTargetAnimation(weapon.OneHandHeavyAttackOne, .2f);
        }

    }
}
