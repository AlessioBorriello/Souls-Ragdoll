using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerCollisionManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private List<int> inContact = new List<int>();

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();
        }

        public void EnterCollision(DamageCollider damageCollider, Collider playerCollider, int damage, float knockbackStrength, float flinchStrenght)
        {
            if (CheckIfHit(damageCollider.GetInstanceID()))
            {
                playerManager.statsManager.ReduceHealth(damage, "Hurt", .1f);
                playerManager.animationManager.UpdateMovementAnimatorValues(0, 0, 0); //Stop the player

                //Knockback
                Knockback(playerCollider, damageCollider, knockbackStrength, flinchStrenght);
            }
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
            }

            inContact.Add(colliderId);
            return firstCollision;
        }

        Vector3 collisionPoint;
        Vector3 hipsPos;
        private void Knockback(Collider playerCollider, DamageCollider damageCollider, float knockbackStrength, float flinchStrenght)
        {
            collisionPoint = playerCollider.ClosestPoint(damageCollider.transform.position);
            hipsPos = playerManager.physicalHips.transform.position;
            hipsPos.y = collisionPoint.y;

            Vector3 knockbackDirection = (hipsPos - collisionPoint).normalized;
            knockbackDirection = Vector3.ProjectOnPlane(knockbackDirection, playerManager.groundNormal);
            playerManager.ragdollManager.AddForceToPlayer(knockbackStrength * knockbackDirection, ForceMode.VelocityChange);

            Vector3 flinchDirection = (playerCollider.transform.position - collisionPoint).normalized;
            playerManager.ragdollManager.AddForceToBodyPart(playerCollider.attachedRigidbody, flinchStrenght * flinchDirection, ForceMode.VelocityChange);
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(collisionPoint, .07f);
            Gizmos.DrawLine(collisionPoint, hipsPos);
        }

    }
}
