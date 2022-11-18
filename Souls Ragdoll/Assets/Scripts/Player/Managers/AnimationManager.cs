using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello {
    public class AnimationManager : NetworkBehaviour
    {
        private Animator animator;
        private PlayerManager playerManager;

        private int normalMovementAmountHash;
        private int strafeMovementAmountHash;
        private int onGroundHash;
        private int attackingWithLeftHash;
        private int blockingWithLeftHash;
        private int changingLeftItemHash;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            Initialize();
        }

        public void Initialize()
        {
            playerManager = GetComponent<PlayerManager>();
            normalMovementAmountHash = Animator.StringToHash("NormalMovementAmount");
            strafeMovementAmountHash = Animator.StringToHash("StrafeMovementAmount");
            onGroundHash = Animator.StringToHash("OnGround");
            attackingWithLeftHash = Animator.StringToHash("AttackingWithLeft");
            blockingWithLeftHash = Animator.StringToHash("BlockingWithLeft");
            changingLeftItemHash = Animator.StringToHash("ChangingLeftItem");

            animator.applyRootMotion = true;
        }

        public void UpdateMovementAnimatorValues(float normal, float strafe, float time)
        {

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

        public void UpdateChangingLeftItemValue(bool changingLeftItem)
        {
            animator.SetBool(changingLeftItemHash, changingLeftItem);
        }

        public void PlayTargetAnimation(string targetAnimation, float fadeDuration, bool isStuckInAnimation)
        {
            PlayTargetAnimationServerRpc(targetAnimation, fadeDuration, isStuckInAnimation);
            if(IsOwner)
            {
                playerManager.playerIsStuckInAnimation = isStuckInAnimation;
                animator.CrossFade(targetAnimation, fadeDuration);
            }
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

        [ServerRpc]
        private void PlayTargetAnimationServerRpc(string targetAnimation, float fadeDuration, bool isStuckInAnimation)
        {
            //Debug.Log($"Client: {playerManager.OwnerClientId}, sending animation: {targetAnimation} to server");
            PlayTargetAnimationClientRpc(targetAnimation, fadeDuration, isStuckInAnimation);
        }

        [ClientRpc]
        private void PlayTargetAnimationClientRpc(string targetAnimation, float fadeDuration, bool isStuckInAnimation)
        {
            //Debug.Log($"Client: {playerManager.OwnerClientId}, playing animation: {targetAnimation} to server");
            if (IsOwner) return;
            playerManager.playerIsStuckInAnimation = isStuckInAnimation;
            animator.CrossFade(targetAnimation, fadeDuration);
        }

    }
}
