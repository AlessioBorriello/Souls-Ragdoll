using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerAttackManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        public bool attackingWithLeft = false;
        private AttackType attackType;

        public int nextComboAttackIndex = 0;
        public bool canCombo = false;
        public bool chainedAttack = false;

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();
        }

        private void Update()
        {
            //Keep the attackingWithLeft bool in sync with the animator
            playerManager.animationManager.animator.SetBool("attackingWithLeft", attackingWithLeft);
        }

        public void HandleAttacks()
        {

            if(playerManager.disablePlayerInteraction && !canCombo) return;

            //Right bumper
            if (playerManager.inputManager.rbInput) TryAttack(false, false);
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

            //Get this attack's proprieties
            bool currentAttackingWithLeft = isLeft;
            AttackType currentAttackType = GetAttackType(isHeavy);

            //Check for combos
            if(nextComboAttackIndex > 0)
            {
                //Cases where the combo is not done
                if(!canCombo || currentAttackingWithLeft != attackingWithLeft || currentAttackType != attackType)
                {
                    //Combo did not happen
                    chainedAttack = false;
                    return;
                }else
                {
                    Debug.Log("Combo");
                    chainedAttack = true;
                }
            }

            //Update proprieties
            attackingWithLeft = currentAttackingWithLeft;
            attackType = currentAttackType;

            //Get animation to play
            string attackAnimation = GetAttackAnimationString((WeaponItem)item, isHeavy);
            //Play animation
            playerManager.animationManager.PlayTargetAnimation(attackAnimation, .2f);

            //Disable combo until it is opened again in the animation events
            canCombo = false;
        }

        private AttackType GetAttackType(bool isHeavy)
        {
            return (isHeavy) ? AttackType.heavy : AttackType.light;
        }

        private string GetAttackAnimationString(WeaponItem weapon, bool isHeavy)
        {
            string animation;
            if (!isHeavy)
            {
                int comboIndex = nextComboAttackIndex % weapon.OneHandedLightAttackCombo.Length;
                animation = weapon.OneHandedLightAttackCombo[comboIndex];
                nextComboAttackIndex++;
                if (animation == "")
                {
                    animation = GetAttackAnimationString(weapon, isHeavy);
                }
            }
            else
            {
                animation = weapon.OneHandedHeavyAttackCombo[nextComboAttackIndex++];
            }

            return animation;
        }

        private enum AttackType
        {
            light,
            heavy,
            running,
            rolling
        }

    }
}
