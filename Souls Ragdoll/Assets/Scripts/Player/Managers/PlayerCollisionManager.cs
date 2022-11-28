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
        public bool shouldStagger = true; //Should be private and be true if the hit breaks the poise

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

        public void CollisionWithDamageCollider(Collider damageCollider, Collider damagedPlayerCollider, int damage, float knockbackStrength, float flinchStrenght)
        {
            if (!playerManager.IsOwner) return;

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
                    }
                }

                statsManager.TakeDamage(damage);
                if (shouldStagger && !attackBlocked)
                {
                    animationManager.PlayTargetAnimation("Hurt", .1f, true);
                    networkManager.PlayTargetAnimationServerRpc("Hurt", .1f, true);
                }
                if (attackBlocked) StartCoroutine(locomotionManager.StopMovementForTime(.22f));

                //Knockback
                Knockback(damagedPlayerCollider, damageCollider, knockbackStrength, flinchStrenght);
            }
        }

        private int AbsorbDamage(int damage)
        {
            float absorption = 0;
            HandEquippableItem blockingItem = (combatManager.GetShieldManager().IsBlockingWithLeft())? inventoryManager.GetCurrentItem(true) : inventoryManager.GetCurrentItem(false);
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
