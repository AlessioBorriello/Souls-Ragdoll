using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class ColliderControl : MonoBehaviour
    {
        private Collider hitbox;
        private List<int> alreadyHit = new List<int>();

        [SerializeField] private int damage = 30;
        [SerializeField] private float knockbackStrength = 5f; //Force added to the hyps
        [SerializeField] private float flinchStrenght = 25f; //Force added to the bodypart that connects first
        [SerializeField] private bool startEnabled = false; //If the collider is already open

        [SerializeField] private bool canHitMultipleTimes = false; //If it can hit multiple times
        [SerializeField] private float hitFrequencyDelay = .3f; //How often the collider can hit the same thing (if canHitMultipleTimes)

        private void Awake()
        {
            hitbox = GetComponent<Collider>();
            hitbox.gameObject.SetActive(true);
            hitbox.isTrigger = true;
            hitbox.enabled = startEnabled;
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
                Debug.Log("Weapon bounce");
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

        private void PlayerTriggerEnter(Collider other)
        {
            int otherId = other.transform.root.GetInstanceID();
            if (!CanHit(otherId) || this.transform.root.GetInstanceID() == otherId) return;

            alreadyHit.Add(otherId);
            if (canHitMultipleTimes) StartCoroutine(RemoveHitId(otherId));


            PlayerCollisionManager playerCollisionManager = other.GetComponentInParent<PlayerCollisionManager>();
            if (playerCollisionManager != null)
            {
                playerCollisionManager.EnterCollision(this, other, damage, knockbackStrength, flinchStrenght);
            }
        }

        public void ToggleCollider(bool enabled)
        {
            hitbox.enabled = enabled;
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

    }
}