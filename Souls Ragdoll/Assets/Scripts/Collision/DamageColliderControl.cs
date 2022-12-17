using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace AlessioBorriello
{
    public struct DamageColliderInfo
    {
        public int damage;
        public int poiseDamage;
        public int staminaDamage;
        public float knockbackStrength;
        public float flinchStrenght;
        public string staggerAnimation;
        [Range(1, 4)] public int attackDeflectionLevel;
    }

    public class DamageColliderControl : MonoBehaviour
    {

        private PlayerManager playerManager;
        private Collider hitbox;
        private List<int> alreadyHit = new List<int>();

        [SerializeField] private bool canHitMultipleTimes = false; //If it can hit multiple times
        [SerializeField] private float hitFrequencyDelay = .6f; //How often the collider can hit the same thing (if canHitMultipleTimes)
        [SerializeField] private bool startEnabled = false; //If the collider is already open
        [SerializeField] private LayerMask parryLayer;

        private bool canBeParried = false; //Only turn on if the attack can be parried

        private DamageColliderInfo colliderInfo;

        private string staggerAnimation;

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
            //if (animationManager != null) animationManager.PlayOverrideAnimation("AttackBounce");
            ToggleCollider(false);
        }

        private void PlayerTriggerEnter(Collider other)
        {
            PlayerManager hitPlayerManager = other.GetComponentInParent<PlayerManager>();

            int otherId = other.transform.root.GetInstanceID();
            if (!CanHit(otherId) || this.transform.root.GetInstanceID() == otherId) return;

            alreadyHit.Add(otherId);
            if (canHitMultipleTimes) StartCoroutine(RemoveHitId(otherId));

            PlayerCollisionManager hitPlayerCollisionManager = hitPlayerManager.GetCollisionManager();
            if (hitPlayerCollisionManager != null)
            {
                hitPlayerCollisionManager.CollisionWithDamageCollider(hitbox, other, colliderInfo);
            }
        }

        public void ToggleCollider(bool enabled)
        {
            hitbox.enabled = enabled;

            //Empty hit list on close
            if (!enabled) EmptyHitList();
        }

        public void ToggleParriable(bool enabled)
        {
            canBeParried = enabled;
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

        public void SetColliderValues(DamageColliderInfo newColliderInfo)
        {
            if (playerManager == null) return;

            colliderInfo = newColliderInfo;

        }

        public bool IsParriable()
        {
            return canBeParried;
        }

    }
}