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
        [Header("Moveset")]
        public WeaponMoveset moveset;

        //Stats scaling

    }
}