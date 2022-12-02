using Newtonsoft.Json.Bson;
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
        //private GameObject animatedPlayer;

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
                if (statsManager.CurrentStamina < 1) return;

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

            //If an attack was NOT chained and it is not the first attack
            if (!chainedAttack && nextComboAttackIndex != 0) return;

            //Update proprieties
            attackType = newAttackType;

            //Check for backstab, if a backstab goes through, then return
            if(TryBackstab(attackType, weapon, isLeft)) return;

            //Check for riposte, if a riposte goes through, then return
            if (TryRiposte(attackType, weapon, isLeft)) return;

            //Get animation to play and movement speed multiplier
            string attackAnimation = GetAttackAnimationString(weapon, attackType);

            //Get weapon values
            float damageMultiplier = GetWeaponDamageMultiplier(weapon);
            float poiseDamageMultiplier = GetWeaponPoiseDamageMultiplier(weapon);

            int damage = (int)(weapon.baseDamage * damageMultiplier);
            int poiseDamage = (int)(weapon.poiseBaseDamage * poiseDamageMultiplier);
            int staminaDamage = (int)(weapon.staminaBaseDamage * damageMultiplier);

            float knockbackStrength = weapon.knockbackStrength;

            //Attack
            Attack(attackAnimation, isLeft, damage, poiseDamage, staminaDamage, knockbackStrength);
            networkManager.AttackServerRpc(attackAnimation, isLeft, damage, poiseDamage, staminaDamage, knockbackStrength);

            //Set attack speed multiplier
            float attackMovementSpeedMultiplier = GetAttackMovementSpeedMultiplier(weapon);
            locomotionManager.SetMovementSpeedMultiplier(attackMovementSpeedMultiplier);

            //Consume stamina
            float staminaCost = GetAttackStaminaCost(attackType, weapon);
            statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);

        }

        public void Attack(string attackAnimation, bool attackingWithLeft, int damage, int poiseDamage, int staminaDamage, float knockbackStrength)
        {
            if (attackAnimation == "") return;

            //Play animation
            animationManager.PlayTargetAnimation(attackAnimation, .2f, true);

            //Set collider values
            inventoryManager.SetColliderValues(attackingWithLeft, damage, poiseDamage, staminaDamage, knockbackStrength);

            //Update animator values
            this.attackingWithLeft = attackingWithLeft;
            animationManager.UpdateAttackingWithLeftValue(attackingWithLeft);
        }

        private bool TryBackstab(AttackType attackType, WeaponItem weapon, bool attackingWithLeft)
        {
            if (attackType != AttackType.light || chainedAttack) return false;

            RaycastHit hit;
            float backstabDistance = 1.4f;
            float backstabAngle = 36f;

            if (Physics.Raycast(physicalHips.transform.position, physicalHips.transform.forward, out hit, backstabDistance, backstabLayer))
            {
                PlayerManager victimManager = hit.collider.GetComponentInParent<PlayerManager>();

                //If somehow the ray hit yourself
                if (playerManager == victimManager) return false;

                if (victimManager == null || !victimManager.canBeBackstabbed) return false;

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
                    string backstabAnimation = weapon.backstabAttack;
                    string backstabbedAnimation = weapon.backstabVictimAnimation;

                    //Consume stamina
                    statsManager.ConsumeStamina(weapon.backstabAttackStaminaUse, statsManager.playerStats.staminaDefaultRecoveryTime);

                    //Play backstab
                    Riposte(backstabAnimation, attackingWithLeft);
                    networkManager.RiposteServerRpc(backstabAnimation, attackingWithLeft);

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
            if (attackType != AttackType.light || chainedAttack) return false;

            RaycastHit hit;
            float riposteDistance = 1.4f;
            float riposteAngle = 115f;

            if (Physics.Raycast(physicalHips.transform.position, physicalHips.transform.forward, out hit, riposteDistance, riposteLayer))
            {
                PlayerManager victimManager = hit.collider.GetComponentInParent<PlayerManager>();

                //If somehow the ray hit yourself
                if (playerManager == victimManager) return false;

                if (victimManager == null || !victimManager.canBeRiposted) return false;

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
                    string riposteAnimation = weapon.riposteAttack;
                    string ripostedAnimation = weapon.riposteVictimAnimation;

                    //Consume stamina
                    statsManager.ConsumeStamina(weapon.riposteAttackStaminaUse, statsManager.playerStats.staminaDefaultRecoveryTime);

                    //Play riposte
                    Riposte(riposteAnimation, attackingWithLeft);
                    networkManager.RiposteServerRpc(riposteAnimation, attackingWithLeft);

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

        public void Riposte(string riposteAnimation, bool attackingWithLeft)
        {

            //Play riposte animation
            animationManager.PlayTargetAnimation(riposteAnimation, .1f, true);

            //Stop player
            locomotionManager.SetMovementSpeedMultiplier(1);

            //Update animator values
            this.attackingWithLeft = attackingWithLeft;
            animationManager.UpdateAttackingWithLeftValue(attackingWithLeft);

        }

        public void Parried()
        {
            animationManager.PlayTargetAnimation("Parried", .15f, true);

            //Disable attack collider
            inventoryManager.GetCurrentItemDamageColliderControl(attackingWithLeft).ToggleCollider(false);

            //Allow enemy to riposte
            playerManager.canBeRiposted = true;

            //Set forward when parried
            playerManager.GetCombatManager().forwardWhenParried = physicalHips.transform.forward;
        }

        public void Riposted(Vector3 riposteVictimPosition, Quaternion riposteVictimRotation, string riposteVictimAnimation, float damage)
        {
            //Play animation
            playerManager.GetAnimationManager().PlayTargetAnimation(riposteVictimAnimation, .1f, true);

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
                case AttackType.riposte: return weapon.riposteAttack;
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

        public bool IsChainingAttack()
        {
            return chainedAttack;
        }

        public void ResetCombo()
        {
            if (!chainedAttack) //Reset combo if player has not chained an attack
            {
                nextComboAttackIndex = 0;

                //Stop attacking
                playerManager.isAttacking = false;
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
            backstab,
            riposte
        }

    }
}
