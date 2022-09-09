using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerLocomotionManager : MonoBehaviour
    {

        private PlayerManager playerManager;

        //Timers
        private float sprintTimer;
        private float rollTimer;
        private float inAirTimer;

        private void Start()
        {

            playerManager = GetComponent<PlayerManager>();
            playerManager.physicalFootMaterial.staticFriction = playerManager.playerData.idleFriction;
            playerManager.physicalFootMaterial.dynamicFriction = playerManager.playerData.idleFriction;

        }

        /// <summary>
        /// Move player with animation
        /// </summary>
        public void HandleMovement()
        {

            playerManager.currentSpeedMultiplier = GetMovementSpeedMultiplier();
            playerManager.currentRotationSpeedMultiplier = GetRotationSpeedMultiplier();
            float moveAmount = GetClampedMovementAmount(playerManager.inputManager.movementInput.magnitude);
            playerManager.animationManager.UpdateMovementAnimatorValues(moveAmount, 0);
            HandleFootFriction();

        }

        /// <summary>
        /// Rotate player towards direction
        /// </summary>
        public void HandleMovementRotation(float rotationSpeed)
        {
            playerManager.movementDirection = GetMovementDirection();
            playerManager.animatedPlayer.transform.rotation = Quaternion.Slerp(playerManager.animatedPlayer.transform.rotation, Quaternion.LookRotation(playerManager.movementDirection), rotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Changes foot material's friction based on if the player is moving or not
        /// </summary>
        private void HandleFootFriction()
        {

            if (playerManager.inputManager.movementInput.magnitude == 0 && playerManager.physicalFootMaterial.staticFriction == playerManager.playerData.movingFriction)
            {
                playerManager.physicalFootMaterial.staticFriction = playerManager.playerData.idleFriction;
                playerManager.physicalFootMaterial.dynamicFriction = playerManager.playerData.idleFriction;
            }
            else if (playerManager.inputManager.movementInput.magnitude > 0 && playerManager.physicalFootMaterial.staticFriction == playerManager.playerData.idleFriction)
            {
                playerManager.physicalFootMaterial.staticFriction = playerManager.playerData.movingFriction;
                playerManager.physicalFootMaterial.dynamicFriction = playerManager.playerData.movingFriction;
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
                if (playerManager.inputManager.movementInput.magnitude > 0) Roll();
                else Backdash();
            }

            if (playerManager.isSprinting) Sprint();

            if (playerManager.inputManager.eastInputReleased)
            {
                rollTimer = 0;
                sprintTimer = 0;
            }

        }

        /// <summary>
        /// Manages falling and landing, start falling when in air for enough time and land when isOnGround is back to true
        /// </summary>
        public void HandleFallingAndLanding()
        {
            //Fall
            if (!playerManager.isOnGround && inAirTimer > playerManager.playerData.timeBeforeFalling)
            {
                playerManager.animationManager.PlayTargetAnimation("Fall", .2f);
            }

            //Land
            if (playerManager.isOnGround && inAirTimer > 0)
            {
                playerManager.animationManager.PlayTargetAnimation("Movement", .2f);
                if (!playerManager.isKnockedOut && inAirTimer > playerManager.playerData.knockoutLandThreshold)
                {
                    //Knock out
                    playerManager.ragdollManager.KnockOut();

                    //Makes the player bounce based on in air time (clamp at times 3)
                    Vector3 bounce = Vector3.up * playerManager.playerData.upwardLandingForce * ((inAirTimer < 3) ? inAirTimer : 3);
                    playerManager.ragdollManager.AddForceToPlayer(bounce, ForceMode.VelocityChange);
                    playerManager.movementDirection = Vector3.zero;
                }

                inAirTimer = 0;
            }
        }

        /// <summary>
        /// Makes player roll
        /// </summary>
        private void Roll()
        {
            playerManager.currentSpeedMultiplier = playerManager.playerData.rollSpeedMultiplier;
            playerManager.animationManager.PlayTargetAnimation("Roll", .2f);
        }

        /// <summary>
        /// Makes player backdash
        /// </summary>
        private void Backdash()
        {
            playerManager.currentSpeedMultiplier = playerManager.playerData.backdashSpeedMultiplier;
            playerManager.animationManager.PlayTargetAnimation("Backdash", .2f);
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
            movementDirection = Vector3.ProjectOnPlane(movementDirection, (playerManager.isOnGround) ? playerManager.groundNormal : Vector3.up); //Project for sloped ground

            if (playerManager.inputManager.movementInput.magnitude == 0) return Vector3.ProjectOnPlane(playerManager.animatedPlayer.transform.forward, Vector3.up); //If not moving return the forward

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
        /// Calculate the player's rotation speed multiplier based on various factors
        /// </summary>
        private float GetRotationSpeedMultiplier()
        {

            float rotationMultiplier = playerManager.playerData.rotationSpeed;
            if (!playerManager.isOnGround)
            {
                rotationMultiplier = playerManager.playerData.inAirRotationSpeed;
            }

            return rotationMultiplier;

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

        /// <summary>
        /// Check if the player is currently touching the ground,
        /// if so set the ground normal and the isOnGround flag
        /// </summary>
        public void CheckIfOnGround()
        {
            RaycastHit hit;
            Vector3 pos = playerManager.groundCheckTransform.position;

            //Cast a ray downwards from the player's hips position
            if (Physics.Raycast(pos, Vector3.down, out hit, Mathf.Infinity, playerManager.playerData.groundCollisionLayers))
            {

                if (hit.distance < playerManager.playerData.minDistanceToFall && Vector3.Angle(Vector3.up, hit.normal) < playerManager.playerData.maxSlopeAngle) playerManager.isOnGround = true;
                else (playerManager.isOnGround) = false;

                playerManager.groundNormal = hit.normal;
            }
            else
            {
                playerManager.isOnGround = false;
            }
        }

        /// <summary>
        /// Calculate the gravity force to apply to the player
        /// </summary>
        public Vector3 GetGravity(bool isOnGround)
        {
            float delta = Time.deltaTime;

            //Base gravity (only if not ko'd)
            Vector3 gravityForce = Vector3.down * playerManager.playerData.baseGravityForce * ((playerManager.isKnockedOut)? 0 : 1);

            Vector3 additionalGravityForce = Vector3.zero;
            //Increase in air timer
            if (!isOnGround)
            {
                //Apply more downwards force when not on the ground based on the in air timer
                additionalGravityForce += Vector3.down * (playerManager.playerData.fallingSpeed * inAirTimer);
                inAirTimer += delta;
            }
            else
            {
                additionalGravityForce = Vector3.zero;
                inAirTimer = 0;
            }

            gravityForce += additionalGravityForce;

            return gravityForce;
        }

    }
}
