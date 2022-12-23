using Animancer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newMoveSet", menuName = "Item/Hand Item/Weapon/Attack Move")]
    public class AttackMove : ScriptableObject
    {
        public int id;
        public string animationName;
        public float timeToRotateAfterAttackStart = .2f; //How much time the player is still allowed to rotate for after the attack start
        public AnimationData animationData;

        public float staminaCostMultiplier;
        public float damageMultiplier;
        public float poiseDamageMultiplier;
        public float staminaDamageMultiplier;

        public float animationSpeed;
        public float movementSpeedMultiplier;

        public float knockbackStrengthMultiplier;
        public float flinchStrengthMultiplier;

        public string victimStaggerAnimation;

        [Range(1, 4)] public int levelNeededToDeflect = 1;
    }
}