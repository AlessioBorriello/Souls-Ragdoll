using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Windows;

namespace AlessioBorriello
{
    public class PlayerManager : CharacterManager
    {
        [Header("Set up")]
        public PlayerData playerData; //Player data reference
        public Transform groundCheckTransform; //Player's ground check's transform

        private GameObject animatedPlayer; //Player data reference
        private Rigidbody physicalHips; //Player's physical hips
        private Transform animatedHips; //Player's animated hips

        //Components
        private InputManager inputManager;
        private Transform cameraTransform;
        private AnimationManager animationManager;
        private PlayerLocomotionManager locomotionManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerInventoryManager inventoryManager;
        private PlayerCombatManager combatManager;
        private PlayerStatsManager statsManager;
        private PlayerCollisionManager collisionManager;
        private UIManager uiManager;

        [HideInInspector] public Transform lockedTarget;

        #region Flags
        [Header("General flags")]
        public bool isClient = true;
        public bool playerIsStuckInAnimation = false;
        public bool consumeInputs = true;
        public bool isOnGround = true;
        public bool isKnockedOut = false;

        [Header("Locomotion flags")]
        public bool canRotate = true;
        public bool shouldSlide = false; //If the friction should be enabled or not
        public bool isRolling = false;
        public bool isBackdashing = false;
        public bool isSprinting = false;

        [Header("Combat flag")]
        public bool isAttacking = false;
        public bool isBlocking = false;
        public bool canLockOn = true;
        public bool isLockingOn = false;
        #endregion

        private void Awake()
        {
            inputManager = GetComponent<InputManager>();
            animationManager = GetComponent<AnimationManager>();
            ragdollManager = GetComponent<ActiveRagdollManager>();
            collisionManager = GetComponent<PlayerCollisionManager>();
            locomotionManager = GetComponent<PlayerLocomotionManager>();
            inventoryManager = GetComponent<PlayerInventoryManager>();
            statsManager = GetComponent<PlayerStatsManager>();
            combatManager = GetComponent<PlayerCombatManager>();

            uiManager = FindObjectOfType<UIManager>();

            animatedPlayer = transform.Find("AnimatedPlayer").gameObject;
            physicalHips = transform.Find("PhysicalPlayer/Armature/Hip").GetComponent<Rigidbody>();
            animatedHips = transform.Find("AnimatedPlayer/Armature/Hip");

            cameraTransform = Camera.main.transform;
        }

        private void FixedUpdate()
        {
            //Move player with animation (Order is important for some reason)
            locomotionManager.ApplyGravity();
            locomotionManager.MovePlayerWithAnimation();

        }

        private void Update()
        {

            //Locomotion
            locomotionManager.HandleLocomotion();

            //Combat
            combatManager.HandleCombat();

            //QuickSlots
            inventoryManager.HandleQuickSlots();

    }

        public InputManager GetInputManager()
        {
            return inputManager;
        }

        public AnimationManager GetAnimationManager()
        {
            return animationManager;
        }

        public PlayerLocomotionManager GetLocomotionManager()
        {
            return locomotionManager;
        }

        public PlayerInventoryManager GetInventoryManager()
        {
            return inventoryManager;
        }

        public ActiveRagdollManager GetRagdollManager()
        {
            return ragdollManager;
        }

        public PlayerCombatManager GetCombatManager()
        {
            return combatManager;
        }

        public PlayerStatsManager GetStatsManager()
        {
            return statsManager;
        }

        public PlayerCollisionManager GetCollisionManager()
        {
            return collisionManager;
        }

        public UIManager GetUiManager()
        {
            return uiManager;
        }

        public Transform GetCameraTransform()
        {
            return cameraTransform;
        }

        public GameObject GetAnimatedPlayer()
        {
            return animatedPlayer;
        }

        public Rigidbody GetPhysicalHips()
        {
            return physicalHips;
        }

        public Transform GetAnimatedHips()
        {
            return animatedHips;
        }

    }
}
