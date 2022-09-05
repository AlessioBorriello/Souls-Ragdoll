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

        [HideInInspector] public InputManager inputManager;
        [HideInInspector] public Transform cameraTransform;
        [HideInInspector] public CameraManager cameraManager;
        [HideInInspector] public AnimationManager animationManager;
        [HideInInspector] public PlayerLocomotionManager playerLocomotionManager;

        public float currentSpeedMultiplier;

        //Flags
        public bool disablePlayerInteraction = false;
        public bool canRotate = true;
        public bool isRolling = false;
        public bool isBackdashing = false;
        public bool isSprinting = false;

        private void Start()
        {

            inputManager = GetComponent<InputManager>();
            animationManager = GetComponentInChildren<AnimationManager>();
            animationManager.Initialize();
            playerLocomotionManager = GetComponent<PlayerLocomotionManager>();

            cameraTransform = Camera.main.transform;
            cameraManager = Camera.main.GetComponentInParent<CameraManager>();
        }

        private void Update()
        {
            //Inputs
            inputManager.TickMovementInput();
            inputManager.TickCameraMovementInput();
            inputManager.TickActionsInput();


            //Movement and animation logic
            if(!disablePlayerInteraction)
            {
                if(canRotate) playerLocomotionManager.HandleRotation(playerData.rotationSpeed);
                playerLocomotionManager.HandleMovement();
                playerLocomotionManager.HandleRollingAndSprinting();
            }

            //Move player with animation
            MovePlayerWithAnimation(currentSpeedMultiplier);

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
        /// Move the player with the root motion of the animator
        /// </summary>
        public void MovePlayerWithAnimation(float speedMultiplier)
        {
            Vector3 GroundNormal = Vector3.up;
            physicalHips.velocity = Vector3.ProjectOnPlane(animationManager.animator.velocity * speedMultiplier, GroundNormal);
        }

    }
}
