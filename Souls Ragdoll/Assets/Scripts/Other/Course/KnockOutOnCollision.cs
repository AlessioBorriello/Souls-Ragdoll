using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class KnockOutOnCollision : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                PlayerManager playerManager = collision.collider.GetComponentInParent<PlayerManager>(); //Get player manager
                KnockOutResistance knockOutResistanceComponent = collision.collider.GetComponent<KnockOutResistance>(); //Get knockout resistance component of the body part
                if (playerManager != null && knockOutResistanceComponent != null)
                {
                    float resistance = knockOutResistanceComponent.knockOutResistance; //Get resistance of that body part
                    if (collision.impulse.magnitude > resistance)
                    {
                        playerManager.ragdollManager.KnockOut();
                    }
                }
            }
        }
    }
}
