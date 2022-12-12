using Animancer;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newWeaponItem", menuName = "Item/Hand Item/Weapon/Weapon Item")]
    public class WeaponItem : HandEquippableItem
    {
        [Header("Base values")]
        public float baseDamage;
        public float staminaBaseDamage;
        public float poiseBaseDamage;
        public float baseKnockbackStrength = 3.2f;
        public float baseFlinchStrength = 25f;

        [Header("Stamina use")]
        public float baseStaminaCost = 40f;

        [Header("Moveset")]
        public MoveSet moveset;

        //Stats scaling

    }
}