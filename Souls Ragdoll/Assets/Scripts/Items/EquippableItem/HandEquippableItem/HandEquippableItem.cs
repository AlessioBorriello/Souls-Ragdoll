using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    //Base class for all the items that can be equipped on the hand
    public class HandEquippableItem : EquippableItem
    {
        public GameObject modelPrefab;

        [Header("One handed Idle")]
        public string oneHandedIdle;

        [Header("Damage absorptions %")]
        public float physicalDamageAbsorption;
    }
}
