using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace AlessioBorriello
{
    public class DamageColliderControl : MonoBehaviour
    {
        private PlayerManager playerManager;
        private Collider hitbox;
        private List<int> alreadyHit = new List<int>();


        [SerializeField] private bool canHitMultipleTimes = false; //If it can hit multiple times
        [SerializeField] private float hitFrequencyDelay = .6f; //How often the collider can hit the same thing (if canHitMultipleTimes)
        [SerializeField] private bool startEnabled = false; //If the collider is already open
        [SerializeField] private LayerMask parryLayer;

        private int damage = 10;
        private int poiseDamage = 10;
        private int staminaDamage = 10;

        private float flinchStrenght = 25f; //Force added to the bodypart that connects first
        private float knockbackStrength = 5f; //Force added to the hyps

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
            if(hitPlayerManager.isParrying)
            {
                //Check angle
                Vector3 hitDirection = (hitPlayerManager.GetPhysicalHips().transform.position - playerManager.GetPhysicalHips().transform.position).normalized;
                float hitAngle = Vector3.Angle(Vector3.ProjectOnPlane(hitPlayerManager.GetPhysicalHips().transform.forward, Vector3.up), Vector3.ProjectOnPlane(hitDirection, Vector3.up));

                if (hitAngle > 95f)
                {
                    //Got parried
                    playerManager.GetWeaponManager().Parried();
                    playerManager.GetNetworkManager().ParriedServerRpc();

                    return;
                }
            }

            int otherId = other.transform.root.GetInstanceID();
            if (!CanHit(otherId) || this.transform.root.GetInstanceID() == otherId) return;

            alreadyHit.Add(otherId);
            if (canHitMultipleTimes) StartCoroutine(RemoveHitId(otherId));

            PlayerCollisionManager hitPlayerCollisionManager = hitPlayerManager.GetCollisionManager();
            if (hitPlayerCollisionManager != null)
            {
                hitPlayerCollisionManager.CollisionWithDamageCollider(hitbox, other, damage, poiseDamage, staminaDamage, knockbackStrength, flinchStrenght);
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

        public void SetColliderValues(int damage, int poiseDamage, int staminaDamage, float knockbackStrength)
        {
            if (playerManager == null) return;

            this.damage = damage;
            this.poiseDamage = poiseDamage;
            this.staminaDamage = staminaDamage;
            this.knockbackStrength = knockbackStrength;

        }

    }
}