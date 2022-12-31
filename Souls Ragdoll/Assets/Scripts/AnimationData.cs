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
        public ClipTransition animationClip;
        public ClipTransition mirroredAnimationClip;
        public AnimationEventStruct[] events;

        private void OnEnable()
        {
            CheckIfClipsMatch();
        }

        private void CheckIfClipsMatch()
        {
            float fadeDuration = animationClip.FadeDuration;
            float fadeDurationMirrored = mirroredAnimationClip.FadeDuration;

            if ((!double.IsNaN(fadeDuration) && !double.IsNaN(fadeDurationMirrored)) && fadeDuration != fadeDurationMirrored) Debug.Log($"Fade durations for the {name} animation data do not match ({fadeDuration} and {fadeDurationMirrored})");

            float speed = animationClip.Speed;
            float speedMirrored = mirroredAnimationClip.Speed;

            if ((!double.IsNaN(speed) && !double.IsNaN(speedMirrored)) && speed != speedMirrored) Debug.Log($"Speeds for the {name} animation data do not match ({speed} and {speedMirrored})");

            float startTime = animationClip.NormalizedStartTime;
            float startTimeMirrored = mirroredAnimationClip.NormalizedStartTime;

            if ((!double.IsNaN(startTime) && !double.IsNaN(startTimeMirrored)) && startTime != startTimeMirrored) Debug.Log($"Start times for the {name} animation data do not match ({startTime} and {startTimeMirrored})");

            float endTime = animationClip.Events.NormalizedEndTime;
            float endTimeMirrored = mirroredAnimationClip.Events.NormalizedEndTime;

            if ((!double.IsNaN(endTime) && !double.IsNaN(endTimeMirrored)) && endTime != endTimeMirrored) Debug.Log($"End times for the {name} animation data do not match ({endTime} and {endTimeMirrored})");
        }
    }
}