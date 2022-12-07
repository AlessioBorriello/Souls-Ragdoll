using Animancer;
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
        public float riposteAttackStaminaUse = 36f;


        [Header("Movement speed multiplier")]
        public float movementSpeedMultiplier = 2.5f; //How fast the player moves forward when attacking

        [Header("Light one hand combo")]
        public string[] oneHandedLightAttackComboNames = new string[3];
        public float oneHandedLightAttacksDamageMultiplier = 1f;
        public float oneHandedLightAttacksPoiseDamageMultiplier = 1f;

        [Header("Heavy one hand combo")]
        public string[] OneHandedHeavyAttackComboNames = new string[3];
        public float oneHandedHeavyAttacksDamageMultiplier = 1.7f;
        public float oneHandedHeavyAttacksPoiseDamageMultiplier = 2f;

        [Header("One Handed Running attack")]
        public string oneHandedRunningAttackName;
        public float oneHandedRunningAttackDamageMultiplier = 1.2f;
        public float oneHandedRunningAttackPoiseDamageMultiplier = 1.4f;

        [Header("One Handed Rolling attack")]
        public string oneHandedRollingAttackName;
        public float oneHandedRollingAttackDamageMultiplier = .9f;
        public float oneHandedRollingAttackPoiseDamageMultiplier = 1.2f;

        [Header("Backstab attack")]
        public string backstabAttackName;
        public string backstabVictimAnimation;
        public float backstabtAttackDamageMultiplier = 2.4f;

        [Header("Riposte attack")]
        public string riposteAttackName;
        public string riposteVictimAnimation;
        public float ripostetAttackDamageMultiplier = 3f;

        [Header("Other")]
        public float knockbackStrength = 3.2f;
        public float flinchStrength = 25f;

        //Stats scaling

    }
}