using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlessioBorriello
{
    public class DamagePlayer : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerStatsManager statManager = other.GetComponentInParent<PlayerStatsManager>();
                if (statManager != null)
                {
                    statManager.ReduceHealth(30, "Hurt");
                }
            }
        }
    }
}