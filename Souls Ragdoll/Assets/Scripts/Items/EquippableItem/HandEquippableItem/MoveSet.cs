using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newMoveSet", menuName = "Item/Hand Item/Weapon/Weapon MoveSet")]
    public class MoveSet : ScriptableObject
    {
        [Header("One handed attacks")]
        public AttackMove[] oneHandedLightCombo;
        public AttackMove[] oneHandedHeavyCombo;
        public AttackMove oneHandedRunningAttack;
        public AttackMove oneHandedRollingAttack;

        [Header("Other attacks")]
        public AttackMove oneHandedBackstabAttack;
        public AttackMove oneHandedRiposteAttack;
    }
}
