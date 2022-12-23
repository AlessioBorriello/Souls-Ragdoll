using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newShieldItem", menuName = "Item/Hand Item/Shield Item")]
    public class ShieldItem : HandEquippableItem
    {
        public float parryStaminaCost = 35f;

        public bool canParry = true;
        public float parryDuration = .13f;
        public AnimationData parryAnimationData;
    }
}