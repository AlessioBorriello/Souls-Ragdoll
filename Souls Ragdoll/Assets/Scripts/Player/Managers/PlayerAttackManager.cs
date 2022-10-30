using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerAttackManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        public bool attackingWithLeft = false;

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();
        }

        public void HandleAttacks()
        {

            if(playerManager.disablePlayerInteraction) return;

            //if (playerManager.inputManager.rbInputPressed || !playerManager.isClient) //To make dummy attack
            //Right bumper
            if (playerManager.inputManager.rbInputPressed) TryAttack(false, false);
            //Right trigger
            if (playerManager.inputManager.rtInputPressed) TryAttack(false, true);

            //Left bumper
            if (playerManager.inputManager.lbInputPressed) TryAttack(true, false);
            //Left trigger
            if (playerManager.inputManager.ltInputPressed) TryAttack(true, true);
        }

        private void TryAttack(bool isLeft, bool isHeavy)
        {
            //Get right or left item
            HandEquippableItem item = (isLeft)? playerManager.inventoryManager.currentLeftSlotItem : playerManager.inventoryManager.currentRightSlotItem;
            if (item is not WeaponItem) return;

            //Set bools
            attackingWithLeft = isLeft;
            playerManager.animationManager.animator.SetBool("attackingWithLeft", isLeft);

            //Get animation to play
            string attackAnimation = GetAttackAnimationString((WeaponItem)item, isHeavy);
            //Play animation
            playerManager.animationManager.PlayTargetAnimation(attackAnimation, .2f);
        }

        private string GetAttackAnimationString(WeaponItem weapon, bool isHeavy)
        {
            if (!isHeavy) return weapon.OneHandLightAttackOne;
            else return weapon.OneHandHeavyAttackOne;
        }

    }
}
