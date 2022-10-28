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
                int id = other.transform.root.GetInstanceID();
                if (!alreadyHit.Contains(id)) alreadyHit.Add(id);
                else if (alreadyHit.Contains(id) && !canHitMultipleTimes) return;

                PlayerManager playerManager = other.GetComponentInParent<PlayerManager>();
                if (playerManager != null)
                {
                    playerManager.statsManager.ReduceHealth(damage, "Hurt", .1f);
                    playerManager.animationManager.UpdateMovementAnimatorValues(0, 0, 0); //Stop the player

                    //Knockback
                    Knockback(other, playerManager);
                    
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