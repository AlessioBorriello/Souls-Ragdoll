using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField] public PlayerData playerData; //Player data reference
        [SerializeField] public GameObject AnimatedPlayer; //Player data reference
        [SerializeField] public Rigidbody physicalHips; //Player's physical hips
        [SerializeField] public PhysicMaterial physicalFootMaterial; //Player's physical foot material

        private InputManager inputManager;
        private Transform cameraTransform;
        private AnimationManager animationManager;

        private void Start()
        {

            inputManager = GetComponent<InputManager>();
            animationManager = GetComponentInChildren<AnimationManager>();
            animationManager.Initialize();

            cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            //Inputs
            inputManager.TickMovementInput();
            inputManager.TickCameraMovementInput();


            //Movement and animation
            HandleRotation(playerData.rotationSpeed); //Add if can rotate
            HandleMovement();

        }

        /// <summary>
        /// Get the direction of the movement based on the camera
        /// </summary>
        /// <returns>The direction</returns>
        private Vector3 GetMovementDirection()
        {
            Vector3 movementDirection = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up) * inputManager.movementInput.y; //Camera's current z axis * vertical movement (Up, down input)
            movementDirection += Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up) * inputManager.movementInput.x; //Camera's current z axis * horizontal movement (Right, Left input)
            movementDirection.Normalize();
            movementDirection.y = 0; //Remove y component from the vector (Y component of the vector from the camera should be ignored)
            //movementDirection = Vector3.ProjectOnPlane(movementDirection, (isOnGround) ? GroundNormal : Vector3.up);

            if (inputManager.movementInput.magnitude == 0) return AnimatedPlayer.transform.forward; //If not moving return the forward

            return movementDirection;
        }

        /// <summary>
        /// Rotate player towards direction
        /// </summary>
        public void HandleRotation(float rotationSpeed)
        {
            Vector3 movementDirection = GetMovementDirection();
            AnimatedPlayer.transform.rotation = Quaternion.Slerp(AnimatedPlayer.transform.rotation, Quaternion.LookRotation(movementDirection), rotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Move player with animation
        /// </summary>
        public void HandleMovement()
        {

            float speedMultiplier = GetMovementSpeedMultiplier();

            animationManager.UpdateMovementAnimatorValues(inputManager.movementInput.magnitude, 0);
            MovePlayerWithAnimation(speedMultiplier);

            if(inputManager.movementInput.magnitude == 0)
            {
                physicalFootMaterial.frictionCombine = PhysicMaterialCombine.Maximum;
            }else
            {
                physicalFootMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
            }
        }

        /// <summary>
        /// Calculate the player's speed multiplier based on how much the movement input is being pressed
        /// </summary>
        private float GetMovementSpeedMultiplier()
        {

            float speedMultiplier = playerData.walkSpeedMultiplier;
            if (Mathf.Abs(inputManager.movementInput.magnitude) > .55f)
            {
                speedMultiplier = playerData.runSpeedMultiplier;
            }

            return speedMultiplier;

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
