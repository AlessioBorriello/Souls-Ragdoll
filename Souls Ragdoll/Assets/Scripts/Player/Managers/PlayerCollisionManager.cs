using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerCollisionManager : MonoBehaviour
    {
        private PlayerManager playerManager;
        private AnimationManager animationManager;
        private PlayerLocomotionManager locomotionManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerStatsManager statsManager;

        private Rigidbody physicalHips;

        private List<int> inContact = new List<int>();
        private bool shouldStagger = true;

        private void Start()
        {
            playerManager = GetComponent<PlayerManager>();
            locomotionManager = playerManager.GetLocomotionManager();
            animationManager = playerManager.GetAnimationManager();
            ragdollManager = playerManager.GetRagdollManager();
            statsManager = playerManager.GetStatsManager();

            physicalHips = playerManager.GetPhysicalHips();
        }

        public void EnterCollision(ColliderControl damageCollider, Collider playerCollider, int damage, float knockbackStrength, float flinchStrenght)
        {
            if (CheckIfHit(damageCollider.GetInstanceID()))
            {
                Debug.Log("Hit");
                statsManager.ReduceHealth(damage, "Hurt", .1f, shouldStagger);
                animationManager.UpdateMovementAnimatorValues(0, 0, 0); //Stop the player

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
                inContact.Add(colliderId);
            }
            
            return firstCollision;
        }

        Vector3 collisionPoint;
        Vector3 hipsPos;
        private void Knockback(Collider playerCollider, ColliderControl damageCollider, float knockbackStrength, float flinchStrenght)
        {
            collisionPoint = playerCollider.ClosestPoint(damageCollider.transform.position);
            hipsPos = physicalHips.transform.position;
            hipsPos.y = collisionPoint.y;

            Vector3 knockbackDirection = (hipsPos - collisionPoint).normalized;
            knockbackDirection = Vector3.ProjectOnPlane(knockbackDirection, locomotionManager.GetGroundNormal());
            ragdollManager.AddForceToPlayer(knockbackStrength * knockbackDirection, ForceMode.VelocityChange);

            Vector3 flinchDirection = (playerCollider.transform.position - collisionPoint).normalized;
            ragdollManager.AddForceToBodyPart(playerCollider.attachedRigidbody, flinchStrenght * flinchDirection, ForceMode.VelocityChange);
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(collisionPoint, .07f);
            Gizmos.DrawLine(collisionPoint, hipsPos);
        }

    }
}
