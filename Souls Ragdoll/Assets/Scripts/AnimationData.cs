using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newAnimationData", menuName = "Override Animation/Animation Data")]
    public class AnimationData : ScriptableObject
    {
        public AnimationClip animationClip;
        public AnimationClip mirroredAnimationClip;
        public AnimationEventStruct[] events;
        public float fadeDuration;
        public float endTime;
        public float defaultAnimationSpeed;
    }
}