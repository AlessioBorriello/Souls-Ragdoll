using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Windows;

namespace AlessioBorriello {

    public class AnimationManager : MonoBehaviour
    {

        [SerializeField] private MixerTransition2DAsset LocomotionBlendTree;

        [Header("Masks")]
        [SerializeField] private AvatarMask leftArmMask;
        [SerializeField] private AvatarMask rightArmMask;
        [SerializeField] private AvatarMask bothArmsMask;
        [SerializeField] private AvatarMask upperBodyLeftArmMask;
        [SerializeField] private AvatarMask upperBodyRightArmMask;
        [SerializeField] private AvatarMask upperBodyMask;

        [Header("Wheights")]
        [SerializeField, Range(0, 1)] private float armsLayerWeight = .65f;
        [SerializeField, Range(0, 1)] private float bothArmsLayerWeight = .85f;
        [SerializeField, Range(0, 1)] private float upperBodyArmsLayerWeight = .85f;
        [SerializeField, Range(0, 1)] private float upperBodyLayerWeight = .85f;
        [SerializeField, Range(0, 1)] private float overrideLayerWeight = 1f;

        private AnimancerComponent animancer;
        private MixerParameterTweenVector2 locomotionMixerTween;
        private MixerState<Vector2> locomotionMixer;

        private AnimancerLayer locomotionLayer;

        private AnimancerLayer leftArmOverride;
        private AnimancerLayer rightArmOverride;
        private AnimancerLayer bothArmsOverride;

        private AnimancerLayer upperBodyLeftArmOverride;
        private AnimancerLayer upperBodyRightArmOverride;
        private AnimancerLayer upperBodyOverride;

        private AnimancerLayer overrideLayer;

        private Animator animator;
        private PlayerManager playerManager;
        private PlayerWeaponManager weaponManager;
        private PlayerNetworkManager networkManager;
        private AnimationEventsManager animationEventsManager;
        private PlayerAnimationsDatabase animationsDatabase;

        //Max movement values
        public float defaultMaxNormalMovementValue = 2f;
        public float defaultMaxStrafeMovementValue = 1f;
        private float maxNormalMovementValue;
        private float maxStrafeMovementValue;

        private Action onOverrideExit = null;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            Initialize();
            SetMaxMovementValues(defaultMaxNormalMovementValue, defaultMaxStrafeMovementValue);
        }

        public void Initialize()
        {
            playerManager = GetComponent<PlayerManager>();
            networkManager = playerManager.GetNetworkManager();
            animationEventsManager = playerManager.GetAnimationEventsManager();
            weaponManager = playerManager.GetWeaponManager();
            animationsDatabase = playerManager.GetAnimationDatabase();
            animancer = GetComponentInChildren<AnimancerComponent>();
            locomotionMixer = (MixerState<Vector2>)LocomotionBlendTree.CreateState();

            AnimancerPlayable.LayerList.SetMinDefaultCapacity(8);

            //Create layers
            locomotionLayer = animancer.Layers[(int)OverrideLayers.locomotionLayer];
            locomotionLayer.SetDebugName("Locomotion layer");
            locomotionLayer.SetWeight(1);
            locomotionLayer.ApplyFootIK = true;

            //Arms
            leftArmOverride = animancer.Layers[(int)OverrideLayers.leftArmLayer];
            leftArmOverride.SetDebugName("Left arm layer");
            leftArmOverride.SetWeight(armsLayerWeight);
            leftArmOverride.SetMask(leftArmMask);

            rightArmOverride = animancer.Layers[(int)OverrideLayers.rightArmLayer];
            rightArmOverride.SetDebugName("Right arm layer");
            rightArmOverride.SetWeight(armsLayerWeight);
            rightArmOverride.SetMask(rightArmMask);

            bothArmsOverride = animancer.Layers[(int)OverrideLayers.bothArmsLayer];
            bothArmsOverride.SetDebugName("Both arms layer");
            bothArmsOverride.SetWeight(bothArmsLayerWeight);
            bothArmsOverride.SetMask(bothArmsMask);

            //Upper body - arms
            upperBodyLeftArmOverride = animancer.Layers[(int)OverrideLayers.upperBodyLeftArmLayer];
            upperBodyLeftArmOverride.SetDebugName("Upper Body - Left arm layer");
            upperBodyLeftArmOverride.SetWeight(upperBodyArmsLayerWeight);
            upperBodyLeftArmOverride.SetMask(upperBodyLeftArmMask);

            upperBodyRightArmOverride = animancer.Layers[(int)OverrideLayers.upperBodyRightArmLayer];
            upperBodyRightArmOverride.SetDebugName("Upper Body - Right arm layer");
            upperBodyRightArmOverride.SetWeight(upperBodyArmsLayerWeight);
            upperBodyRightArmOverride.SetMask(upperBodyRightArmMask);

            //Upper body
            upperBodyOverride = animancer.Layers[(int)OverrideLayers.upperBodyLayer];
            upperBodyOverride.SetDebugName("Upper Body");
            upperBodyOverride.SetWeight(upperBodyLayerWeight);
            upperBodyOverride.SetMask(upperBodyMask);

            //Override
            overrideLayer = animancer.Layers[(int)OverrideLayers.overrideLayer];
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
            Vector2 movementVector = new Vector2(Mathf.Min(strafe, maxStrafeMovementValue), Mathf.Min(normal, maxNormalMovementValue));
            BlendMixerParameter(movementVector, time);

        }

        public void SetMaxMovementValues(float normal, float strafe)
        {
            maxNormalMovementValue = normal;
            maxStrafeMovementValue = strafe;
        }

        private void BlendMixerParameter(Vector2 parameter, float duration)
        {
            // Start interpolating the Mixer parameter.
            locomotionMixerTween.Start(parameter, duration);
        }

        public AnimancerState PlayOverrideAnimation(string animationName, Action newOnOverrideEnter = null, Action newOnOverrideExit = null, OverrideLayers layer = OverrideLayers.overrideLayer)
        {
            if (animationName == "") return null;
            ClipTransition animation = animationsDatabase.GetClipTransition(animationName);
            if (animation == null) return null;

            //Play old override exit if the player was still in an override animation (override was interrupted)
            if(onOverrideExit != null && playerManager.isInOverrideAnimation) EarlyExitOverrideAnimation();

            AnimancerState state = animancer.Layers[(int)layer].Play(animation);

            //Play new override enter
            if(newOnOverrideEnter != null) newOnOverrideEnter();

            //Set override exit as end event and update override exit (if it gets interrupted)
            if(newOnOverrideExit != null)
            {
                state.Events.OnEnd = newOnOverrideExit;
                onOverrideExit = newOnOverrideExit;
            }

            //Set target weight
            animancer.Layers[(int)layer].TargetWeight = GetDefaultLayerWeight(layer);

            return state;
        }

        public AnimancerState PlayOverrideAnimation(string animationName, float speed, Action newOnOverrideEnter = null, Action newOnOverrideExit = null, OverrideLayers layer = OverrideLayers.overrideLayer)
        {
            if (animationName == "") return null;
            AnimancerState state = PlayOverrideAnimation(animationName, newOnOverrideEnter, newOnOverrideExit, layer);
            if (state == null) return null;

            state.Speed = speed;

            return state;
        }

        public AnimancerState PlayOverrideAnimation(AnimationData animationData, Action newOnOverrideEnter = null, Action newOnOverrideExit = null, OverrideLayers layer = OverrideLayers.overrideLayer, bool mirrored = false)
        {
            //Choose animation clip
            AnimationClip animationClip;
            if (!mirrored) animationClip = animationData.animationClip;
            else animationClip = (animationData.mirroredAnimationClip != null) ? animationData.mirroredAnimationClip : animationData.animationClip;

            if (animationClip == null) return null;

            //Play old override exit if the player was still in an override animation (override was interrupted)
            if (onOverrideExit != null && playerManager.isInOverrideAnimation) EarlyExitOverrideAnimation();

            AnimancerState state = animancer.Layers[(int)layer].Play(animationClip, animationData.fadeDuration);

            //Set animation events
            state.Events = animationEventsManager.GetEventSequence(animationData.events, animationData.endTime);

            //Play new override enter
            if (newOnOverrideEnter != null) newOnOverrideEnter();

            state.Speed = animationData.defaultAnimationSpeed;

            //Set override exit as end event and update override exit (if it gets interrupted)
            if (newOnOverrideExit != null)
            {
                state.Events.OnEnd = newOnOverrideExit;
                onOverrideExit = newOnOverrideExit;
            }

            //Set target weight
            animancer.Layers[(int)layer].TargetWeight = GetDefaultLayerWeight(layer);

            return state;
        }

        public AnimancerState PlayOverrideAnimation(AnimationData animationData, float speed, Action newOnOverrideEnter = null, Action newOnOverrideExit = null, OverrideLayers layer = OverrideLayers.overrideLayer, bool mirrored = false)
        {
            AnimancerState state = PlayOverrideAnimation(animationData, newOnOverrideEnter, newOnOverrideExit, layer, mirrored);
            if (state == null) return null;

            state.Speed = speed;

            return state;
        }

        public void EarlyExitOverrideAnimation()
        {
            if(onOverrideExit != null) onOverrideExit();
        }

        public void FadeOutOverrideAnimation(float time, OverrideLayers layer = OverrideLayers.overrideLayer)
        {
            animancer.Layers[(int)layer].StartFade(0, time);
        }

        private float GetDefaultLayerWeight(OverrideLayers layer)
        {
            switch (layer)
            {
                case OverrideLayers.leftArmLayer: return armsLayerWeight;
                case OverrideLayers.rightArmLayer: return armsLayerWeight;
                case OverrideLayers.bothArmsLayer: return bothArmsLayerWeight;
                case OverrideLayers.upperBodyLeftArmLayer: return upperBodyArmsLayerWeight;
                case OverrideLayers.upperBodyRightArmLayer: return upperBodyArmsLayerWeight;
                case OverrideLayers.upperBodyLayer: return upperBodyLayerWeight;
                case OverrideLayers.overrideLayer: return overrideLayerWeight;
                default: return 1f;
            }
        }

        public Animator GetAnimator()
        {
            return animancer.Animator;
        }

    }

    public enum OverrideLayers
    {
        locomotionLayer,
        leftArmLayer,
        rightArmLayer,
        bothArmsLayer,
        upperBodyLeftArmLayer,
        upperBodyRightArmLayer,
        upperBodyLayer,
        overrideLayer
    }
}
