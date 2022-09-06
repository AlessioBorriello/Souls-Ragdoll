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

        public float currentSpeedMultiplier;
        public Vector3 currentGravityForce;
        public Vector3 groundNormal;
        public Vector3 movementDirection;

        //Flags
        public bool disablePlayerInteraction = false;
        public bool canRotate = true;
        public bool isOnGround = true;
        public bool isRolling = false;
        public bool isBackdashing = false;
        public bool isSprinting = false;
        public bool isKnockedOut = false;

        private void Start()
        {

            inputManager = GetComponent<InputManager>();
            animationManager = GetComponentInChildren<AnimationManager>();
            animationManager.Initialize();
            playerLocomotionManager = GetComponent<PlayerLocomotionManager>();
            ragdollManager = GetComponentInChildren<ActiveRagdollManager>();

            cameraTransform = Camera.main.transform;
            cameraManager = Camera.main.GetComponentInParent<CameraManager>();
        }

        private void FixedUpdate()
        {
            //Move player with animation
            MovePlayerWithAnimation(currentSpeedMultiplier);
            ApplyGravity();
        }

        private void Update()
        {
            //Inputs
            inputManager.TickMovementInput();
            inputManager.TickCameraMovementInput();
            inputManager.TickActionsInput();


            //Movement and animation logic
            if(!disablePlayerInteraction && !isKnockedOut)
            {
                if (canRotate) playerLocomotionManager.HandleMovementRotation(playerData.rotationSpeed);
                playerLocomotionManager.HandleMovement();
                playerLocomotionManager.HandleRollingAndSprinting();
            }

            playerLocomotionManager.CheckIfOnGround();
            playerLocomotionManager.HandleFallingAndLanding();

            //Reset flags
            ResetFlags();

            disablePlayerInteraction = animationManager.animator.GetBool("DisablePlayerInteraction");

        }

        private void LateUpdate()
        {
            cameraManager.HandleCamera(inputManager.cameraInput);
        }

        private void ResetFlags()
        {
            isRolling = false;
            isBackdashing = false;
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

            currentGravityForce = playerLocomotionManager.GetGravity(isOnGround);
            //Clamp
            if (Mathf.Abs(currentGravityForce.y) > Mathf.Abs(playerData.maxFallingSpeed)) currentGravityForce = new Vector3(currentGravityForce.x, -playerData.maxFallingSpeed, currentGravityForce.z);
            
            physicalHips.velocity += (currentGravityForce);

        }

    }
}
