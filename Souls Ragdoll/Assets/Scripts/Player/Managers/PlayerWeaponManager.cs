using Animancer;
using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
        private PlayerNetworkManager networkManager;

        [SerializeField] private LayerMask backstabLayer;
        [SerializeField] private LayerMask riposteLayer;

        private Rigidbody physicalHips;

        private AttackType attackType;
        private int nextComboAttackIndex = 0;

        #region Rolling and Backdashing attack timers handling
        private float rollingAttackTimer = 0; //The window to perform a rolling attack
        private float backdashingAttackTimer = 0; //The window to perform a backdashing attack
        #endregion

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            statsManager = playerManager.GetStatsManager();
            inputManager = playerManager.GetInputManager();
            locomotionManager = playerManager.GetLocomotionManager();
            animationManager = playerManager.GetAnimationManager();
            inventoryManager = playerManager.GetInventoryManager();
            ragdollManager = playerManager.GetRagdollManager();
            networkManager = playerManager.GetNetworkManager();

            physicalHips = playerManager.GetPhysicalHips();
            //animatedPlayer = playerManager.GetAnimatedPlayer();
        }

        public void HandleAttacks()
        {

            HandleRollAndBackdashAttackTimers();

            //Store presses
            bool rb = inputManager.rbInputPressed;
            bool rt = inputManager.rtInputPressed;

            //Define if attack is heavy
            bool isHeavy = rt;

            //If it's not a weapon
            if (inventoryManager.GetCurrentItemType(false) != PlayerInventoryManager.ItemType.weapon) return;

            //If any of these is pressed
            if (rb || rt)
            {
                //If no stamina
                if (statsManager.CurrentStamina < 1) return;

                AttackType newAttackType = GetAttackType(isHeavy);

                //Reset combo counter if the attack type is not the same as the old one
                if (attackType != newAttackType) nextComboAttackIndex = 0;

                //Try to attack
                TryAttack(newAttackType);
            }

        }

        private void TryAttack(AttackType newAttackType)
        {
            if (playerManager.isStuckInAnimation) return;

            //Get right or left item
            WeaponItem weapon = (WeaponItem)inventoryManager.GetCurrentItem(false);

            //Update proprieties
            attackType = newAttackType;

            //Check for backstab, if a backstab goes through, then return
            if(TryBackstab(attackType, weapon)) return;

            //Check for riposte, if a riposte goes through, then return
            if (TryRiposte(attackType, weapon)) return;

            //Get weapon values
            float damageMultiplier = GetWeaponDamageMultiplier(weapon);
            float poiseDamageMultiplier = GetWeaponPoiseDamageMultiplier(weapon);

            int damage = (int)(weapon.baseDamage * damageMultiplier);
            int poiseDamage = (int)(weapon.poiseBaseDamage * poiseDamageMultiplier);
            int staminaDamage = (int)(weapon.staminaBaseDamage * damageMultiplier);

            float knockbackStrength = weapon.knockbackStrength;

            //Get animation to play and movement speed multiplier
            string attackAnimationName = GetAttackAnimationName(weapon, attackType);

            //Attack
            Attack(attackAnimationName, damage, poiseDamage, staminaDamage, knockbackStrength);
            networkManager.AttackServerRpc(attackAnimationName, damage, poiseDamage, staminaDamage, knockbackStrength);

            //If the attack animation exists
            if(attackAnimationName != "")
            {
                //Set attack speed multiplier
                float attackMovementSpeedMultiplier = GetAttackMovementSpeedMultiplier(weapon);
                locomotionManager.SetMovementSpeedMultiplier(attackMovementSpeedMultiplier);

                //Consume stamina
                float staminaCost = GetAttackStaminaCost(attackType, weapon);
                statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);
            }

        }

        public void Attack(string attackAnimationName, int damage, int poiseDamage, int staminaDamage, float knockbackStrength)
        {
            if (attackAnimationName == "") return;

            //Create enter and exit events
            Action onAttackEnterAction = () =>
            {
                //Debug.Log("Attack enter");
                playerManager.isStuckInAnimation = true;
                playerManager.isInOverrideAnimation = true;
                playerManager.shouldSlide = true;
                playerManager.isAttacking = true;
            };

            Action onAttackExitAction = () =>
            {
                //Debug.Log("Attack exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                playerManager.isAttacking = false;
                StartCoroutine(ResetCombo());
                animationManager.FadeOutOverrideAnimation(.15f);

                //Close weapon collider and parriable if the animation was interrupted mid attack
                inventoryManager.GetCurrentItemDamageColliderControl(false).ToggleCollider(false);
                inventoryManager.GetCurrentItemDamageColliderControl(false).ToggleParriable(false);
            };

            //Play animation
            animationManager.PlayOverrideAnimation(attackAnimationName, onAttackEnterAction, onAttackExitAction);

            //Set collider values
            inventoryManager.SetDamageColliderValues(damage, poiseDamage, staminaDamage, knockbackStrength);
        }

        private bool TryBackstab(AttackType attackType, WeaponItem weapon)
        {
            if (attackType != AttackType.light || nextComboAttackIndex != 0) return false;

            RaycastHit hit;
            float backstabDistance = 1.4f;
            float backstabAngle = 36f;

            if (Physics.Raycast(physicalHips.transform.position, physicalHips.transform.forward, out hit, backstabDistance, backstabLayer))
            {
                PlayerManager victimManager = hit.collider.GetComponentInParent<PlayerManager>();

                //If somehow the ray hit yourself
                if (playerManager == victimManager) return false;

                if (victimManager == null || !victimManager.canBeBackstabbed || victimManager.areIFramesActive) return false;

                Rigidbody victimHips = victimManager.GetPhysicalHips();

                Vector3 hitDirection = (victimHips.transform.position - physicalHips.transform.position).normalized;
                float hitAngle = Vector3.Angle(Vector3.ProjectOnPlane(hitDirection, Vector3.up), Vector3.ProjectOnPlane(victimHips.transform.forward, Vector3.up));

                if (hitAngle < backstabAngle)
                {
                    //Set proprieties
                    this.attackType = AttackType.backstab;

                    //Damage
                    float damage = weapon.baseDamage;
                    damage *= GetWeaponDamageMultiplier(weapon);

                    //Animations
                    string backstabAnimation = weapon.backstabAttackName;
                    string backstabbedAnimation = weapon.backstabVictimAnimation;

                    //Consume stamina
                    statsManager.ConsumeStamina(weapon.backstabAttackStaminaUse, statsManager.playerStats.staminaDefaultRecoveryTime);

                    //Play backstab
                    Riposte(backstabAnimation);
                    networkManager.RiposteServerRpc(backstabAnimation);

                    //Get position and rotation for the victim
                    Vector3 backstabbedPosition = playerManager.backstabbedTransform.position;
                    Quaternion backstabbedRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(playerManager.backstabbedTransform.forward, Vector3.up));

                    //Position victim and play animation
                    victimManager.GetWeaponManager().Riposted(backstabbedPosition, backstabbedRotation, backstabbedAnimation, damage);
                    victimManager.GetNetworkManager().RipostedServerRpc(backstabbedPosition, backstabbedRotation, backstabbedAnimation, damage, victimManager.OwnerClientId);
                }
                else return false;

                return true;
            }

            return false;
        }

        private bool TryRiposte(AttackType attackType, WeaponItem weapon)
        {
            if (attackType != AttackType.light || nextComboAttackIndex != 0) return false;

            RaycastHit hit;
            float riposteDistance = 1.4f;
            float riposteAngle = 115f;

            if (Physics.Raycast(physicalHips.transform.position, physicalHips.transform.forward, out hit, riposteDistance, riposteLayer))
            {
                PlayerManager victimManager = hit.collider.GetComponentInParent<PlayerManager>();

                //If somehow the ray hit yourself
                if (playerManager == victimManager) return false;

                if (victimManager == null || !victimManager.canBeRiposted || victimManager.areIFramesActive) return false;

                Rigidbody victimHips = victimManager.GetPhysicalHips();

                Vector3 hitDirection = (victimHips.transform.position - physicalHips.transform.position).normalized;
                float hitAngle = Vector3.Angle(Vector3.ProjectOnPlane(hitDirection, Vector3.up), Vector3.ProjectOnPlane(victimManager.GetCombatManager().forwardWhenParried, Vector3.up));

                if (hitAngle > riposteAngle)
                {
                    //Set proprieties
                    this.attackType = AttackType.riposte;

                    //Damage
                    float damage = weapon.baseDamage;
                    damage *= GetWeaponDamageMultiplier(weapon);

                    //Animations
                    string riposteAnimation = weapon.riposteAttackName;
                    string ripostedAnimation = weapon.riposteVictimAnimation;

                    //Consume stamina
                    statsManager.ConsumeStamina(weapon.riposteAttackStaminaUse, statsManager.playerStats.staminaDefaultRecoveryTime);

                    //Play riposte
                    Riposte(riposteAnimation);
                    networkManager.RiposteServerRpc(riposteAnimation);

                    //Get position and rotation for the victim
                    Vector3 ripostedPosition = playerManager.ripostedTransform.position;
                    Quaternion ripostedRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(playerManager.ripostedTransform.forward, Vector3.up));

                    //Position victim and play animation
                    victimManager.GetWeaponManager().Riposted(ripostedPosition, ripostedRotation, ripostedAnimation, damage);
                    victimManager.GetNetworkManager().RipostedServerRpc(ripostedPosition, ripostedRotation, ripostedAnimation, damage, victimManager.OwnerClientId);
                }
                else return false;

                return true;
            }

            return false;
        }

        public void Riposte(string riposteAnimation)
        {

            //Create enter and exit events
            Action onRiposteEnterAction = () =>
            {
                //Debug.Log("Riposte enter");
                playerManager.isStuckInAnimation = true;
                playerManager.isInOverrideAnimation = true;
                playerManager.shouldSlide = false;
                playerManager.canRotate = false;
                playerManager.isAttacking = true;
                playerManager.areIFramesActive = true;
            };

            Action onRiposteExitAction = () =>
            {
                //Debug.Log("Riposte exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                playerManager.isAttacking = false;
                playerManager.areIFramesActive = false;
                animationManager.FadeOutOverrideAnimation(.15f);
            };

            //Play riposte animation
            animationManager.PlayOverrideAnimation(riposteAnimation, onRiposteEnterAction, onRiposteExitAction);

            //Stop player
            locomotionManager.SetMovementSpeedMultiplier(1);

        }

        public void Parried()
        {
            //Create enter and exit events
            Action onParriedEnterAction = () =>
            {
                //Debug.Log("Parried enter");
                playerManager.isStuckInAnimation = true;
                playerManager.isInOverrideAnimation = true;
                playerManager.shouldSlide = false;
                playerManager.canRotate = false;
                playerManager.canBeRiposted = true;

                //Disable attack collider
                inventoryManager.GetCurrentItemDamageColliderControl(false).ToggleCollider(false);

                //Set forward when parried
                playerManager.GetCombatManager().forwardWhenParried = physicalHips.transform.forward;
            };

            Action onParriedExitAction = () =>
            {
                //Debug.Log("Parried exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                playerManager.canBeRiposted = false;
                animationManager.FadeOutOverrideAnimation(.15f);
            };

            animationManager.PlayOverrideAnimation("Parried",onParriedEnterAction, onParriedExitAction);
        }

        public void Riposted(Vector3 riposteVictimPosition, Quaternion riposteVictimRotation, string riposteVictimAnimation, float damage)
        {
            //Create enter and exit events
            Action onRipostedEnterAction = () =>
            {
                //Debug.Log("Riposted enter");
                playerManager.isStuckInAnimation = true;
                playerManager.isInOverrideAnimation = true;
                playerManager.shouldSlide = false;
                playerManager.canRotate = false;
                playerManager.areIFramesActive = true;
            };

            Action onRipostedExitAction = () =>
            {
                //Debug.Log("Riposted exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                playerManager.areIFramesActive = false;
                animationManager.FadeOutOverrideAnimation(.15f);
            };

            //Play animation
            animationManager.PlayOverrideAnimation(riposteVictimAnimation, onRipostedEnterAction, onRipostedExitAction);

            //Stop player
            playerManager.GetLocomotionManager().SetMovementSpeedMultiplier(1);

            //Don't continue if not in the client side
            if (!playerManager.IsOwner) return;

            //Position player
            playerManager.GetPhysicalHips().transform.position = riposteVictimPosition;
            playerManager.GetAnimatedPlayer().transform.rotation = riposteVictimRotation;

            //Take damage
            StartCoroutine(playerManager.GetStatsManager().TakeCriticalDamage((int)damage, .5f));

            //Disable riposte
            playerManager.canBeRiposted = false;
        }

        public float GetWeaponDamageMultiplier(WeaponItem weapon)
        {
            switch (this.attackType)
            {
                case AttackType.light: return weapon.oneHandedLightAttacksDamageMultiplier;
                case AttackType.heavy: return weapon.oneHandedHeavyAttacksDamageMultiplier;
                case AttackType.running: return weapon.oneHandedRunningAttackDamageMultiplier;
                case AttackType.rolling: return weapon.oneHandedRollingAttackDamageMultiplier;
                case AttackType.backstab: return weapon.backstabtAttackDamageMultiplier;
                case AttackType.riposte: return weapon.ripostetAttackDamageMultiplier;
                default: return 1f;
            }
        }

        public float GetWeaponPoiseDamageMultiplier(WeaponItem weapon)
        {
            switch (this.attackType)
            {
                case AttackType.light: return weapon.oneHandedLightAttacksPoiseDamageMultiplier;
                case AttackType.heavy: return weapon.oneHandedHeavyAttacksPoiseDamageMultiplier;
                case AttackType.running: return weapon.oneHandedRunningAttackPoiseDamageMultiplier;
                case AttackType.rolling: return weapon.oneHandedRollingAttackPoiseDamageMultiplier;
                default: return 1f;
            }
        }

        private float GetAttackMovementSpeedMultiplier(WeaponItem weapon)
        {
            float multiplier = weapon.movementSpeedMultiplier;
            return multiplier;
        }

        private AttackType GetAttackType(bool isHeavy)
        {
            if (playerManager.isSprinting && !isHeavy) return AttackType.running;
            if((playerManager.IsRolling || rollingAttackTimer > 0) && !isHeavy) return AttackType.rolling;
            if((playerManager.isBackdashing || backdashingAttackTimer > 0) && !isHeavy) return AttackType.running;

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

        private void HandleRollAndBackdashAttackTimers()
        {
            //Just rolled
            if(playerManager.IsRolling) rollingAttackTimer = playerManager.playerData.rollingAttackWindow;

            //Just backdashed
            if (playerManager.isBackdashing) backdashingAttackTimer = playerManager.playerData.backdashingAttackWindow;

            if (rollingAttackTimer > 0) rollingAttackTimer -= Time.deltaTime;
            if (backdashingAttackTimer > 0) backdashingAttackTimer -= Time.deltaTime;
        }

        private string GetAttackAnimationName(WeaponItem weapon, AttackType attackType)
        {
            string animationName;

            string[] comboArray;

            switch(this.attackType)
            {
                case AttackType.light: comboArray = weapon.oneHandedLightAttackComboNames; break;
                case AttackType.heavy: comboArray = weapon.OneHandedHeavyAttackComboNames; break;
                case AttackType.running: return weapon.oneHandedRunningAttackName;
                case AttackType.rolling: return weapon.oneHandedRollingAttackName;
                case AttackType.backstab: return weapon.backstabAttackName;
                case AttackType.riposte: return weapon.riposteAttackName;
                default: comboArray = weapon.oneHandedLightAttackComboNames; break;
            }

            if (IsArrayEmpty(comboArray)) return "";

            animationName = comboArray[nextComboAttackIndex++];
            nextComboAttackIndex %= comboArray.Length;

            if (animationName == "") animationName = GetAttackAnimationName(weapon, attackType);

            return animationName;
        }

        private bool IsArrayEmpty(string[] array)
        {
            if(array == null || array.Length == 0) return true;

            foreach(string a in array)
            {
                if (a != "") return false;
            }

            return true;
        }

        public IEnumerator ResetCombo()
        {
            //Wait for next frame to see if the player is attacking again (is comboing)
            yield return new WaitForFixedUpdate();

            //If it's not, reset
            if (!playerManager.isAttacking) nextComboAttackIndex = 0;
        }

        public enum AttackType
        {
            light,
            heavy,
            running,
            rolling,
            backstab,
            riposte
        }

    }
}
