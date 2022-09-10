using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class KnockOutOnCollision : MonoBehaviour
    {

        public float forceToKnockOut = 40;
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                PlayerManager playerManager = collision.collider.GetComponentInParent<PlayerManager>(); //Get player manager
                if (collision.impulse.magnitude > forceToKnockOut)
                {
                    playerManager.ragdollManager.KnockOut();
                }
            }
        }
    }
}
