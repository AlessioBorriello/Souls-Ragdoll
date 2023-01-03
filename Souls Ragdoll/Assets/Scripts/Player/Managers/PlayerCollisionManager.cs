using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerCollisionManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private PlayerNetworkManager networkManager;
        private AnimationManager animationManager;
        private PlayerLocomotionManager locomotionManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerStatsManager statsManager;
        private PlayerCombatManager combatManager;
        private PlayerInventoryManager inventoryManager;

        private Rigidbody physicalHips;
        [SerializeField] private LayerMask characterLayer;

        private List<int> inContact = new List<int>();
        private bool shouldStagger = false;

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
            locomotionManager = playerManager.GetLocomotionManager();
            animationManager = playerManager.GetAnimationManager();
            ragdollManager = playerManager.GetRagdollManager();
            statsManager = playerManager.GetStatsManager();
            combatManager = playerManager.GetCombatManager();
            inventoryManager = playerManager.GetInventoryManager();

            physicalHips = playerManager.GetPhysicalHips();
        }

        public void CollisionWithDamageCollider(Collider damageCollider, Collider damagedPlayerCollider, DamageColliderInfo colliderInfo)
        {
            if (playerManager.IsOwner && playerManager.areIFramesActive)
            {
                Debug.Log("Attack dodged");
                return;
            }

            if (CheckIfHit(damageCollider.GetInstanceID()))
            {
                PlayerManager playerManagerHitting = damageCollider.GetComponentInParent<PlayerManager>();

                //Flinch
                Collider bodyPartHit = GetBodyPartHit(damagedPlayerCollider, damageCollider);
                Flinch(playerManagerHitting.GetPhysicalHips().position, bodyPartHit, colliderInfo.flinchStrenght);

                DamageColliderControl damageColliderControl = damageCollider.GetComponent<DamageColliderControl>();

                if (!playerManager.IsOwner || damageColliderControl == null) return;

                //Check for block
                bool attackBlocked = false;
                int damage = colliderInfo.damage;
                if(playerManager.isBlocking)
                {
                    //Check for blocking
                    if (playerManagerHitting != null && CheckForBlock(physicalHips.transform.position, playerManagerHitting.GetPhysicalHips().transform.position))
                    {
                        attackBlocked = true;
                        BlockAttack(ref damage, colliderInfo, playerManagerHitting);
                    }
                }

                GetHit(damage, attackBlocked, colliderInfo);
            }
        }

        private void GetHit(int damage, bool attackBlocked, DamageColliderInfo colliderInfo)
        {
            //Take poise damage
            if (!attackBlocked) statsManager.TakePoiseDamage(colliderInfo.poiseDamage);

            //Check if should stagger
            shouldStagger = statsManager.IsPoiseBroken();

            if (shouldStagger && !attackBlocked)
            {
                PlayerHurt(colliderInfo.staggerAnimation);
                networkManager.PlayerHurtServerRpc(colliderInfo.staggerAnimation);
            }
            if (attackBlocked) StartCoroutine(locomotionManager.StopMovementForTime(.22f));

            //Take health damage
            statsManager.TakeDamage(damage);
        }

        private void BlockAttack(ref int damage, DamageColliderInfo colliderInfo, PlayerManager playerManagerHitting)
        {
            HandEquippableItem blockingItem = inventoryManager.GetCurrentItem(playerManager.GetShieldManager().IsBlockingWithLeft()); //If 2 handing get the right item

            //Reduce damage
            damage = AbsorbDamage(blockingItem, damage);

            //Reduce stamina damage based on stability
            float staminaCost = colliderInfo.staminaDamage;
            staminaCost = AbsorbStaminaDamage(blockingItem, staminaCost);

            //Consume stamina
            statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);

            //Check if shield broken
            if (statsManager.CurrentStamina <= 0)
            {
                playerManager.GetShieldManager().ShieldBroken();
                networkManager.ShieldBrokenServerRpc();
            }
            else //Check if the shield deflected the attack
            {
                //Debug.Log($"Attack level : {colliderInfo.attackDeflectionLevel}; Block level : {blockingItem.deflectionLevel}");
                if (colliderInfo.attackDeflectionLevel < blockingItem.deflectionLevel)
                {
                    playerManagerHitting.GetWeaponManager().AttackDeflected();
                    playerManagerHitting.GetNetworkManager().AttackDeflectedServerRpc(playerManagerHitting.OwnerClientId);
                }
            }
        }

        public void PlayerHurt(string hurtAnimation)
        {
            //Create enter and exit events
            Action onHurtEnterAction = () =>
            {
                //Debug.Log("Hurt enter");
                playerManager.isStuckInAnimation = true;
                playerManager.isInOverrideAnimation = true;
                playerManager.canRotate = false;
                playerManager.shouldSlide = false;
            };

            Action onHurtExitAction = () =>
            {
                //Debug.Log("Hurt exit");
                playerManager.isStuckInAnimation = false;
                playerManager.isInOverrideAnimation = false;
                playerManager.canRotate = true;
                animationManager.FadeOutOverrideAnimation(.1f);
            };

            animationManager.PlayOverrideAnimation(hurtAnimation, onHurtEnterAction, onHurtExitAction);
            shouldStagger = false;
        }

        private int AbsorbDamage(HandEquippableItem blockingItem, int damage)
        {
            float absorption = 0;
            if (blockingItem != null) absorption = blockingItem.physicalDamageAbsorption;

            damage -= Mathf.RoundToInt((damage * absorption) / 100);
            return damage;
        }

        private float AbsorbStaminaDamage(HandEquippableItem blockingItem, float staminaDamage)
        {
            float absorption = 0;
            if (blockingItem != null) absorption = blockingItem.blockStability;

            staminaDamage -= Mathf.RoundToInt((staminaDamage * absorption) / 100);
            return staminaDamage;
        }

        private bool CheckForBlock(Vector3 thisHipsPosition, Vector3 hittingHipsPosition)
        {
            Vector3 hitDirection = (thisHipsPosition - hittingHipsPosition).normalized;
            float hitAngle = Vector3.Angle(Vector3.ProjectOnPlane(physicalHips.transform.forward, Vector3.up), Vector3.ProjectOnPlane(hitDirection, Vector3.up));

            if (hitAngle > 95f) return true;
            else return false;
        }

        public void ExitCollision(int colliderId)
        {
            if (inContact.Contains(colliderId))
            {
                inContact.Remove(colliderId);
            }

        }

        private bool CheckIfHit(int colliderId)
        {
            bool firstCollision = false;
            if (!inContact.Contains(colliderId)) //If no player collider is in contact with the other collider
            {
                firstCollision = true;
            }
            else
            {
                inContact.Add(colliderId);
            }
            
            return firstCollision;
        }

        private void Flinch(Vector3 hittingPlayerHipsPosition, Collider bodyPartHit, float flinchStrenght)
        {
            if (bodyPartHit == null) return;
            Vector3 flinchDirection = Vector3.ProjectOnPlane(physicalHips.position - hittingPlayerHipsPosition, locomotionManager.GetGroundNormal()).normalized;

            //Debug.Log($"Part: {bodyPartHit}, strength: {flinchStrenght}");
            ragdollManager.AddForceToBodyPart(bodyPartHit.attachedRigidbody, flinchStrenght * flinchDirection, ForceMode.VelocityChange);
        }

        private Collider GetBodyPartHit(Collider playerHurtbox, Collider damageCollider)
        {
            Vector3 collisionPoint = playerHurtbox.ClosestPoint(damageCollider.transform.position);
            Vector3 hipsPos = physicalHips.transform.position;

            RaycastHit hit;
            if (Physics.Raycast(collisionPoint, Vector3.ProjectOnPlane((hipsPos - collisionPoint), locomotionManager.GetGroundNormal()), out hit, 1f, characterLayer))
            {
                return hit.collider;
            }

            return null;
        }

        private void Knockback(Collider playerHurtbox, Collider damageCollider, float knockbackStrength, float flinchStrenght)
        {
            Vector3 collisionPoint = playerHurtbox.ClosestPoint(damageCollider.transform.position);
            Vector3 hipsPos = physicalHips.transform.position;
            hipsPos.y = collisionPoint.y;

            Vector3 knockbackDirection = (hipsPos - collisionPoint).normalized;
            knockbackDirection = Vector3.ProjectOnPlane(knockbackDirection, locomotionManager.GetGroundNormal());
            ragdollManager.AddForceToPlayer(knockbackStrength * knockbackDirection, ForceMode.VelocityChange);
        }

    }
}
