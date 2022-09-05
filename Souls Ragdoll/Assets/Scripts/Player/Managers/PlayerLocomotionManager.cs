using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerLocomotionManager : MonoBehaviour
    {

        private PlayerManager playerManager;
        private float sprintTimer;
        private float rollTimer;

        private void Start()
        {

            playerManager = GetComponent<PlayerManager>();

        }

        /// <summary>
        /// Move player with animation
        /// </summary>
        public void HandleMovement()
        {

            playerManager.currentSpeedMultiplier = GetMovementSpeedMultiplier();
            float moveAmount = GetClampedMovementAmount(playerManager.inputManager.movementInput.magnitude);
            playerManager.animationManager.UpdateMovementAnimatorValues(moveAmount, 0);
            HandleFootFriction();

        }

        /// <summary>
        /// Rotate player towards direction
        /// </summary>
        public void HandleRotation(float rotationSpeed)
        {
            Vector3 movementDirection = GetMovementDirection();
            playerManager.animatedPlayer.transform.rotation = Quaternion.Slerp(playerManager.animatedPlayer.transform.rotation, Quaternion.LookRotation(movementDirection), rotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Changes foot material's friction based on if the player is moving or not
        /// </summary>
        private void HandleFootFriction()
        {

            if (playerManager.inputManager.movementInput.magnitude == 0 && playerManager.physicalFootMaterial.frictionCombine == PhysicMaterialCombine.Minimum)
            {
                //Debug.Log("Switching to max friction");
                playerManager.physicalFootMaterial.frictionCombine = PhysicMaterialCombine.Maximum;
            }
            else if (playerManager.inputManager.movementInput.magnitude > 0 && playerManager.physicalFootMaterial.frictionCombine == PhysicMaterialCombine.Maximum)
            {
                //Debug.Log("Switching to min friction");
                playerManager.physicalFootMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
            }

        }

        /// <summary>
        /// Manages rolls, backdashes and sprinting based on the player's east button press duration and movement velocity
        /// </summary>
        public void HandleRollingAndSprinting()
        {

            if (playerManager.disablePlayerInteraction) return;


            if (playerManager.inputManager.eastInput)
            {
                float delta = Time.deltaTime;
                rollTimer += delta;
                if (playerManager.inputManager.movementInput.magnitude > 0) sprintTimer += delta;
                else sprintTimer = 0;
            }

            if (playerManager.inputManager.eastInput && sprintTimer > playerManager.playerData.sprintThreshold) playerManager.isSprinting = true;

            if (playerManager.inputManager.eastInputReleased && rollTimer < playerManager.playerData.sprintThreshold)
            {
                if (playerManager.inputManager.movementInput.magnitude > 0) playerManager.isRolling = true;
                else playerManager.isBackdashing = true;
            }

            if (playerManager.isRolling) Roll();
            else if (playerManager.isBackdashing) Backdash();
            else if (playerManager.isSprinting) Sprint();

            if (playerManager.inputManager.eastInputReleased)
            {
                rollTimer = 0;
                sprintTimer = 0;
            }

        }

        /// <summary>
        /// Makes player roll
        /// </summary>
        private void Roll()
        {
            playerManager.currentSpeedMultiplier = playerManager.playerData.rollSpeedMultiplier;
            playerManager.animationManager.PlayTargetAnimation("Roll", true, .2f);
        }

        /// <summary>
        /// Makes player backdash
        /// </summary>
        private void Backdash()
        {
            playerManager.currentSpeedMultiplier = playerManager.playerData.backdashSpeedMultiplier;
            playerManager.animationManager.PlayTargetAnimation("Backdash", true, .2f);
        }

        /// <summary>
        /// Makes player sprint, if the player releases the east input button or stops, the player stops sprinting
        /// </summary>
        private void Sprint()
        {
            if (playerManager.inputManager.movementInput.magnitude == 0 || playerManager.inputManager.eastInputReleased)
            {
                playerManager.isSprinting = false;
                return;
            }

            playerManager.animationManager.UpdateMovementAnimatorValues(2, 0);
            playerManager.currentSpeedMultiplier = playerManager.playerData.sprintSpeedMultiplier;
        }

        /// <summary>
        /// Get the direction of the movement based on the camera
        /// </summary>
        /// <returns>The direction</returns>
        private Vector3 GetMovementDirection()
        {
            Vector3 movementDirection = Vector3.ProjectOnPlane(playerManager.cameraTransform.forward, Vector3.up) * playerManager.inputManager.movementInput.y; //Camera's current z axis * vertical movement (Up, down input)
            movementDirection += Vector3.ProjectOnPlane(playerManager.cameraTransform.right, Vector3.up) * playerManager.inputManager.movementInput.x; //Camera's current z axis * horizontal movement (Right, Left input)
            movementDirection.Normalize();
            movementDirection.y = 0; //Remove y component from the vector (Y component of the vector from the camera should be ignored)
                                     //movementDirection = Vector3.ProjectOnPlane(movementDirection, (isOnGround) ? GroundNormal : Vector3.up);

            if (playerManager.inputManager.movementInput.magnitude == 0) return playerManager.animatedPlayer.transform.forward; //If not moving return the forward

            return movementDirection;
        }

        /// <summary>
        /// Calculate the player's speed multiplier based on how much the movement input is being pressed
        /// </summary>
        private float GetMovementSpeedMultiplier()
        {

            float speedMultiplier = playerManager.playerData.walkSpeedMultiplier;
            if (Mathf.Abs(playerManager.inputManager.movementInput.magnitude) > .55f)
            {
                speedMultiplier = playerManager.playerData.runSpeedMultiplier;
            }

            return speedMultiplier;

        }

        /// <summary>
        /// Sets amount to hard values to snap animation speed
        /// </summary>
        private float GetClampedMovementAmount(float amount)
        {
            float clampedAmount = 0;
            if (Mathf.Abs(amount) > 0 && Mathf.Abs(amount) < .55f)
            {
                clampedAmount = .5f * Mathf.Sign(amount);
            }
            else if (Mathf.Abs(amount) > .55f)
            {
                clampedAmount = 1 * Mathf.Sign(amount);
            }

            return clampedAmount;
        }

    }
}
