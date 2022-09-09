using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class DespawnAfterTime : MonoBehaviour
    {

        public float totalLifeTime = 15;
        private float currentLifeTime = 0;

        private void Update()
        {
            currentLifeTime += Time.deltaTime;
            if(currentLifeTime > totalLifeTime) Destroy(gameObject);
        }

    }
}