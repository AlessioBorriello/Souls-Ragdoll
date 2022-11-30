using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello {
    public class AnimationManager : MonoBehaviour
    {
        private Animator animator;
        private PlayerManager playerManager;
        private PlayerNetworkManager networkManager;

        private int normalMovementAmountHash;
        private int strafeMovementAmountHash;
        private int onGroundHash;
        private int attackingWithLeftHash;
        private int blockingWithLeftHash;
        private int parryingWithLeftHash;
        private int changingLeftItemHash;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            Initialize();
        }

        public void Initialize()
        {
            playerManager = GetComponent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
            normalMovementAmountHash = Animator.StringToHash("NormalMovementAmount");
            strafeMovementAmountHash = Animator.StringToHash("StrafeMovementAmount");
            onGroundHash = Animator.StringToHash("OnGround");
            attackingWithLeftHash = Animator.StringToHash("AttackingWithLeft");
            blockingWithLeftHash = Animator.StringToHash("BlockingWithLeft");
            parryingWithLeftHash = Animator.StringToHash("ParryingWithLeft");
            changingLeftItemHash = Animator.StringToHash("ChangingLeftItem");

            if(playerManager.IsOwner) animator.applyRootMotion = true;
        }

        public void UpdateMovementAnimatorValues(float normal, float strafe, float time)
        {
            if (playerManager.IsOwner)
            {
                networkManager.netNormalMovementAmount.Value = normal;
                networkManager.netStrafeMovementAmount.Value = strafe;
            }

            animator.SetFloat(normalMovementAmountHash, normal, time, Time.deltaTime);
            animator.SetFloat(strafeMovementAmountHash, strafe, time, Time.deltaTime);

        }

        public void UpdateOnGroundValue(bool onGround)
        {
            animator.SetBool(onGroundHash, onGround);
        }

        public void UpdateAttackingWithLeftValue(bool attackingWithLeft)
        {
            animator.SetBool(attackingWithLeftHash, attackingWithLeft);
        }

        public void UpdateBlockingWithLeftValue(bool blockingWithLeft)
        {
            animator.SetBool(blockingWithLeftHash, blockingWithLeft);
        }

        public void UpdateParryingWithLeftValue(bool blockingWithLeft)
        {
            animator.SetBool(parryingWithLeftHash, blockingWithLeft);
        }

        public void UpdateChangingLeftItemValue(bool changingLeftItem)
        {
            animator.SetBool(changingLeftItemHash, changingLeftItem);
        }

        public void PlayTargetAnimation(string targetAnimation, float fadeDuration, bool isStuckInAnimation)
        {
            playerManager.playerIsStuckInAnimation = isStuckInAnimation;
            animator.CrossFade(targetAnimation, fadeDuration);
        }

        public void PlayTargetAnimation(string targetAnimation, float fadeDuration, bool isStuckInAnimation, int layer)
        {
            playerManager.playerIsStuckInAnimation = isStuckInAnimation;
            animator.CrossFade(targetAnimation, fadeDuration, layer);
        }

        public Animator GetAnimator()
        {
            return animator;
        }

    }
}
