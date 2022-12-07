using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;

namespace AlessioBorriello {
    public class AnimationManager : MonoBehaviour
    {

        [SerializeField] private MixerTransition2DAsset LocomotionBlendTree;

        [Header("Masks")]
        [SerializeField] private AvatarMask upperBodyLeftArmMask;
        [SerializeField] private AvatarMask upperBodyRightArmMask;
        [SerializeField] private AvatarMask leftArmMask;
        [SerializeField] private AvatarMask rightArmMask;

        [Header("Wheights")]
        [SerializeField, Range(0, 1)] private float overrideLayerWeight = 1f;
        [SerializeField, Range(0, 1)] private float upperBodyArmsLayerWeight = .85f;
        [SerializeField, Range(0, 1)] private float armsLayerWeight = .65f;

        private AnimancerComponent animancer;
        private MixerParameterTweenVector2 locomotionMixerTween;
        private MixerState<Vector2> locomotionMixer;

        private AnimancerLayer locomotionLayer;
        private AnimancerLayer overrideLayer;
        private AnimancerLayer upperBodyLeftArmOverride;
        private AnimancerLayer upperBodyRightArmOverride;

        private AnimancerLayer leftArmOverride;
        private AnimancerLayer rightArmOverride;

        private Animator animator;
        private PlayerManager playerManager;
        private PlayerWeaponManager weaponManager;
        private PlayerNetworkManager networkManager;
        private PlayerAnimationsDatabase animationsDatabase;

        private Action onOverrideExit = null;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            Initialize();
        }

        public void Initialize()
        {
            playerManager = GetComponent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
            weaponManager = playerManager.GetWeaponManager();
            animationsDatabase = GetComponent<PlayerAnimationsDatabase>();
            animancer = GetComponentInChildren<AnimancerComponent>();
            locomotionMixer = (MixerState<Vector2>)LocomotionBlendTree.CreateState();

            AnimancerPlayable.LayerList.SetMinDefaultCapacity(7);

            //Create layers
            locomotionLayer = animancer.Layers[0];
            locomotionLayer.SetDebugName("Locomotion layer");
            locomotionLayer.SetWeight(1);
            locomotionLayer.ApplyFootIK = true;

            //Arms
            leftArmOverride = animancer.Layers[1];
            leftArmOverride.SetDebugName("Left arm layer");
            leftArmOverride.SetWeight(armsLayerWeight);
            leftArmOverride.SetMask(leftArmMask);

            rightArmOverride = animancer.Layers[2];
            rightArmOverride.SetDebugName("Right arm layer");
            rightArmOverride.SetWeight(armsLayerWeight);
            rightArmOverride.SetMask(rightArmMask);

            //Upper body - arms
            upperBodyLeftArmOverride = animancer.Layers[3];
            upperBodyLeftArmOverride.SetDebugName("Upper Body - Left arm layer");
            upperBodyLeftArmOverride.SetWeight(upperBodyArmsLayerWeight);
            upperBodyLeftArmOverride.SetMask(upperBodyLeftArmMask);

            upperBodyRightArmOverride = animancer.Layers[4];
            upperBodyRightArmOverride.SetDebugName("Upper Body - Right arm layer");
            upperBodyRightArmOverride.SetWeight(upperBodyArmsLayerWeight);
            upperBodyRightArmOverride.SetMask(upperBodyRightArmMask);

            //Override
            overrideLayer = animancer.Layers[5];
            overrideLayer.SetDebugName("Override actions layer");
            overrideLayer.SetWeight(overrideLayerWeight);




            //Play locomotion
            locomotionLayer.Play(locomotionMixer);
            locomotionMixerTween = new MixerParameterTweenVector2(locomotionMixer);

            if(playerManager.IsOwner) animator.applyRootMotion = true;
        }

        public void UpdateMovementAnimatorValues(float normal, float strafe, float time)
        {
            if (playerManager.IsOwner)
            {
                networkManager.netNormalMovementAmount.Value = normal;
                networkManager.netStrafeMovementAmount.Value = strafe;
            }

            //Smooth values
            Vector2 movementVector = new Vector2(strafe, normal);
            BlendMixerParameter(movementVector, time);

        }

        private void BlendMixerParameter(Vector2 parameter, float duration)
        {
            // Start interpolating the Mixer parameter.
            locomotionMixerTween.Start(parameter, duration);
        }

        public void UpdateChangingLeftItemValue(bool changingLeftItem)
        {
            //animator.SetBool(changingLeftItemHash, changingLeftItem);
        }

        public void PlayTargetAnimation(string targetAnimation, float fadeDuration, bool isStuckInAnimation)
        {
            //playerManager.playerIsStuckInAnimation = isStuckInAnimation;
            //animator.CrossFade(targetAnimation, fadeDuration);
        }

        public void PlayOverrideAnimation(string animationName, Action newOnOverrideEnter, Action newOnOverrideExit)
        {
            if (animationName == "") return;
            ClipTransition animation = animationsDatabase.GetClipTransition(animationName);
            if (animation == null) return;

            //Play old override exit if the player was still in an override animation (override was interrupted)
            if(onOverrideExit != null && playerManager.isInOverrideAnimation) EarlyExitOverrideAnimation();

            AnimancerState state = overrideLayer.Play(animation);

            //Play new override enter
            newOnOverrideEnter();

            //Set override exit as end event and update override exit (if it gets interrupted)
            state.Events.OnEnd = newOnOverrideExit;
            onOverrideExit = newOnOverrideExit;
        }

        public void PlayUpperBodyArmsOverrideAnimation(string animationName, bool leftHand)
        {
            animationName += (leftHand) ? "Left" : "Right";
            if (animationName == "") return;
            ClipTransition animation = animationsDatabase.GetClipTransition(animationName);
            if (animation == null) return;

            AnimancerState state;
            if (leftHand) state = upperBodyLeftArmOverride.Play(animation);
            else state = upperBodyRightArmOverride.Play(animation);

            if (leftHand) upperBodyLeftArmOverride.StartFade(upperBodyArmsLayerWeight);
            else upperBodyRightArmOverride.StartFade(upperBodyArmsLayerWeight);
        }

        public void PlayArmsOverrideAnimation(string animationName, bool leftHand)
        {
            animationName += (leftHand) ? "Left" : "Right";
            if (animationName == "") return;
            ClipTransition animation = animationsDatabase.GetClipTransition(animationName);
            if (animation == null) return;

            AnimancerState state;
            if (leftHand) state = leftArmOverride.Play(animation, .1f, FadeMode.FromStart);
            else state = rightArmOverride.Play(animation, .1f, FadeMode.FromStart);

            if(leftHand) leftArmOverride.StartFade(armsLayerWeight);
            else rightArmOverride.StartFade(armsLayerWeight);
        }

        public void EarlyExitOverrideAnimation()
        {
            if(onOverrideExit != null) onOverrideExit();
        }

        public void FadeOutOverrideAnimation(float time)
        {
            overrideLayer.StartFade(0, time);
        }

        public void FadeOutUpperBodyArmsOverrideAnimation(float time, bool leftHand)
        {
            if (leftHand) upperBodyLeftArmOverride.StartFade(0, time);
            else upperBodyRightArmOverride.StartFade(0, time);

            leftArmOverride.SetWeight(armsLayerWeight);
            rightArmOverride.SetWeight(armsLayerWeight);
        }

        public void FadeOutArmsOverrideAnimation(float time, bool leftHand)
        {
            if (leftHand) leftArmOverride.StartFade(0, time);
            else rightArmOverride.StartFade(0, time);
        }

        public void PlayTargetAnimation(string targetAnimation, float fadeDuration, bool isStuckInAnimation, int layer)
        {
            //playerManager.playerIsStuckInAnimation = isStuckInAnimation;
            //animator.CrossFade(targetAnimation, fadeDuration, layer);
        }

        public Animator GetAnimator()
        {
            return animancer.Animator;
        }

    }
}
