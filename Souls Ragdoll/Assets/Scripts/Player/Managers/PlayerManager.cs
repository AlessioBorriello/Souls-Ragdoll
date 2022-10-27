using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField] public PlayerData playerData; //Player data reference
        [SerializeField] public GameObject animatedPlayer; //Player data reference
        [SerializeField] public Rigidbody physicalHips; //Player's physical hips
        [SerializeField] public PhysicMaterial physicalFootMaterial; //Player's physical foot material
        [SerializeField] public Transform groundCheckTransform; //Player's ground check's transform

        [HideInInspector] public InputManager inputManager;
        [HideInInspector] public Transform cameraTransform;
        [HideInInspector] public CameraManager cameraManager;
        [HideInInspector] public AnimationManager animationManager;
        [HideInInspector] public PlayerLocomotionManager playerLocomotionManager;
        [HideInInspector] public ActiveRagdollManager ragdollManager;
        [HideInInspector] public PlayerInventoryManager inventoryManager;
        [HideInInspector] public PlayerAttackManager attackManager;

        public float currentSpeedMultiplier;
        public float currentRotationSpeedMultiplier;
        public Vector3 additionalGravityForce;
        public Vector3 groundNormal;
        public float groundDistance;
        public Vector3 movementDirection;

        //Flags
        [Header("Flags")]
        public bool disablePlayerInteraction = false;
        public bool canRotate = true;
        public bool isOnGround = true;
        public bool isRolling = false;
        public bool isBackdashing = false;
        public bool isSprinting = false;
        public bool isAttacking = false;
        public bool isKnockedOut = false;

        private void Start()
        {

            inputManager = GetComponent<InputManager>();
            animationManager = GetComponentInChildren<AnimationManager>();
            animationManager.Initialize();
            playerLocomotionManager = GetComponent<PlayerLocomotionManager>();
            ragdollManager = GetComponentInChildren<ActiveRagdollManager>();
            inventoryManager = GetComponentInChildren<PlayerInventoryManager>();
            attackManager = GetComponent<PlayerAttackManager>();

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
            inputManager.TickActionsInput();

            //Movement, animation logic
            if (!disablePlayerInteraction)
            {
                if (canRotate) playerLocomotionManager.HandleMovementRotation(currentRotationSpeedMultiplier);
                playerLocomotionManager.HandleMovement();
                playerLocomotionManager.HandleRollingAndSprinting();

                attackManager.HandleAttacks();
            }

            playerLocomotionManager.CheckIfOnGround();
            playerLocomotionManager.HandleFallingAndLanding();

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
            physicalHips.velocity = Vector3.ProjectOnPlane(animationManager.animator.velocity * speedMultiplier, groundNormal);

        }

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
