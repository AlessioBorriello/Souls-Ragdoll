using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello {
    public class AnimationManager : MonoBehaviour
    {
        [HideInInspector] public Animator animator;

        int normalMovementAmount;
        int strafeMovementAmount;

        public bool disablePlayerInteraction = false;

        private void Update()
        {
            disablePlayerInteraction = animator.GetBool("DisablePlayerInteraction");
        }

        public void Initialize()
        {
            animator = GetComponentInChildren<Animator>();
            normalMovementAmount = Animator.StringToHash("NormalMovementAmount");
            strafeMovementAmount = Animator.StringToHash("StrafeMovementAmount");
        }

        public void UpdateMovementAnimatorValues(float normal, float strafe)
        {
            float n = GetClampedMovementAmount(normal);
            animator.SetFloat(normalMovementAmount, n, .1f, Time.deltaTime);

            float s = GetClampedMovementAmount(strafe);
            animator.SetFloat(strafeMovementAmount, s, .1f, Time.deltaTime);

        }

        private float GetClampedMovementAmount(float amount)
        {
            float clampedAmount = 0;
            if (Mathf.Abs(amount) > 0 && Mathf.Abs(amount) < .55f)
            {
                clampedAmount = .5f * Mathf.Sign(amount);
            }
            else if (Mathf.Abs(amount) > .55f)
            {
                clampedAmount = 1 * Mathf.Sign(amount);
            }

            return clampedAmount;
        }

        public void PlayTargetAnimation(string targetAnimation, bool disablePlayerInteraction, float fadeDuration)
        {
            animator.SetBool("DisablePlayerInteraction", disablePlayerInteraction);
            animator.CrossFade(targetAnimation, fadeDuration);
        }

    }
}
