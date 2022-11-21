using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerWeaponManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private PlayerStatsManager statsManager;
        private InputManager inputManager;
        private PlayerLocomotionManager locomotionManager;
        private AnimationManager animationManager;
        private PlayerInventoryManager inventoryManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerCombatManager combatManager;

        private bool attackingWithLeft = false;
        private AttackType attackType;
        private int nextComboAttackIndex = 0;
        private bool chainedAttack = false;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            statsManager = playerManager.GetStatsManager();
            inputManager = playerManager.GetInputManager();
            locomotionManager = playerManager.GetLocomotionManager();
            animationManager = playerManager.GetAnimationManager();
            inventoryManager = playerManager.GetInventoryManager();
            ragdollManager = playerManager.GetRagdollManager();
            combatManager = playerManager.GetCombatManager();
        }

        public void HandleAttacks()
        {

            HandleRollAndBackdashAttackTimers();

            //Store presses
            bool rb = inputManager.rbInputPressed;
            bool rt = inputManager.rtInputPressed;
            bool lb = inputManager.lbInputPressed;
            bool lt = inputManager.ltInputPressed;

            //Define if the attack is left handed and if its heavy
            bool isLeft = (lb || lt);
            bool isHeavy = (lt || rt);

            //If it's not a weapon
            if (inventoryManager.GetCurrentItemType(isLeft) != PlayerInventoryManager.ItemType.weapon) return;

            //If any of these is pressed
            if (rb || rt || lb || lt)
            {
                //If no stamina
                if (statsManager.currentStamina < 1) return;

                //Check for combos
                AttackType attackType = GetAttackType(isHeavy);
                chainedAttack = CheckForCombo(isLeft, attackType);
                //Try to attack
                TryAttack(isLeft, attackType);
            }

        }

        private void TryAttack(bool isLeft, AttackType newAttackType)
        {
            if (playerManager.playerIsStuckInAnimation) return;

            //Get right or left item
            HandEquippableItem item = (isLeft)? inventoryManager.GetCurrentItem(true) : inventoryManager.GetCurrentItem(false);

            //Get this new attack's proprieties
            bool newAttackingWithLeft = isLeft;

            //If an attack was NOT chained and it is not the first attack
            if (!chainedAttack && nextComboAttackIndex != 0) return;

            //Update proprieties
            attackingWithLeft = newAttackingWithLeft;
            attackType = newAttackType;
            animationManager.UpdateAttackingWithLeftValue(attackingWithLeft);

            if(CheckForBackstab(attackType))
            {
                //Do backstab
                Debug.Log("Backstab");
                return;
            }

            //Get animation to play and movement speed multiplier
            string attackAnimation = GetAttackAnimationString((WeaponItem)item, attackType);
            float attackMovementSpeedMultiplier = GetAttackMovementSpeedMultiplier((WeaponItem)item);
            float staminaCost = GetAttackStaminaCost(attackType, (WeaponItem)item);

            //Attack
            Attack(attackAnimation, attackMovementSpeedMultiplier, staminaCost);

        }

        private bool CheckForBackstab(AttackType attackType)
        {
            if(attackType != AttackType.light) return false;
            return false;
        }

        private void Attack(string attackAnimation, float attackMovementSpeedMultiplier, float staminaCost)
        {

            //Play animation
            animationManager.PlayTargetAnimation(attackAnimation, .2f, true);

            //Set speed multiplier
            locomotionManager.SetMovementSpeedMultiplier(attackMovementSpeedMultiplier);
            //if (chainedAttack) Debug.Log("Combo: " + attackAnimation);

            //Disable arms collision
            ragdollManager.ToggleCollisionOfArms(false);
            ragdollManager.ToggleCollisionOfArmsServerRpc(false);

            //Consume stamina
            statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);
        }

        private float GetAttackMovementSpeedMultiplier(WeaponItem weapon)
        {
            float multiplier = weapon.movementSpeedMultiplier;
            return multiplier;
        }

        private bool CheckForCombo(bool isLeft, AttackType attackType)
        {
            if (nextComboAttackIndex == 0) return false;

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

        private float GetAttackStaminaCost(AttackType attackType, WeaponItem weapon)
        {
            switch(attackType)
            {
                case AttackType.light: return weapon.lightAttackStaminaUse;
                case AttackType.heavy: return weapon.heavyAttackStaminaUse;
                case AttackType.running: return weapon.runningAttackStaminaUse;
                case AttackType.rolling: return weapon.rollingAttackStaminaUse;

                default: return weapon.lightAttackStaminaUse;
            }
        }

        #region Rolling and Backdashing attack timers handling
        private bool previousIsRolling = false; //If the player was rolling the previous frame
        private bool previousIsBackdashing = false; //If the player was backdashing the previous frame
        private float rollingAttackTimer = 0; //The window to perform a rolling attack
        private float backdashingAttackTimer = 0; //The window to perform a backdashing attack
        #endregion
        private void HandleRollAndBackdashAttackTimers()
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

        public bool IsAttackingWithLeft()
        {
            return attackingWithLeft;
        }

        public void ResetCombo()
        {
            if (!chainedAttack) //Reset combo if player has not chained an attack
            {
                nextComboAttackIndex = 0;
                //Enable arms collision
                playerManager.GetRagdollManager().ToggleCollisionOfArms(true);
                playerManager.GetRagdollManager().ToggleCollisionOfArmsServerRpc(true);
            }
            else
            {
                //Continue attacking
                playerManager.isAttacking = true;
                playerManager.playerIsStuckInAnimation = true;
            }

            //Reset
            chainedAttack = false; //Set chaining to false so the player has to press again
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
