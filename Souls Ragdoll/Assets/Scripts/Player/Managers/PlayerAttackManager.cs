using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerAttackManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        [HideInInspector] public bool attackingWithLeft = false;
        private AttackType attackType;

        [HideInInspector] public int nextComboAttackIndex = 0;
        [HideInInspector] public bool canCombo = false;
        [HideInInspector] public bool chainedAttack = false;

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

            //If player is already attacking and can not combo into next attack, return
            if(playerManager.disablePlayerInteraction && !canCombo) return;

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

            //Get this new attack's proprieties
            bool newAttackingWithLeft = isLeft;
            AttackType newAttackType = GetAttackType(isHeavy);

            //Check for combos
            if(nextComboAttackIndex > 0 && canCombo) chainedAttack = CheckForCombo(newAttackingWithLeft, newAttackType);

            //If an attack was NOT chained and it is not the first attack
            if (!chainedAttack && nextComboAttackIndex != 0) return;

            //Update proprieties
            attackingWithLeft = newAttackingWithLeft;
            attackType = newAttackType;

            //Get animation to play
            string attackAnimation = GetAttackAnimationString((WeaponItem)item, isHeavy);
            //Play animation
            playerManager.animationManager.PlayTargetAnimation(attackAnimation, .2f);
            if (chainedAttack) Debug.Log("Combo: " + attackAnimation);

            //Disable combo until it is opened again in the animation events
            canCombo = false;
        }

        private bool CheckForCombo(bool isLeft, AttackType attackType)
        {
            //Cases where the combo is not done
            if (isLeft != attackingWithLeft || this.attackType != attackType) return false;
            else return true;
        }

        private AttackType GetAttackType(bool isHeavy)
        {
            return (isHeavy) ? AttackType.heavy : AttackType.light;
        }

        private string GetAttackAnimationString(WeaponItem weapon, bool isHeavy)
        {
            string animation;

            string[] animationArray;
            if (!isHeavy) animationArray = weapon.OneHandedLightAttackCombo;
            else animationArray = weapon.OneHandedHeavyAttackCombo;

            animation = animationArray[nextComboAttackIndex++];
            nextComboAttackIndex %= animationArray.Length;

            if (animation == "") animation = GetAttackAnimationString(weapon, isHeavy);

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
