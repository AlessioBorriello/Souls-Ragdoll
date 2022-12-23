using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    //Base class for all the items that can be equipped on the hand
    public class HandEquippableItem : EquippableItem
    {
        public GameObject modelPrefab;

        [Header("Idle animations")]
        public AnimationData oneHandedIdleAnimationData;
        public AnimationData twoHandedIdleAnimationData;

        [Header("Damage absorptions %")]
        [Range(0, 100)] public float physicalDamageAbsorption;

        [Header("Other")]
        [Range(0, 100)] public float blockStability;
        [Range(1, 4)] public int deflectionLevel = 1;
    }
}
