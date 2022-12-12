using Animancer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newMoveSet", menuName = "Item/Hand Item/Weapon/Attack Move")]
    public class AttackMove : ScriptableObject
    {
        public ClipTransition animation;
        public int id;
        public string animationName;

        public float staminaCostMultiplier;
        public float damageMultiplier;
        public float poiseDamageMultiplier;
        public float staminaDamageMultiplier;

        public float speed;
        public float movementSpeedMultiplier;

        public float knockbackStrengthMultiplier;
        public float flinchStrengthMultiplier;

        public string victimStaggerAnimation;
    }
}