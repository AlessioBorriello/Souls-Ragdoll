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
        private PlayerCombatManager combatManager;

        [SerializeField] private LayerMask backstabLayer;
        [SerializeField] private LayerMask riposteLayer;

        [SerializeField] private AnimationData weaponWallBounceAnimationData;
        [SerializeField] private AnimationData parriedAnimationData;

        private Rigidbody physicalHips;

        private AttackType attackType;
        private bool attackingWithLeft = false;
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
            combatManager = playerManager.GetCombatManager();

            physicalHips = playerManager.GetPhysicalHips();
        }

        public void HandleAttacks()
        {

            HandleRollAndBackdashAttackTimers();

            //Store presses
            bool rb = inputManager.rbInputPressed;
            bool rt = inputManager.rtInputPressed;

            bool lb = inputManager.lbInputPressed;
            bool lt = inputManager.ltInputPressed;

            HandleAttacks(rb, rt, lb, lt);

        }

        private void HandleAttacks(bool rb, bool rt, bool lb, bool lt)
        {
            //Define if attack is heavy
            bool isHeavy = rt || lt;

            //Define if the attack is being done with the left hand
            bool isLeft = lb || lt;

            //If any of these is pressed
            if (rb || rt || lb || lt)
            {
                //If 2 handing, attack only with the right buttons
                if (combatManager.twoHanding && isLeft != false) return;

                //If no stamina
                if (statsManager.CurrentStamina < 1) return;

                AttackType newAttackType = GetAttackType(isHeavy);

                //Try to attack
                TryAttack(newAttackType, (!combatManager.twoHanding)? isLeft : combatManager.twoHandingLeft);
            }
        }

        private void TryAttack(AttackType newAttackType, bool isLeft)
        {
            if (playerManager.isStuckInAnimation) return;

            //Get used item
            HandEquippableItem itemUsed = inventoryManager.GetCurrentItem(isLeft);

            //If not 2 handing, allow attacking only with weapons
            if (!combatManager.twoHanding && itemUsed is not WeaponItem) return;

            //Check for backstabs and ripostes only if it's a weapon
            if (itemUsed is WeaponItem)
            {
                //Cast to weapon
                WeaponItem weapon = (WeaponItem)itemUsed;

                //Check for backstab, if a backstab goes through, then return
                if (TryBackstab(newAttackType, weapon, isLeft)) return;

                //Check for riposte, if a riposte goes through, then return
                if (TryRiposte(newAttackType, weapon, isLeft)) return;
            }

            //Attack
            Attack(newAttackType, isLeft);
            networkManager.AttackServerRpc(newAttackType, isLeft);

        }

        public void Attack(AttackType newAttackType, bool isLeft)
        {
            //Get used item
            HandEquippableItem itemUsed = inventoryManager.GetCurrentItem(isLeft);
            if (itemUsed == null) return;

            DamageColliderControl colliderControl = inventoryManager.GetCurrentItemDamageColliderControl(isLeft);

            //Debug.Log("Attacking with " + itemUsed.name + " from " + ((isLeft)? "left" : "right") + " side");

            //Get attack move from moveset if weapon, otherwise use generic attack
            AttackMove attackMove = ((itemUsed is WeaponItem weapon))? GetAttackMove(weapon, newAttackType, isLeft) : attackMove = itemUsed.genericAttackMove;

            if (attackMove == null) return;

            //Update proprieties
            attackType = newAttackType;
            attackingWithLeft = isLeft;

            //Create enter and exit events
            Action onAttackEnterAction = () =>
            {
                //Debug.Log("Attack enter");
                playerManager.isStuckInAnimation = true;
                playerManager.isInOverrideAnimation = true;
                playerManager.shouldSlide = true;
                playerManager.isAttacking = true;

                //Close colliders in case they are open already
                colliderControl?.ToggleCollider(false);
                colliderControl?.ToggleParriable(false);

                //Set movement speed multiplier
                locomotionManager.SetMovementSpeedMultiplier(attackMove.movementSpeedMultiplier);

                //Set up collider
                AttackColliderSetup(itemUsed, attackMove, isLeft);

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
                colliderControl?.ToggleCollider(false);
                colliderControl?.ToggleParriable(false);
            };

            //Play animation
            animationManager.PlayOverrideAnimation(attackMove.animationData, attackMove.animationSpeed, onAttackEnterAction, onAttackExitAction, mirrored: isLeft);

            //Consume stamina
            float staminaCost = itemUsed.baseStaminaCost * attackMove.staminaCostMultiplier;
            statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);
        }

        private IEnumerator DisablePlayerRotationAfterAttackStart(float time)
        {
            yield return new WaitForSeconds(time);

            //If the player is still attacking (was not interrupted)
            if (playerManager.isAttacking) playerManager.canRotate = false;
        }

        private void AttackColliderSetup(HandEquippableItem item, AttackMove attackMove, bool isLeft)
        {
            //Get weapon values
            float damageMultiplier = attackMove.damageMultiplier;
            float poiseDamageMultiplier = attackMove.poiseDamageMultiplier;
            float staminaDamageMultiplier = attackMove.staminaDamageMultiplier;
            float knockbackStrengthMultiplier = attackMove.knockbackStrengthMultiplier;
            float flinchStrengthMultiplier = attackMove.flinchStrengthMultiplier;

            int damage = (int)(item.baseDamage * damageMultiplier);
            int poiseDamage = (int)(item.poiseBaseDamage * poiseDamageMultiplier);
            int staminaDamage = (int)(item.staminaBaseDamage * staminaDamageMultiplier);

            float knockbackStrength = item.baseKnockbackStrength * knockbackStrengthMultiplier;
            float flinchStrength = item.baseFlinchStrength * flinchStrengthMultiplier;

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
            inventoryManager.SetDamageColliderValues(colliderInfo, isLeft);
        }

        private bool TryBackstab(AttackType attackType, WeaponItem weapon, bool attackingWithLeft)
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

                    //Play backstab
                    Riposte(AttackType.backstab, attackingWithLeft);
                    networkManager.RiposteServerRpc(AttackType.backstab, attackingWithLeft);

                    //For the victim
                    AttackMove attackMove = GetAttackMove(weapon, AttackType.backstab, attackingWithLeft);

                    //Damage for the victim
                    float damage = weapon.baseDamage * attackMove.damageMultiplier;

                    //Victim animation
                    string backstabbedAnimation = attackMove.victimStaggerAnimation;

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

        private bool TryRiposte(AttackType attackType, WeaponItem weapon, bool attackingWithLeft)
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
                    //Play backstab
                    Riposte(AttackType.riposte, attackingWithLeft);
                    networkManager.RiposteServerRpc(AttackType.riposte, attackingWithLeft);

                    //For the victim
                    AttackMove attackMove = GetAttackMove(weapon, AttackType.riposte, attackingWithLeft);

                    //Damage for the victim
                    float damage = weapon.baseDamage * attackMove.damageMultiplier;

                    //Victim animation
                    string ripostedAnimation = attackMove.victimStaggerAnimation;

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

        public void Riposte(AttackType newAttackType, bool attackingWithLeft)
        {
            this.attackingWithLeft = attackingWithLeft;

            //Get right or left item
            WeaponItem weapon = (WeaponItem)inventoryManager.GetCurrentItem(attackingWithLeft);
            if (weapon == null) return;

            AttackMove attackMove = GetAttackMove(weapon, newAttackType, attackingWithLeft);
            if (attackMove == null) return;

            //Update proprieties
            attackType = newAttackType;

            //Stamina
            float staminaCost = weapon.baseStaminaCost * attackMove.staminaCostMultiplier;

            //Consume stamina
            statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);

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
            animationManager.PlayOverrideAnimation(attackMove.animationData, onRiposteEnterAction, onRiposteExitAction, mirrored: attackingWithLeft);

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
                inventoryManager.GetCurrentItemDamageColliderControl(attackingWithLeft).ToggleCollider(false);
            };

            Action onAttackDeflectedExitAction = () =>
            {
                //Debug.Log("Attack deflected exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                animationManager.FadeOutOverrideAnimation(.15f);
            };

            animationManager.PlayOverrideAnimation(weaponWallBounceAnimationData, onAttackDeflectedEnterAction, onAttackDeflectedExitAction, mirrored: attackingWithLeft);
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
                inventoryManager.GetCurrentItemDamageColliderControl(attackingWithLeft).ToggleCollider(false);
            };

            Action onWallBounceExitAction = () =>
            {
                //Debug.Log("Wall bounce exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                animationManager.FadeOutOverrideAnimation(.15f);
            };

            animationManager.PlayOverrideAnimation(weaponWallBounceAnimationData, 1.2f, onWallBounceEnterAction, onWallBounceExitAction, mirrored: attackingWithLeft);
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
                inventoryManager.GetCurrentItemDamageColliderControl(attackingWithLeft).ToggleCollider(false);

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

            animationManager.PlayOverrideAnimation(parriedAnimationData,onParriedEnterAction, onParriedExitAction, mirrored: attackingWithLeft);
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

        private AttackMove GetAttackMove(WeaponItem weapon, AttackType newAttackType, bool newAttackingWithLeft)
        {
            if (!DoesCombo(newAttackType, newAttackingWithLeft)) nextComboMoveIndex = 0;

            bool twoHanding = combatManager.twoHanding;

            AttackMove[] comboArray;
            switch (newAttackType)
            {
                case AttackType.light: comboArray = (!twoHanding) ? weapon.moveset.oneHandedLightCombo : weapon.moveset.twoHandedLightCombo; break;
                case AttackType.heavy: comboArray = (!twoHanding) ? weapon.moveset.oneHandedHeavyCombo : weapon.moveset.twoHandedHeavyCombo; break;
                case AttackType.running: return (!twoHanding) ? weapon.moveset.oneHandedRunningAttack : weapon.moveset.twoHandedRunningAttack;
                case AttackType.rolling: return (!twoHanding) ? weapon.moveset.oneHandedRollingAttack : weapon.moveset.twoHandedRollingAttack;
                case AttackType.backstab: return weapon.moveset.oneHandedBackstabAttack;
                case AttackType.riposte: return weapon.moveset.oneHandedRiposteAttack;
                default: comboArray = (!twoHanding) ? weapon.moveset.oneHandedLightCombo : weapon.moveset.oneHandedLightCombo; break;
            }

            if (IsComboArrayEmpty(comboArray)) return null;

            AttackMove attackMove = comboArray[nextComboMoveIndex++];
            nextComboMoveIndex %= comboArray.Length;

            if(attackMove == null || attackMove.animationData == null) GetAttackMove(weapon, attackType, newAttackingWithLeft);

            return attackMove;

        }

        private bool DoesCombo(AttackType newAttackType, bool newAttackingWithLeft)
        {
            return (newAttackType == attackType && newAttackingWithLeft == attackingWithLeft);
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

        public bool IsAttackingWithLeft()
        {
            return attackingWithLeft;
        }

        public IEnumerator ResetCombo()
        {
            //Wait for a few milliseconds to see if the player is attacking again (is comboing)
            yield return new WaitForSeconds(.05f);

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
