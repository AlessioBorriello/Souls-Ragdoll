using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newWeaponItem", menuName = "Item/Hand Item/Weapon Item")]
    public class WeaponItem : HandEquippableItem
    {
        [Header("Damage")]
        public float baseDamage;
        public float staminaBaseDamage;
        public float poiseBaseDamage;

        [Header("Stamina use")]
        public float lightAttackStaminaUse = 40f;
        public float heavyAttackStaminaUse = 65f;
        public float rollingAttackStaminaUse = 30f;
        public float runningAttackStaminaUse = 34f;
        public float backstabAttackStaminaUse = 30f;


        [Header("Movement speed multiplier")]
        public float movementSpeedMultiplier = 2.5f; //How fast the player moves forward when attacking

        [Header("Light one hand combo")]
        public string[] oneHandedLightAttackCombo = new string[3];
        public float oneHandedLightAttacksDamageMultiplier = 1f;

        [Header("Heavy one hand combo")]
        public string[] OneHandedHeavyAttackCombo = new string[3];
        public float oneHandedHeavyAttacksDamageMultiplier = 1.7f;

        [Header("One Handed Running attack")]
        public string oneHandedRunningAttack;
        public float oneHandedRunningAttackDamageMultiplier = 1.2f;

        [Header("One Handed Rolling attack")]
        public string oneHandedRollingAttack;
        public float oneHandedRollingAttackDamageMultiplier = .9f;

        [Header("Backstab attack")]
        public string backstabAttack;
        public string backstabVictimAnimation;
        public float backstabtAttackDamageMultiplier = 2.4f;

        [Header("Other")]
        public float knockbackStrength = 3.2f;
        public float flinchStrength = 25f;

        //Stats scaling

    }
}