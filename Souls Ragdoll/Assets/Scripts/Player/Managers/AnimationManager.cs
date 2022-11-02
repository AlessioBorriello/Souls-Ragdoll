using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello {
    public class AnimationManager : MonoBehaviour
    {
        public Animator animator;
        [HideInInspector] public PlayerManager playerManager;

        int normalMovementAmountHash;
        int strafeMovementAmountHash;
        int onGroundHash;
        int attackingWithLeftHash;

        public void Initialize()
        {
            playerManager = GetComponent<PlayerManager>();
            normalMovementAmountHash = Animator.StringToHash("NormalMovementAmount");
            strafeMovementAmountHash = Animator.StringToHash("StrafeMovementAmount");
            onGroundHash = Animator.StringToHash("onGround");
            attackingWithLeftHash = Animator.StringToHash("attackingWithLeft");

            animator.applyRootMotion = true;
        }

        public void UpdateMovementAnimatorValues(float normal, float strafe, float time)
        {

            animator.SetFloat(normalMovementAmountHash, normal, time, Time.deltaTime);
            animator.SetFloat(strafeMovementAmountHash, strafe, time, Time.deltaTime);

        }

        public void UpdateOnGroundValue(bool onGround)
        {
            playerManager.animationManager.animator.SetBool(onGroundHash, onGround);
        }

        public void UpdateAttackingWithLeftValue(bool attackingWithLeft)
        {
            playerManager.animationManager.animator.SetBool(attackingWithLeftHash, attackingWithLeft);
        }

        public void PlayTargetAnimation(string targetAnimation, float fadeDuration)
        {
            animator.CrossFade(targetAnimation, fadeDuration);
        }

    }
}
