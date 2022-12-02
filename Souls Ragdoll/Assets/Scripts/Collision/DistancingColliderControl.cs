using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class DistancingColliderControl : MonoBehaviour
    {

        private void LateUpdate()
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }
}
