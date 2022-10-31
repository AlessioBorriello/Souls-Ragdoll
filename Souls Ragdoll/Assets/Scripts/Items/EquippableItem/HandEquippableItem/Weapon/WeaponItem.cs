using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newWeaponItem", menuName = "Item/Hand Item/Weapon Item")]
    public class WeaponItem : HandEquippableItem
    {
        [Header("One Handed Light Attack Combo")]
        public string[] OneHandedLightAttackCombo = new string[3];

        [Header("One Handed Heavy Attack Combo")]
        public string[] OneHandedHeavyAttackCombo = new string[3];
    }
}