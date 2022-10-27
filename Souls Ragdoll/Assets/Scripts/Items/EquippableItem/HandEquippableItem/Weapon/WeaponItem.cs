using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newWeaponItem", menuName = "Item/Hand Item/Weapon Item")]
    public class WeaponItem : HandEquippableItem
    {
        [Header("One Handed Attack Animations")]
        public string OneHandLightAttackOne;
        public string OneHandHeavyAttackOne;
    }
}