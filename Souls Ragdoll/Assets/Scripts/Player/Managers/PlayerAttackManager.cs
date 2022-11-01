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

        public void HandleAttacks()
        {

            HandleRollingAndDashingAttacks();

            //If player is already attacking and can not combo into next attack, return
            if (playerManager.disablePlayerInteraction && !canCombo) return;

            //Store presses
            bool rb = playerManager.inputManager.rbInputPressed;
            bool rt = playerManager.inputManager.rtInputPressed;
            bool lb = playerManager.inputManager.lbInputPressed;
            bool lt = playerManager.inputManager.ltInputPressed;

            //Define if the attack is left handed and if its heavy
            bool isLeft = (lb || lt);
            bool isHeavy = (lt || rt);

            //Try to attack if any one of the buttons is pressed
            if(rb || rt || lb || lt) TryAttack(isLeft, isHeavy);

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
            playerManager.animationManager.animator.SetBool("attackingWithLeft", attackingWithLeft);

            //Get animation to play
            string attackAnimation = GetAttackAnimationString((WeaponItem)item, attackType);

            //Play animation
            playerManager.animationManager.PlayTargetAnimation(attackAnimation, .2f);
            if (chainedAttack) Debug.Log("Combo: " + attackAnimation);

            //Disable combo until it is opened again in the animation events
            canCombo = false;
        }

        private bool CheckForCombo(bool isLeft, AttackType attackType)
        {
            //Cases where the combo is not performed
            if (isLeft != attackingWithLeft || this.attackType != attackType) return false;
            else return true;
        }

        private AttackType GetAttackType(bool isHeavy)
        {
            if (playerManager.isSprinting && !isHeavy) return AttackType.running;
            if(rollingAttackTimer > 0 && !isHeavy) return AttackType.rolling;
            if(backdashingAttackTimer > 0 && !isHeavy) return AttackType.running;

            return (isHeavy) ? AttackType.heavy : AttackType.light;
        }

        private bool previousIsRolling = false; //If the player was rolling the previous frame
        private bool previousIsBackdashing = false; //If the player was backdashing the previous frame
        private float rollingAttackTimer = 0; //The window to perform a rolling attack
        private float backdashingAttackTimer = 0; //The window to perform a backdashing attack
        private void HandleRollingAndDashingAttacks()
        {
            //Just finished rolling
            if(!playerManager.isRolling && previousIsRolling) rollingAttackTimer = playerManager.playerData.rollingAttackWindow;

            //Just finished backdashing
            if (!playerManager.isBackdashing && previousIsBackdashing) backdashingAttackTimer = playerManager.playerData.backdashingAttackWindow;

            if (rollingAttackTimer > 0) rollingAttackTimer -= Time.deltaTime;
            if (backdashingAttackTimer > 0) backdashingAttackTimer -= Time.deltaTime;

            //Update values
            previousIsRolling = playerManager.isRolling;
            previousIsBackdashing = playerManager.isBackdashing;
        }

        private string GetAttackAnimationString(WeaponItem weapon, AttackType attackType)
        {
            string animation;

            string[] animationArray;

            switch(this.attackType)
            {
                case AttackType.light: animationArray = weapon.OneHandedLightAttackCombo; break;
                case AttackType.heavy: animationArray = weapon.OneHandedHeavyAttackCombo; break;
                case AttackType.running: return weapon.OneHandedRunningAttack;
                case AttackType.rolling: return weapon.OneHandedRollingAttack;
                default: animationArray = weapon.OneHandedLightAttackCombo; break;
            }

            if (IsArrayEmpty(animationArray)) return "";

            animation = animationArray[nextComboAttackIndex++];
            nextComboAttackIndex %= animationArray.Length;

            if (animation == "") animation = GetAttackAnimationString(weapon, attackType);

            return animation;
        }

        private bool IsArrayEmpty(string[] array)
        {
            if(array == null || array.Length == 0) return true;

            foreach(string s in array)
            {
                if (s != "") return false;
            }

            return true;
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
