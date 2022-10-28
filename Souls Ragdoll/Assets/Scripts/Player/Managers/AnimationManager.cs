using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello {
    public class AnimationManager : MonoBehaviour
    {
        [HideInInspector] public Animator animator;
        [HideInInspector] public PlayerManager playerManager;

        int normalMovementAmount;
        int strafeMovementAmount;

        public void Initialize()
        {
            animator = GetComponentInChildren<Animator>();
            playerManager = GetComponent<PlayerManager>();
            normalMovementAmount = Animator.StringToHash("NormalMovementAmount");
            strafeMovementAmount = Animator.StringToHash("StrafeMovementAmount");
        }

        public void UpdateMovementAnimatorValues(float normal, float strafe, float time)
        {

            animator.SetFloat(normalMovementAmount, normal, time, Time.deltaTime);
            animator.SetFloat(strafeMovementAmount, strafe, time, Time.deltaTime);

        }

        public void UpdateOnGroundValue(bool onGround)
        {
            playerManager.animationManager.animator.SetBool("onGround", onGround);
        }

        public void PlayTargetAnimation(string targetAnimation, float fadeDuration)
        {
            animator.CrossFade(targetAnimation, fadeDuration);
        }

    }
}
