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

        public void OpenParryCollider(float time)
        {
            parryCollider.enabled = true;
            StartCoroutine(CloseParryColliderAfterTime(time));
        }

        public void CloseParryCollider()
        {
            parryCollider.enabled = false;
        }

        private IEnumerator CloseParryColliderAfterTime(float time)
        {
            yield return new WaitForSeconds(time);
            CloseParryCollider();
        }
    }
}