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

        public void CollisionWithDamageCollider(Collider damageCollider, Collider damagedPlayerCollider, int damage, int poiseDamage, int staminaDamage, float knockbackStrength, float flinchStrenght, string staggerAnimation)
        {
            Debug.Log("Hit");
            if (!playerManager.IsOwner) return;

            if (playerManager.areIFramesActive)
            {
                Debug.Log("Attack dodged");
                return;
            }

            if (CheckIfHit(damageCollider.GetInstanceID()))
            {
                bool attackBlocked = false;
                if(playerManager.isBlocking)
                {
                    //Check for blocking
                    PlayerManager playerManagerHitting = damageCollider.GetComponentInParent<PlayerManager>();
                    if (playerManagerHitting != null && CheckForBlock(physicalHips.transform.position, playerManagerHitting.GetPhysicalHips().transform.position))
                    {
                        attackBlocked = true;
                        damage = AbsorbDamage(damage);

                        //Reduce stamina
                        //Reduce less if shield stability is higher
                        float staminaCost = staminaDamage;
                        statsManager.ConsumeStamina(staminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);

                        //Check if shield broken
                        if(statsManager.CurrentStamina <= 0)
                        {
                            playerManager.GetShieldManager().ShieldBroken();
                            networkManager.ShieldBrokenServerRpc();
                        }
                    }
                }

                //Take poise damage
                if (!attackBlocked) statsManager.TakePoiseDamage(poiseDamage);

                //Check if should stagger
                shouldStagger = statsManager.IsPoiseBroken();

                if (shouldStagger && !attackBlocked)
                {
                    PlayerHurt(staggerAnimation);
                    networkManager.PlayerHurtServerRpc(staggerAnimation);
                }
                if (attackBlocked) StartCoroutine(locomotionManager.StopMovementForTime(.22f));

                //Knockback
                Knockback(damagedPlayerCollider, damageCollider, knockbackStrength, flinchStrenght);

                //Take health damage
                statsManager.TakeDamage(damage);
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

            animationManager.PlayOverrideAnimation("Hurt", onHurtEnterAction, onHurtExitAction);
            shouldStagger = false;
        }

        private int AbsorbDamage(int damage)
        {
            float absorption = 0;
            HandEquippableItem blockingItem = inventoryManager.GetCurrentItem(true); //If 2 handing get the right item
            if (blockingItem != null) absorption = blockingItem.physicalDamageAbsorption;

            damage -= Mathf.RoundToInt((damage * absorption) / 100);
            return damage;
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

        private void Knockback(Collider playerCollider, Collider damageCollider, float knockbackStrength, float flinchStrenght)
        {
            Vector3 collisionPoint = playerCollider.ClosestPoint(damageCollider.transform.position);
            Vector3 hipsPos = physicalHips.transform.position;
            hipsPos.y = collisionPoint.y;

            Vector3 knockbackDirection = (hipsPos - collisionPoint).normalized;
            knockbackDirection = Vector3.ProjectOnPlane(knockbackDirection, locomotionManager.GetGroundNormal());
            ragdollManager.AddForceToPlayer(knockbackStrength * knockbackDirection, ForceMode.VelocityChange);

            Vector3 flinchDirection = (playerCollider.transform.position - collisionPoint).normalized;
            ragdollManager.AddForceToBodyPart(playerCollider.attachedRigidbody, flinchStrenght * flinchDirection, ForceMode.VelocityChange);
        }

    }
}
