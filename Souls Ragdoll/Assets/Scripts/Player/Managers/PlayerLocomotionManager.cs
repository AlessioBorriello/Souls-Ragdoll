using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

namespace AlessioBorriello
{
    public class PlayerLocomotionManager : MonoBehaviour
    {

        private PlayerManager playerManager;

        [SerializeField] private PhysicMaterial physicalFootMaterialIdle; //Player's physical foot material when idle
        [SerializeField] private PhysicMaterial physicalFootMaterialMoving; //Player's physical foot material when moving
        [SerializeField] private Collider[] feetColliders = new Collider[2];
        private PhysicMaterial currentFootMaterial;

        //Timers
        private float sprintTimer;
        private float rollTimer;
        private float inAirTimer;

        private void Start()
        {

            playerManager = GetComponent<PlayerManager>();
            SetFeetMaterial(false);

        }

        /// <summary>
        /// Move player with animation
        /// </summary>
        public void HandleMovement()
        {

            float moveAmount = GetClampedMovementAmount(playerManager.inputManager.movementInput.magnitude);
            playerManager.animationManager.UpdateMovementAnimatorValues(moveAmount, 0, .1f);

            if (playerManager.disablePlayerInteraction) return;

            playerManager.currentSpeedMultiplier = GetMovementSpeedMultiplier();
            HandleMovementFootFriction();

        }

        /// <summary>
        /// Sets the foot friction based on shouldSlide bool
        /// </summary>
        public void HandleFootFriction()
        {
            if(playerManager.shouldSlide && currentFootMaterial == physicalFootMaterialIdle)
            {
                SetFeetMaterial(false);
            }else if(!playerManager.shouldSlide && currentFootMaterial == physicalFootMaterialMoving)
            {
                SetFeetMaterial(true);
            }
        }

        /// <summary>
        /// Rotate player towards direction
        /// </summary>
        public void HandleMovementRotation()
        {
            if (!playerManager.canRotate) return;

            playerManager.currentRotationSpeedMultiplier = GetRotationSpeedMultiplier();
            playerManager.movementDirection = GetMovementDirection();

            Quaternion newRotation = Quaternion.LookRotation(playerManager.movementDirection);
            float rotationSpeed = playerManager.currentRotationSpeedMultiplier * Time.deltaTime;
            playerManager.animatedPlayer.transform.rotation = Quaternion.Slerp(playerManager.animatedPlayer.transform.rotation, newRotation, rotationSpeed);

            if(playerManager.playerData.tiltOnDirectionChange) HandleTilt();
        }

        private float currentDirectionAngle = 0;
        private Vector3 currentPos = Vector3.zero;
        private float tiltAmount = 0;
        /// <summary>
        /// Tilts the player in the direction it's moving when moving fast enough
        /// </summary>
        private void HandleTilt()
        {
            if (playerManager.disablePlayerInteraction) return;

            float speed = Vector3.ProjectOnPlane((playerManager.physicalHips.transform.position - currentPos), playerManager.groundNormal).magnitude;
            if (speed <= playerManager.playerData.speedNeededToTilt || !playerManager.isOnGround)
            {
                tiltAmount = 0;
                return;
            }

            float angleDifference = Mathf.DeltaAngle(playerManager.physicalHips.rotation.eulerAngles.y, currentDirectionAngle);

            tiltAmount = Mathf.Lerp(tiltAmount, angleDifference * speed * 10, Time.deltaTime * playerManager.playerData.tiltSpeed);
            tiltAmount = Mathf.Clamp(tiltAmount, -playerManager.playerData.maxTiltAmount, playerManager.playerData.maxTiltAmount);

            Quaternion tiltedRotation = Quaternion.AngleAxis(tiltAmount, playerManager.animatedPlayer.transform.forward) * playerManager.animatedPlayer.transform.rotation;
            playerManager.animatedPlayer.transform.rotation = tiltedRotation;

            currentDirectionAngle = playerManager.physicalHips.rotation.eulerAngles.y;
            currentPos = playerManager.physicalHips.transform.position;

        }

        /// <summary>
        /// Sets if the player should slide or not based on input magnitude
        /// </summary>
        private void HandleMovementFootFriction()
        {

            if (playerManager.inputManager.movementInput.magnitude == 0) playerManager.shouldSlide = false;
            else if (playerManager.inputManager.movementInput.magnitude > 0) playerManager.shouldSlide = true;

        }

        /// <summary>
        /// Manages rolls, backdashes and sprinting based on the player's east button press duration and movement velocity
        /// </summary>
        public void HandleRollingAndSprinting()
        {

            if (playerManager.disablePlayerInteraction)
            {
                //rollTimer = 0;
                sprintTimer = 0;
                playerManager.isSprinting = false;
                return;
            }


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
                if(playerManager.animationManager.animator.GetBool("onGround"))
                {
                    playerManager.animationManager.UpdateOnGroundValue(false);
                    playerManager.animationManager.PlayTargetAnimation("Fall", .2f);
                }
            }

            //Land
            if (playerManager.isOnGround && inAirTimer > 0)
            {
                if(!playerManager.animationManager.animator.GetBool("onGround"))
                {
                    playerManager.animationManager.UpdateOnGroundValue(true);
                    playerManager.animationManager.PlayTargetAnimation("Empty", .2f);
                }

                //Knock out
                if (!playerManager.isKnockedOut && inAirTimer > playerManager.playerData.knockoutLandThreshold) playerManager.ragdollManager.KnockOut();

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

            playerManager.animationManager.UpdateMovementAnimatorValues(2, 0, .1f);
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

            if (!playerManager.isOnGround) return playerManager.playerData.inAirRotationSpeed;

            if (playerManager.isAttacking) return playerManager.playerData.attackingRotationSpeed;

            return playerManager.playerData.rotationSpeed;

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
            if (Physics.SphereCast(pos, .1f, Vector3.down, out hit, Mathf.Infinity, playerManager.playerData.groundCollisionLayers))
            {

                if (hit.distance < playerManager.playerData.minDistanceToFall && Vector3.Angle(Vector3.up, hit.normal) < playerManager.playerData.maxSlopeAngle) playerManager.isOnGround = true;
                else (playerManager.isOnGround) = false;

                playerManager.groundNormal = hit.normal;
                playerManager.groundDistance = hit.distance;
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

            Vector3 gravityForce = Vector3.zero;
            //Increase in air timer
            if (!isOnGround)
            {
                //Apply more downwards force when not on the ground based on the in air timer
                gravityForce += Vector3.down * (playerManager.playerData.fallingSpeed * inAirTimer);
                inAirTimer += delta;
            }
            else
            {
                gravityForce = Vector3.zero;
                inAirTimer = 0;
            }

            return gravityForce;
        }

        /// <summary>
        /// Sets the physical material of the feet when either moving or not moving
        /// </summary>
        public void SetFeetMaterial(bool idleMat)
        {

            if (idleMat)
            {
                feetColliders[0].material = physicalFootMaterialIdle;
                feetColliders[1].material = physicalFootMaterialIdle;
                currentFootMaterial = physicalFootMaterialIdle;
            }else
            {
                feetColliders[0].material = physicalFootMaterialMoving;
                feetColliders[1].material = physicalFootMaterialMoving;
                currentFootMaterial = physicalFootMaterialMoving;
            }
        }

        /// <summary>
        /// Makes the player do a 180 animation if the direction changes suddenly (Not used)
        /// </summary>
        private void HandleSharpTurns()
        {

            if (!playerManager.isSprinting) return;

            float angle = Vector3.Angle(playerManager.movementDirection, Vector3.ProjectOnPlane(playerManager.physicalHips.velocity, Vector3.up).normalized);
            //if (angle > playerManager.playerData.minimumSharpTurnAngle)
            if (angle > 170)
            {
                //playerManager.animationManager.PlayTargetAnimation("180 Turn", .1f);
            }

        }

    }
}
