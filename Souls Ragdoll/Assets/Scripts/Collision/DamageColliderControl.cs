using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{
    public class DamageColliderControl : MonoBehaviour
    {
        private PlayerManager playerManager;
        private Collider hitbox;
        private List<int> alreadyHit = new List<int>();

        private int damage = 10;
        [SerializeField] private float knockbackStrength = 5f; //Force added to the hyps
        [SerializeField] private float flinchStrenght = 25f; //Force added to the bodypart that connects first
        [SerializeField] private bool startEnabled = false; //If the collider is already open

        [SerializeField] private bool canHitMultipleTimes = false; //If it can hit multiple times
        [SerializeField] private float hitFrequencyDelay = .6f; //How often the collider can hit the same thing (if canHitMultipleTimes)

        private void Awake()
        {
            hitbox = GetComponent<Collider>();
            hitbox.gameObject.SetActive(true);
            hitbox.isTrigger = true;
            hitbox.enabled = startEnabled;
        }

        private void Start()
        {
            playerManager = GetComponentInParent<PlayerManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerTriggerEnter(other);
            }
            else if(other.CompareTag("Enemy"))
            {
                //If it's an enemy
            }
            else if (other.CompareTag("Static"))
            {
                //Vector3 collisionPoint = other.ClosestPoint(transform.position);
                //Vector3 collisionNormal = (transform.position - collisionPoint).normalized;
                //StaticTriggerEnter(collisionNormal);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerCollisionManager playerCollisionManager = other.GetComponentInParent<PlayerCollisionManager>();
                if (playerCollisionManager != null)
                {
                    playerCollisionManager.ExitCollision(this.GetInstanceID());
                }
            }
            else if (other.CompareTag("Enemy"))
            {
                //If it's an enemy
            }
        }

        private void StaticTriggerEnter(Vector3 normal)
        {
            PlayerManager playerManager = GetComponentInParent<PlayerManager>();
            //If the static was the ground
            if (playerManager != null && normal == playerManager.GetLocomotionManager().GetGroundNormal()) return;

            AnimationManager animationManager = playerManager.GetAnimationManager();
            if (animationManager != null) animationManager.PlayTargetAnimation("AttackBounce", .1f, true);
            ToggleCollider(false);
        }

        private void PlayerTriggerEnter(Collider other)
        {
            int otherId = other.transform.root.GetInstanceID();
            if (!CanHit(otherId) || this.transform.root.GetInstanceID() == otherId) return;

            alreadyHit.Add(otherId);
            if (canHitMultipleTimes) StartCoroutine(RemoveHitId(otherId));

            PlayerCollisionManager playerCollisionManager = other.GetComponentInParent<PlayerCollisionManager>();
            if (playerCollisionManager != null)
            {
                playerCollisionManager.CollisionWithDamageCollider(hitbox, other, damage, knockbackStrength, flinchStrenght);
            }
        }

        public void ToggleCollider(bool enabled)
        {
            hitbox.enabled = enabled;

            //Empty hit list on close
            if (!enabled) EmptyHitList();
        }

        public void EmptyHitList()
        {
            alreadyHit.Clear();
        }

        private bool CanHit(int otherId)
        {
            if (!alreadyHit.Contains(otherId)) return true;
            else return false;
        }

        private IEnumerator RemoveHitId(int id)
        {
            yield return new WaitForSeconds(hitFrequencyDelay);
            if (alreadyHit.Contains(id)) alreadyHit.Remove(id);
        }

        public void SetColliderValues(int damage, float knockbackStrength, float flinchStrenght)
        {
            if (playerManager == null) return;

            this.damage = damage;
            this.knockbackStrength = knockbackStrength;
            this.flinchStrenght = flinchStrenght;
        }

    }
}