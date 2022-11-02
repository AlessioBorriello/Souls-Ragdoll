using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newWeaponItem", menuName = "Item/Hand Item/Weapon Item")]
    public class WeaponItem : HandEquippableItem
    {
        [Header("Movement speed multiplier")]
        public float movementSpeedMultiplier = 2.5f; //How fast the player moves forward when attacking

        //Light combo
        public string[] OneHandedLightAttackCombo = new string[3];

        //Heavy combo
        public string[] OneHandedHeavyAttackCombo = new string[3];

        [Header("One Handed Running attack")]
        public string OneHandedRunningAttack;

        [Header("One Handed Rolling attack")]
        public string OneHandedRollingAttack;
    }
}