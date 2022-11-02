using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerManager : MonoBehaviour
    {
        public PlayerData playerData; //Player data reference
        public GameObject animatedPlayer; //Player data reference
        public Rigidbody physicalHips; //Player's physical hips
        public Transform groundCheckTransform; //Player's ground check's transform

        [HideInInspector] public InputManager inputManager;
        [HideInInspector] public Transform cameraTransform;
        [HideInInspector] public CameraManager cameraManager;
        [HideInInspector] public AnimationManager animationManager;
        [HideInInspector] public PlayerLocomotionManager playerLocomotionManager;
        [HideInInspector] public ActiveRagdollManager ragdollManager;
        [HideInInspector] public PlayerInventoryManager inventoryManager;
        [HideInInspector] public PlayerAttackManager attackManager;
        [HideInInspector] public PlayerStatsManager statsManager;
        [HideInInspector] public PlayerCollisionManager collisionManager;
        [HideInInspector] public UIManager uiManager; //UI manager

        [HideInInspector] public float currentSpeedMultiplier;
        [HideInInspector] public float currentRotationSpeedMultiplier;
        [HideInInspector] public Vector3 groundNormal;
        [HideInInspector] public float groundDistance;
        [HideInInspector] public Vector3 movementDirection;

        #region Flags
        [Header("Flags")]
        public bool isClient = true;
        public bool disablePlayerInteraction = false;
        public bool canRotate = true;
        public bool isOnGround = true;
        public bool shouldSlide = false; //If the friction should be enabled or not
        public bool isRolling = false;
        public bool isBackdashing = false;
        public bool isSprinting = false;
        public bool isAttacking = false;
        public bool isKnockedOut = false;
        #endregion

        private void Awake()
        {

            inputManager = GetComponent<InputManager>();
            animationManager = GetComponent<AnimationManager>();
            animationManager.Initialize();
            playerLocomotionManager = GetComponent<PlayerLocomotionManager>();
            ragdollManager = GetComponentInChildren<ActiveRagdollManager>();
            inventoryManager = GetComponentInChildren<PlayerInventoryManager>();
            attackManager = GetComponent<PlayerAttackManager>();
            statsManager = GetComponent<PlayerStatsManager>();
            collisionManager = GetComponent<PlayerCollisionManager>();

            uiManager = FindObjectOfType<UIManager>();

            cameraTransform = Camera.main.transform;
            cameraManager = Camera.main.GetComponentInParent<CameraManager>();

        }

        private void FixedUpdate()
        {
            //Move player with animation (Order is important for some reason)
            ApplyGravity();
            MovePlayerWithAnimation(currentSpeedMultiplier);

        }

        private void Update()
        {
            //Inputs
            inputManager.TickMovementInput();
            inputManager.TickCameraMovementInput();
            inputManager.TickDPadInput();
            inputManager.TickActionsInput();

            //Movement, animation logic
            playerLocomotionManager.HandleMovementRotation();
            playerLocomotionManager.HandleMovement();
            playerLocomotionManager.HandleFootFriction();
            playerLocomotionManager.CheckIfOnGround();
            playerLocomotionManager.HandleFallingAndLanding();
            playerLocomotionManager.HandleRollingAndSprinting();

            //Attacks
            attackManager.HandleAttacks();

            //QuickSlots
            inventoryManager.HandleQuickSlots();

        }

        private void LateUpdate()
        {
            cameraManager.HandleCamera(inputManager.cameraInput);

        }

        /// <summary>
        /// Move the player with the root motion of the animator and based on the current gravity force
        /// </summary>
        private void MovePlayerWithAnimation(float speedMultiplier)
        {
            if (isKnockedOut) return;
            //Debug.Log(animationManager.animator.velocity * speedMultiplier);
            physicalHips.velocity = Vector3.ProjectOnPlane(animationManager.animator.velocity * speedMultiplier, groundNormal);

        }

        private Vector3 additionalGravityForce;
        private void ApplyGravity()
        {

            //Base gravity
            ragdollManager.AddForceToPlayer(Vector3.down * playerData.baseGravityForce * ((isKnockedOut) ? 0 : 1), ForceMode.Acceleration);

            //Add additional gravity force if not too fast
            additionalGravityForce = playerLocomotionManager.GetGravity(isOnGround);
            if (Mathf.Abs(physicalHips.velocity.y) < playerData.maxFallingSpeed) ragdollManager.AddForceToPlayer(additionalGravityForce, ForceMode.Acceleration);

        }

    }
}
