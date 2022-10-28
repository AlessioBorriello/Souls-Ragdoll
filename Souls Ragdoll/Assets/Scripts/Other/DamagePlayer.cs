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

        //For debug, declare in OnTriggerEnter
        private Vector3 collisionPoint;
        private Vector3 hipsPos;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerManager playerManager = other.GetComponentInParent<PlayerManager>();
                if (playerManager != null)
                {
                    playerManager.statsManager.ReduceHealth(damage, "Hurt", .1f);
                    playerManager.animationManager.UpdateMovementAnimatorValues(0, 0, 0); //Stop the player

                    //Knockback
                    collisionPoint = other.ClosestPoint(transform.position);
                    hipsPos = playerManager.physicalHips.transform.position;
                    hipsPos.y = collisionPoint.y;

                    Vector3 knockbackDirection = (hipsPos - collisionPoint).normalized;

                    playerManager.ragdollManager.AddForceToPlayer(knockbackStrength * knockbackDirection, ForceMode.VelocityChange);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if(collisionPoint != null)
            {
                Gizmos.DrawSphere(collisionPoint, .05f);
                Gizmos.DrawLine(collisionPoint, hipsPos);
            }
        }
    }
}