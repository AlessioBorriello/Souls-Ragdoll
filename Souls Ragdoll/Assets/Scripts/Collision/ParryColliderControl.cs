using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class ParryColliderControl : MonoBehaviour
    {
        private Collider parryCollider;

        private void Awake()
        {
            parryCollider = GetComponent<Collider>();
        }

        public void ToggleParryCollider(bool enabled)
        {
            parryCollider.enabled = enabled;
        }
    }
}