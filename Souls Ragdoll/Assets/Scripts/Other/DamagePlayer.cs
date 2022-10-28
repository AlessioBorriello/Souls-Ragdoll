using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlessioBorriello
{
    public class DamagePlayer : MonoBehaviour
    {
        public int damage = 30;
        public float knockbackStrength = 5f;
        public bool canHitMultipleTimes = false;
        private List<int> alreadyHit = new List<int>();

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                int otherId = other.transform.root.GetInstanceID();
                if (!alreadyHit.Contains(otherId)) alreadyHit.Add(otherId);
                else if (alreadyHit.Contains(otherId) && !canHitMultipleTimes) return;

                PlayerManager playerManager = other.GetComponentInParent<PlayerManager>();
                if (playerManager != null)
                {
                    if(playerManager.collisionManager.EnterCollision(this.GetInstanceID()))
                    {
                        playerManager.statsManager.ReduceHealth(damage, "Hurt", .1f);
                        playerManager.animationManager.UpdateMovementAnimatorValues(0, 0, 0); //Stop the player

                        //Knockback
                        Knockback(other, playerManager);
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerManager playerManager = other.GetComponentInParent<PlayerManager>();
                if (playerManager != null)
                {
                    playerManager.collisionManager.ExitCollision(this.GetInstanceID());
                }
            }
        }

        private void Knockback(Collider other, PlayerManager playerManager)
        {
            Vector3 collisionPoint = other.ClosestPoint(transform.position);
            Vector3 hipsPos = playerManager.physicalHips.transform.position;
            hipsPos.y = collisionPoint.y;

            Vector3 knockbackDirection = (hipsPos - collisionPoint).normalized;
            knockbackDirection = Vector3.ProjectOnPlane(knockbackDirection, playerManager.groundNormal);

            playerManager.ragdollManager.AddForceToPlayer(knockbackStrength * knockbackDirection, ForceMode.VelocityChange);
        }
    
    }
}