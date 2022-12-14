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

        [Header("Blocking animations")]
        public AnimationData oneHandedBlockAnimationData;
        public AnimationData twoHandedBlockAnimationData;

        [Header("Generic attack")]
        public AttackMove genericAttackMove;

        [Header("Damage absorptions %")]
        [Range(0, 100)] public float physicalDamageAbsorption;

        [Header("Damage values")]
        public float baseDamage;
        public float staminaBaseDamage;
        public float poiseBaseDamage;
        public float baseKnockbackStrength = 3.2f;
        public float baseFlinchStrength = 25f;

        [Header("Stamina cost")]
        public float baseStaminaCost = 40f;

        [Header("Other")]
        [Range(0, 100)] public float blockStability;
        [Range(1, 4)] public int deflectionLevel = 1;
    }
}
