using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello {
    public class AnimationManager : MonoBehaviour
    {
        [HideInInspector] public Animator animator;

        int normalMovementAmount;
        int strafeMovementAmount;

        public void Initialize()
        {
            animator = GetComponentInChildren<Animator>();
            normalMovementAmount = Animator.StringToHash("NormalMovementAmount");
            strafeMovementAmount = Animator.StringToHash("StrafeMovementAmount");
        }

        public void UpdateMovementAnimatorValues(float normal, float strafe)
        {

            animator.SetFloat(normalMovementAmount, normal, .1f, Time.deltaTime);
            animator.SetFloat(strafeMovementAmount, strafe, .1f, Time.deltaTime);

        }

        public void PlayTargetAnimation(string targetAnimation, bool disablePlayerInteraction, float fadeDuration)
        {
            animator.SetBool("DisablePlayerInteraction", disablePlayerInteraction);
            animator.CrossFade(targetAnimation, fadeDuration);
        }

    }
}
