using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        [SerializeField] private LayerMask backstabLayer;

        private Rigidbody physicalHips;
        private GameObject animatedPlayer;

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

            physicalHips = playerManager.GetPhysicalHips();
            animatedPlayer = playerManager.GetAnimatedPlayer();
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
            WeaponItem weapon = (WeaponItem)((isLeft)? inventoryManager.GetCurrentItem(true) : inventoryManager.GetCurrentItem(false));

            //Get this new attack's proprieties
            bool newAttackingWithLeft = isLeft;

            //If an attack was NOT chained and it is not the first attack
            if (!chainedAttack && nextComboAttackIndex != 0) return;

            //Update proprieties
            attackingWithLeft = newAttackingWithLeft;
            attackType = newAttackType;
            animationManager.UpdateAttackingWithLeftValue(attackingWithLeft);

            //Check for backstab, if the backstab goes through, then return
            if(TryBackstab(attackType, weapon)) return;

            //Get animation to play and movement speed multiplier
            string attackAnimation = GetAttackAnimationString(weapon, attackType);
            float attackMovementSpeedMultiplier = GetAttackMovementSpeedMultiplier(weapon);
            float staminaCost = GetAttackStaminaCost(attackType, weapon);

            //Set collider values
            int damage = (int)(weapon.baseDamage * GetWeaponDamageMultiplier(weapon));
            float knockbackStrength = weapon.knockbackStrength;
            float flinchStrength = weapon.flinchStrength;
            inventoryManager.SetColliderValues(damage, knockbackStrength, flinchStrength, attackingWithLeft);


            //Attack
            Attack(attackAnimation, attackMovementSpeedMultiplier, staminaCost);

        }

        private bool TryBackstab(AttackType attackType, WeaponItem weapon)
        {
            if(attackType != AttackType.light) return false;

            RaycastHit hit;
            float backstabDistance = 1.4f;
            if(Physics.Raycast(physicalHips.transform.position, physicalHips.transform.forward, out hit, backstabDistance, backstabLayer))
            {
                PlayerManager victimManager = hit.collider.GetComponentInParent<PlayerManager>();

                if (victimManager == null) return false;

                Rigidbody victimHips = victimManager.GetPhysicalHips();

                Vector3 hitDirection = (victimHips.transform.position - physicalHips.transform.position).normalized;
                float hitAngle = Vector3.Angle(Vector3.ProjectOnPlane(hitDirection, Vector3.up), Vector3.ProjectOnPlane(victimHips.transform.forward, Vector3.up));

                if (hitAngle < 25f) Backstab(victimManager, weapon);
                else return false;

                return true;
            }

            return false;
        }

        private void Backstab(PlayerManager victim, WeaponItem weapon)
        {
            //Set proprieties
            animationManager.UpdateAttackingWithLeftValue(attackingWithLeft);
            attackType = AttackType.backstab;

            //Disable arms collision
            ragdollManager.ToggleCollisionOfArms(false);
            ragdollManager.ToggleCollisionOfArmsServerRpc(false);

            //Play backstab animation
            animationManager.PlayTargetAnimation(weapon.backstabAttack, .1f, true);

            //Stop player
            locomotionManager.SetMovementSpeedMultiplier(1);

            //Damage
            float damage = weapon.baseDamage;
            damage *= GetWeaponDamageMultiplier(weapon);

            //Stamina
            float staminaCost = weapon.backstabAttackStaminaUse;

            //Send backstab to victim
            Vector3 backstabbedPosition = playerManager.backstabbedTransform.position;
            Quaternion backstabbedRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(playerManager.backstabbedTransform.forward, Vector3.up));
            victim.GetCombatManager().GotBackstabbedServerRpc(backstabbedPosition, backstabbedRotation, weapon.backstabVictimAnimation, damage, victim.OwnerClientId);

            //Consume stamina
            statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);

        }

        private void Attack(string attackAnimation, float attackMovementSpeedMultiplier, float staminaCost)
        {
            if (attackAnimation == "") return;

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

        private float GetWeaponDamageMultiplier(WeaponItem weapon)
        {

            switch (this.attackType)
            {
                case AttackType.light: return weapon.oneHandedLightAttacksDamageMultiplier;
                case AttackType.heavy: return weapon.oneHandedHeavyAttacksDamageMultiplier;
                case AttackType.running: return weapon.oneHandedRunningAttackDamageMultiplier;
                case AttackType.rolling: return weapon.oneHandedRollingAttackDamageMultiplier;
                case AttackType.backstab: return weapon.backstabtAttackDamageMultiplier;
                default: return 1f;
            }
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
                case AttackType.light: animationArray = weapon.oneHandedLightAttackCombo; break;
                case AttackType.heavy: animationArray = weapon.OneHandedHeavyAttackCombo; break;
                case AttackType.running: return weapon.oneHandedRunningAttack;
                case AttackType.rolling: return weapon.oneHandedRollingAttack;
                case AttackType.backstab: return weapon.backstabAttack;
                default: animationArray = weapon.oneHandedLightAttackCombo; break;
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
            rolling,
            backstab
        }

    }
}
