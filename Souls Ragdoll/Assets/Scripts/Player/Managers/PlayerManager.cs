using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
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

        private GameObject animatedPlayer; //Animated player reference
        private GameObject physicalPlayer; //Physical player reference
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
        private PlayerWeaponManager weaponManager;
        private PlayerShieldManager shieldManager;

        private UIManager uiManager;

        private CameraControl cameraControl;

        [HideInInspector] public Transform lockedTarget;

        #region Flags
        [Header("General flags")]
        public bool isClient = true;
        public bool playerIsStuckInAnimation = false; //Disables actions like dodging, attacking... and movement
        public bool disableActions = false; //If the player can perform actions like dodging, attacking, blocking... (Indipendent from movement)
        public bool consumeInputs = true;
        public bool isOnGround = true;
        public bool isKnockedOut = false;
        public bool isDead = false;

        [Header("Locomotion flags")]
        public bool canRotate = true;
        public bool shouldSlide = false; //If the friction should be enabled or not
        public bool isRolling = false;
        public bool isBackdashing = false;
        public bool isSprinting = false;

        [Header("Combat flag")]
        public bool isAttacking = false;
        public bool isBlocking = false;
        public bool canBlock = true;
        public bool isLockingOn = false;
        public bool canLockOn = true;
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
            weaponManager = GetComponent<PlayerWeaponManager>();
            shieldManager = GetComponent<PlayerShieldManager>();

            uiManager = FindObjectOfType<UIManager>();

            animatedPlayer = transform.Find("AnimatedPlayer").gameObject;
            physicalPlayer = transform.Find("PhysicalPlayer").gameObject;
            physicalHips = transform.Find("PhysicalPlayer/Armature/Hip").GetComponent<Rigidbody>();
            animatedHips = transform.Find("AnimatedPlayer/Armature/Hip");

            cameraTransform = Camera.main.transform;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                inputManager.enabled = false;
                animationManager.GetAnimator().applyRootMotion = false;
                isClient = false;
                Destroy(inputManager);
                //Destroy(animationManager);
                Destroy(collisionManager);
                Destroy(locomotionManager);
                Destroy(inventoryManager);
                Destroy(statsManager);
                Destroy(combatManager);
                Destroy(weaponManager);
                Destroy(shieldManager);
            }
            else
            {
                isClient = true;
                transform.position = new Vector3(58, 20, 0);

                cameraControl = cameraTransform.GetComponentInParent<CameraControl>();

                cameraControl.SetCameraPlayerManager(this);
                cameraControl.SetCameraInputManager(inputManager);

                cameraControl.SetCameraPhysicalHips(physicalHips);
                cameraControl.SetCameraFollowTransform(physicalHips.transform);

                uiManager.SetPlayerStatsManager(statsManager);
            }
        }

        private void FixedUpdate()
        {
            if (isDead) return;

            //Move player with animation (Order is important for some reason)
            locomotionManager.ApplyGravity();
            locomotionManager.MovePlayerWithAnimation();

        }

        private void Update()
        {
            if (isDead) return;

            //Ragdoll
            ragdollManager.HandleWakeUp();

            //Locomotion
            locomotionManager.HandleLocomotion();

            //Combat
            combatManager.HandleCombat();

            //QuickSlots
            inventoryManager.HandleQuickSlots();

            //Reset inputs
            inputManager.ResetAllInputValues();

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

        public PlayerWeaponManager GetWeaponManager()
        {
            return weaponManager;
        }

        public PlayerShieldManager GetShieldManager()
        {
            return shieldManager;
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

        public GameObject GetPhysicalPlayer()
        {
            return physicalPlayer;
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
