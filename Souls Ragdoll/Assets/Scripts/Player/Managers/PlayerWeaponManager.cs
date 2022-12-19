using Animancer;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
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
        private PlayerNetworkManager networkManager;

        [SerializeField] private LayerMask backstabLayer;
        [SerializeField] private LayerMask riposteLayer;

        private Rigidbody physicalHips;

        private AttackType attackType;
        private int nextComboMoveIndex = 0;

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
            networkManager = playerManager.GetNetworkManager();

            physicalHips = playerManager.GetPhysicalHips();
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

                //Try to attack
                TryAttack(newAttackType);
            }

        }

        private void TryAttack(AttackType newAttackType)
        {
            if (playerManager.isStuckInAnimation) return;

            //Get right or left item
            WeaponItem weapon = (WeaponItem)inventoryManager.GetCurrentItem(false);

            //Check for backstab, if a backstab goes through, then return
            if (TryBackstab(newAttackType, weapon)) return;

            //Check for riposte, if a riposte goes through, then return
            if (TryRiposte(newAttackType, weapon)) return;

            //Attack
            Attack(newAttackType);
            networkManager.AttackServerRpc(newAttackType);

        }

        public void Attack(AttackType newAttackType)
        {
            //Get right or left item
            WeaponItem weapon = (WeaponItem)inventoryManager.GetCurrentItem(false);
            if (weapon == null) return;

            AttackMove attackMove = GetAttackMove(weapon, newAttackType);
            if (attackMove == null) return;

            //Update proprieties
            attackType = newAttackType;

            //Get animation to play and movement speed multiplier
            string attackAnimationName = attackMove.animationName;
            if (attackAnimationName == "") return;

            //Create enter and exit events
            Action onAttackEnterAction = () =>
            {
                //Debug.Log("Attack enter");
                playerManager.isStuckInAnimation = true;
                playerManager.isInOverrideAnimation = true;
                playerManager.shouldSlide = true;
                playerManager.isAttacking = true;

                //Set movement speed multiplier
                locomotionManager.SetMovementSpeedMultiplier(attackMove.movementSpeedMultiplier);

                //Consume stamina
                float staminaCost = weapon.baseStaminaCost * attackMove.staminaCostMultiplier;
                statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);

                //Set up collider
                AttackColliderSetup(weapon, attackMove);

                //Disable rotation
                StartCoroutine(DisablePlayerRotationAfterAttackStart(attackMove.timeToRotateAfterAttackStart));
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
            animationManager.PlayOverrideAnimation(attackAnimationName, attackMove.speed, onAttackEnterAction, onAttackExitAction);
        }

        private IEnumerator DisablePlayerRotationAfterAttackStart(float time)
        {
            yield return new WaitForSeconds(time);

            //If the player is still attacking (was not interrupted)
            if (playerManager.isAttacking) playerManager.canRotate = false;
        }

        private void AttackColliderSetup(WeaponItem weapon, AttackMove attackMove)
        {
            //Get weapon values
            float damageMultiplier = attackMove.damageMultiplier;
            float poiseDamageMultiplier = attackMove.poiseDamageMultiplier;
            float staminaDamageMultiplier = attackMove.staminaDamageMultiplier;
            float knockbackStrengthMultiplier = attackMove.knockbackStrengthMultiplier;
            float flinchStrengthMultiplier = attackMove.flinchStrengthMultiplier;

            int damage = (int)(weapon.baseDamage * damageMultiplier);
            int poiseDamage = (int)(weapon.poiseBaseDamage * poiseDamageMultiplier);
            int staminaDamage = (int)(weapon.staminaBaseDamage * staminaDamageMultiplier);

            float knockbackStrength = weapon.baseKnockbackStrength * knockbackStrengthMultiplier;
            float flinchStrength = weapon.baseFlinchStrength * flinchStrengthMultiplier;

            int attackDeflectionLevel = attackMove.levelNeededToDeflect;

            //Stagger animation in case of poise break
            string staggerAnimation = attackMove.victimStaggerAnimation;


            DamageColliderInfo colliderInfo = new DamageColliderInfo
            {
                //Create collider info
                damage = damage,
                poiseDamage = poiseDamage,
                staminaDamage = staminaDamage,
                knockbackStrength = knockbackStrength,
                flinchStrenght = flinchStrength,

                staggerAnimation = staggerAnimation,
                attackDeflectionLevel = attackDeflectionLevel
            };

            //Set collider values
            inventoryManager.SetDamageColliderValues(colliderInfo);
        }

        private bool TryBackstab(AttackType attackType, WeaponItem weapon)
        {
            if (attackType != AttackType.light || nextComboMoveIndex != 0) return false;

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

                    AttackMove attackMove = GetAttackMove(weapon, this.attackType);
                    if (attackMove == null) return false;

                    //Damage and stamina
                    float damage = weapon.baseDamage * attackMove.damageMultiplier;
                    float staminaCost = weapon.baseStaminaCost * attackMove.staminaCostMultiplier;

                    //Animations
                    string backstabAnimation = attackMove.animationName;
                    string backstabbedAnimation = attackMove.victimStaggerAnimation;

                    //Consume stamina
                    statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);

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
            if (attackType != AttackType.light || nextComboMoveIndex != 0) return false;

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

                    AttackMove attackMove = GetAttackMove(weapon, this.attackType);
                    if (attackMove == null) return false;

                    //Damage and stamina
                    float damage = weapon.baseDamage * attackMove.damageMultiplier;
                    float staminaCost = weapon.baseStaminaCost * attackMove.staminaCostMultiplier;

                    //Animations
                    string riposteAnimation = attackMove.animationName;
                    string ripostedAnimation = attackMove.victimStaggerAnimation;

                    //Consume stamina
                    statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);

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

        public void AttackDeflected()
        {
            //Create enter and exit events
            Action onAttackDeflectedEnterAction = () =>
            {
                //Debug.Log("Attack deflected enter");
                playerManager.isStuckInAnimation = true;
                playerManager.isInOverrideAnimation = true;
                playerManager.shouldSlide = false;
                playerManager.canRotate = false;

                //Disable attack collider
                inventoryManager.GetCurrentItemDamageColliderControl(false).ToggleCollider(false);
            };

            Action onAttackDeflectedExitAction = () =>
            {
                //Debug.Log("Attack deflected exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                animationManager.FadeOutOverrideAnimation(.15f);
            };

            animationManager.PlayOverrideAnimation("AttackBounceRight", onAttackDeflectedEnterAction, onAttackDeflectedExitAction);
        }

        public void WallBounce()
        {
            //Create enter and exit events
            Action onWallBounceEnterAction = () =>
            {
                //Debug.Log("Wall bounce enter");
                playerManager.isStuckInAnimation = true;
                playerManager.isInOverrideAnimation = true;
                playerManager.shouldSlide = false;
                playerManager.canRotate = false;

                //Disable attack collider
                inventoryManager.GetCurrentItemDamageColliderControl(false).ToggleCollider(false);
            };

            Action onWallBounceExitAction = () =>
            {
                //Debug.Log("Wall bounce exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                animationManager.FadeOutOverrideAnimation(.15f);
            };

            animationManager.PlayOverrideAnimation("AttackBounceRight", 1.2f, onWallBounceEnterAction, onWallBounceExitAction);
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

        private AttackMove GetAttackMove(WeaponItem weapon, AttackType newAttackType)
        {

            if (!DoesCombo(newAttackType)) nextComboMoveIndex = 0;

            AttackMove[] comboArray;
            switch (newAttackType)
            {
                case AttackType.light: comboArray = weapon.moveset.oneHandedLightCombo; break;
                case AttackType.heavy: comboArray = weapon.moveset.oneHandedHeavyCombo; break;
                case AttackType.running: return weapon.moveset.oneHandedRunningAttack;
                case AttackType.rolling: return weapon.moveset.oneHandedRollingAttack;
                case AttackType.backstab: return weapon.moveset.oneHandedBackstabAttack;
                case AttackType.riposte: return weapon.moveset.oneHandedRiposteAttack;
                default: comboArray = weapon.moveset.oneHandedLightCombo; break;
            }

            if (IsComboArrayEmpty(comboArray)) return null;

            AttackMove attackMove = comboArray[nextComboMoveIndex++];
            nextComboMoveIndex %= comboArray.Length;

            if(attackMove == null || attackMove.animationName == "") GetAttackMove(weapon, attackType);

            return attackMove;

        }

        private bool DoesCombo(AttackType newAttackType)
        {
            return (newAttackType == attackType);
        }

        private bool IsComboArrayEmpty(AttackMove[] combo)
        {
            foreach(AttackMove move in combo)
            {
                if (move != null) return false;
            }

            return true;
        }

        private AttackType GetAttackType(bool isHeavy)
        {
            if (playerManager.isSprinting && !isHeavy) return AttackType.running;
            if((playerManager.IsRolling || rollingAttackTimer > 0) && !isHeavy) return AttackType.rolling;
            if((playerManager.isBackdashing || backdashingAttackTimer > 0) && !isHeavy) return AttackType.running;

            return (isHeavy) ? AttackType.heavy : AttackType.light;
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

        public IEnumerator ResetCombo()
        {
            //Wait for next frame to see if the player is attacking again (is comboing)
            yield return new WaitForFixedUpdate();

            //If it's not, reset
            if (!playerManager.isAttacking) nextComboMoveIndex = 0;
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
