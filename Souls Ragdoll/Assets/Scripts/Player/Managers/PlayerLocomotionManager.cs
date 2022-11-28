using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

namespace AlessioBorriello
{
    public class PlayerLocomotionManager : MonoBehaviour
    {

        private PlayerManager playerManager;
        private InputManager inputManager;
        private AnimationManager animationManager;
        private ActiveRagdollManager ragdollManager;
        private PlayerStatsManager statsManager;
        private Animator animator;

        [Header("Set up")]
        [SerializeField] private PhysicMaterial physicalFootMaterialIdle; //Player's physical foot material when idle
        [SerializeField] private PhysicMaterial physicalFootMaterialMoving; //Player's physical foot material when moving
        [SerializeField] private Collider[] feetColliders = new Collider[2];

        private GameObject animatedPlayer;
        private GameObject physicalPlayer;
        private PhysicMaterial currentFootMaterial;
        private Rigidbody physicalHips;
        private Transform cameraTransform;

        private float currentSpeedMultiplier;
        private float currentRotationSpeed;

        private Vector3 groundNormal;
        private Vector3 movementDirection;

        //Timers
        private float sprintTimer;
        private float rollTimer;
        private float inAirTimer;

        private void Awake()
        {

            playerManager = GetComponent<PlayerManager>();
            animatedPlayer = playerManager.GetAnimatedPlayer();
            physicalPlayer = playerManager.GetPhysicalPlayer();
            inputManager = playerManager.GetInputManager();
            statsManager = playerManager.GetStatsManager();
            animationManager = playerManager.GetAnimationManager();
            animator = animationManager.GetAnimator();
            ragdollManager = playerManager.GetRagdollManager();

            physicalHips = playerManager.GetPhysicalHips();
            cameraTransform = playerManager.GetCameraTransform();

            movementDirection = transform.forward;

        }

        private void Start()
        {
            if (playerManager.IsOwner) SetFeetMaterial(false);
        }

        /// <summary>
        /// Move the player with the root motion of the animator and based on the current gravity force
        /// </summary>
        public void MovePlayerWithAnimation()
        {
            if (playerManager.isKnockedOut) return;
            //Debug.Log(animationManager.animator.velocity * speedMultiplier);
            physicalHips.velocity = Vector3.ProjectOnPlane(animator.velocity * currentSpeedMultiplier, groundNormal);

        }

        private Vector3 additionalGravityForce;
        public void ApplyGravity()
        {

            //Base gravity
            ragdollManager.AddForceToPlayer(Vector3.down * playerManager.playerData.baseGravityForce * ((playerManager.isKnockedOut) ? 0 : 1), ForceMode.Acceleration);

            //Add additional gravity force if not too fast
            additionalGravityForce = GetGravity(playerManager.isOnGround);
            if (Mathf.Abs(physicalHips.velocity.y) < playerManager.playerData.maxFallingSpeed) ragdollManager.AddForceToPlayer(additionalGravityForce, ForceMode.Acceleration);

        }

        public void HandleLocomotion()
        {
            HandleMovementRotation();
            HandleMovementAnimations();
            HandleFootFriction();
            CheckIfOnGround();
            HandleFallingAndLanding();
            HandleRollingAndSprinting();
        }

        /// <summary>
        /// Move player with animation
        /// </summary>
        private void HandleMovementAnimations()
        {
            if (playerManager.playerIsStuckInAnimation)
            {
                animationManager.UpdateMovementAnimatorValues(0, 0, .1f); //Stop
                return;
            }

            //If player is not sprinting
            if (!playerManager.isSprinting)
            {
                //Not locked on animations
                if (!playerManager.isLockingOn)
                {
                    float moveAmount = GetClampedMovementAmount(inputManager.movementInput.magnitude);
                    animationManager.UpdateMovementAnimatorValues(moveAmount, 0, .1f);
                }
                //Locked on animations
                else
                {
                    Vector2 input = GetClampedLockedOnMovementAmount(inputManager.movementInput);
                    animationManager.UpdateMovementAnimatorValues(input.y, input.x, .06f);
                }
            }

            //Allow the player to exit an override animation early if the player is moving, is not stuck in the animation and is NOT in the empty animation already
            if (inputManager.movementInput.magnitude > 0 && !animator.GetBool("IsInEmptyOverride"))
            {
                animationManager.PlayTargetAnimation("EmptyOverride", .2f, false);
            }

            float multiplier = CalculateMovementSpeedMultiplier();
            SetMovementSpeedMultiplier(multiplier);
            HandleMovementFootFriction();

        }

        /// <summary>
        /// Sets the foot friction based on shouldSlide bool
        /// </summary>
        private void HandleFootFriction()
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
        private void HandleMovementRotation()
        {
            if (!playerManager.canRotate) return;

            currentRotationSpeed = GetRotationSpeed();

            if(!playerManager.isLockingOn || playerManager.isSprinting || playerManager.isRolling) movementDirection = GetMovementDirection();
            else movementDirection = GetLockedOnMovementDirection();

            Quaternion newRotation = Quaternion.LookRotation(movementDirection);
            float rotationSpeed = currentRotationSpeed * Time.deltaTime;

            animatedPlayer.transform.rotation = Quaternion.Slerp(animatedPlayer.transform.rotation, newRotation, rotationSpeed);
            //animatedPlayer.transform.rotation = Quaternion.RotateTowards(animatedPlayer.transform.rotation, newRotation, rotationSpeed);

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
            if (playerManager.playerIsStuckInAnimation) return;

            float speed = Vector3.ProjectOnPlane((physicalHips.transform.position - currentPos), groundNormal).magnitude;
            if (speed <= playerManager.playerData.speedNeededToTilt || !playerManager.isOnGround)
            {
                tiltAmount = 0;
                return;
            }

            float angleDifference = Mathf.DeltaAngle(physicalHips.rotation.eulerAngles.y, currentDirectionAngle);

            tiltAmount = Mathf.Lerp(tiltAmount, angleDifference * speed * 10, Time.deltaTime * playerManager.playerData.tiltSpeed);
            tiltAmount = Mathf.Clamp(tiltAmount, -playerManager.playerData.maxTiltAmount, playerManager.playerData.maxTiltAmount);

            Quaternion tiltedRotation = Quaternion.AngleAxis(tiltAmount, animatedPlayer.transform.forward) * animatedPlayer.transform.rotation;
            animatedPlayer.transform.rotation = tiltedRotation;

            currentDirectionAngle = physicalHips.rotation.eulerAngles.y;
            currentPos = physicalHips.transform.position;

        }

        /// <summary>
        /// Sets if the player should slide or not based on input magnitude
        /// </summary>
        private void HandleMovementFootFriction()
        {

            if (inputManager.movementInput.magnitude == 0) playerManager.shouldSlide = false;
            else if (inputManager.movementInput.magnitude > 0) playerManager.shouldSlide = true;

        }

        /// <summary>
        /// Manages rolls, backdashes and sprinting based on the player's east button press duration and movement velocity
        /// </summary>
        private void HandleRollingAndSprinting()
        {

            if (playerManager.playerIsStuckInAnimation)
            {
                sprintTimer = 0;
                playerManager.isSprinting = false;
                return;
            }

            if(inputManager.eastInputPressed)
            {
                rollTimer = 0;
            }

            if (inputManager.eastInput)
            {
                float delta = Time.deltaTime;
                rollTimer += delta;
                if (inputManager.movementInput.magnitude > 0) sprintTimer += delta;
                else sprintTimer = 0;
            }

            if (inputManager.eastInput && sprintTimer > playerManager.playerData.sprintThreshold) playerManager.isSprinting = true;

            if (inputManager.eastInputReleased && rollTimer < playerManager.playerData.sprintThreshold)
            {
                if (inputManager.movementInput.magnitude > 0) Roll();
                else Backdash();
            }

            if (playerManager.isSprinting) Sprint();

            if (inputManager.eastInputReleased)
            {
                rollTimer = 0;
                sprintTimer = 0;
            }

        }

        /// <summary>
        /// Manages falling and landing, start falling when in air for enough time and land when isOnGround is back to true
        /// </summary>
        private void HandleFallingAndLanding()
        {
            //Fall
            if (!playerManager.isOnGround && inAirTimer > playerManager.playerData.timeBeforeFalling)
            {
                if(animator.GetBool("OnGround"))
                {
                    animationManager.UpdateOnGroundValue(false);
                    animationManager.PlayTargetAnimation("Fall", .2f, true);
                }
            }

            //Land
            if (playerManager.isOnGround && inAirTimer > 0)
            {
                if(!animator.GetBool("OnGround"))
                {
                    animationManager.UpdateOnGroundValue(true);
                    animationManager.PlayTargetAnimation("EmptyOverride", .2f, false);
                }

                //Knock out
                if (!playerManager.isKnockedOut && inAirTimer > playerManager.playerData.knockoutLandThreshold)
                {
                    ragdollManager.KnockOutServerRpc();
                    ragdollManager.KnockOut();
                }

                inAirTimer = 0;
            }
        }

        /// <summary>
        /// Makes player roll
        /// </summary>
        private void Roll()
        {
            if (playerManager.disableActions || statsManager.currentStamina < 1) return;

            currentSpeedMultiplier = playerManager.playerData.rollSpeedMultiplier;
            animationManager.PlayTargetAnimation("Roll", .15f, true);

            statsManager.ConsumeStamina(playerManager.playerData.rollBaseStaminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);
        }

        /// <summary>
        /// Makes player backdash
        /// </summary>
        private void Backdash()
        {
            if (playerManager.disableActions || statsManager.currentStamina < 1) return;

            currentSpeedMultiplier = playerManager.playerData.backdashSpeedMultiplier;
            animationManager.PlayTargetAnimation("Backdash", .2f, true);

            statsManager.ConsumeStamina(playerManager.playerData.backdashBaseStaminaCost, statsManager.playerStats.staminaDefaultRecoveryTime);
        }

        /// <summary>
        /// Makes player sprint, if the player releases the east input button or stops, the player stops sprinting
        /// </summary>
        private void Sprint()
        {
            if (playerManager.disableSprint || inputManager.movementInput.magnitude == 0 || inputManager.eastInputReleased || statsManager.currentStamina < 1)
            {
                playerManager.isSprinting = false;
                //Reenable sprint when stamina reaches a certain value
                if (playerManager.disableSprint && statsManager.currentStamina > playerManager.playerData.sprintStaminaNecessaryAfterStaminaDepleted) playerManager.disableSprint = false;
                return;
            }

            animationManager.UpdateMovementAnimatorValues(1.8f, 0, .1f);
            currentSpeedMultiplier = playerManager.playerData.sprintSpeedMultiplier;

            statsManager.ConsumeStamina(playerManager.playerData.sprintBaseStaminaCost, .05f);

            //Out of stamina
            if(statsManager.currentStamina < 1)
            {
                playerManager.isSprinting = false;

                //Set stamina to 0
                statsManager.ConsumeStamina(statsManager.maxStamina, statsManager.playerStats.staminaDefaultRecoveryTime);

            }
        }

        /// <summary>
        /// Get the direction of the movement based on the camera
        /// </summary>
        /// <returns>The direction</returns>
        private Vector3 GetMovementDirection()
        {
            Vector3 normal = (playerManager.isOnGround) ? groundNormal : Vector3.up;
            Vector3 movementDirection = Vector3.ProjectOnPlane(cameraTransform.forward, normal) * inputManager.movementInput.y; //Camera's current z axis * vertical movement (Up, down input)
            movementDirection += Vector3.ProjectOnPlane(cameraTransform.right, normal) * inputManager.movementInput.x; //Camera's current z axis * horizontal movement (Right, Left input)
            movementDirection.Normalize();
            
            if (inputManager.movementInput.magnitude == 0) return Vector3.ProjectOnPlane(animatedPlayer.transform.forward, Vector3.up); //If not moving return the forward
            
            return movementDirection;
        }

        /// <summary>
        /// Get the direction of the movement based on locked enemy direction
        /// </summary>
        /// <returns>The direction</returns>
        private Vector3 GetLockedOnMovementDirection()
        {
            if (playerManager.lockedTarget == null) return physicalHips.transform.forward;

            Vector3 normal = (playerManager.isOnGround) ? groundNormal : Vector3.up;
            Vector3 movementDirection = Vector3.ProjectOnPlane(playerManager.lockedTarget.position - physicalHips.position, normal);
            movementDirection.Normalize();

            return movementDirection;
        }

        /// <summary>
        /// Calculate the player's speed multiplier based on how much the movement input is being pressed
        /// </summary>
        private float CalculateMovementSpeedMultiplier()
        {
            if (playerManager.isRolling) return playerManager.playerData.rollSpeedMultiplier;
            if (playerManager.isBackdashing) return playerManager.playerData.backdashSpeedMultiplier;
            if (Mathf.Abs(inputManager.movementInput.magnitude) > .55f) return playerManager.playerData.runSpeedMultiplier;

            return playerManager.playerData.walkSpeedMultiplier;

        }

        /// <summary>
        /// Calculate the player's rotation speed multiplier based on various factors
        /// </summary>
        private float GetRotationSpeed()
        {
            if (playerManager.isLockingOn && playerManager.isRolling) return playerManager.playerData.lockedOnRollRotationSpeed;
            else if(!playerManager.isLockingOn && playerManager.isRolling) return playerManager.playerData.rollRotationSpeed;

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
        /// Sets amount to hard values to snap animation speed while locked on
        /// </summary>
        private Vector2 GetClampedLockedOnMovementAmount(Vector2 input)
        {
            Vector2 clampedInput = Vector2.zero;
            float magnitude = input.magnitude;
            float multiplier = 0;

            if (Mathf.Abs(magnitude) > 0 && Mathf.Abs(magnitude) < .55f) //Walking
            {
                multiplier = .5f;
            }
            else if (Mathf.Abs(magnitude) > .55f) //Running
            {
                multiplier = 1f;
            }

            if (Mathf.Abs(input.x) > .8f * magnitude && Mathf.Abs(input.y) < .25f * magnitude) //Right Left
            {
                clampedInput.x = 1 * Mathf.Sign(input.x) * multiplier;
                clampedInput.y = (input.y * Mathf.Sign(input.y));
            }
            else if (Mathf.Abs(input.y) > .8f * magnitude && Mathf.Abs(input.x) < .25f * magnitude) //Up Down
            {
                clampedInput.x = 0 + (input.x * Mathf.Sign(input.x));
                clampedInput.y = 1 * Mathf.Sign(input.y) * multiplier;
            }
            else if (Mathf.Abs(input.x) > .25f * magnitude && Mathf.Abs(input.y) > .25f * magnitude) //Up-Right Down-Left
            {
                clampedInput.x = 1 * Mathf.Sign(input.x) * multiplier;
                clampedInput.y = 1 * Mathf.Sign(input.y) * multiplier;
            }

            return clampedInput;
        }

        /// <summary>
        /// Check if the player is currently touching the ground,
        /// if so set the ground normal and the isOnGround flag
        /// </summary>
        private void CheckIfOnGround()
        {
            RaycastHit hit;
            Vector3 pos = playerManager.groundCheckTransform.position;

            //Cast a ray downwards from the player's hips position
            if (Physics.SphereCast(pos, .1f, Vector3.down, out hit, Mathf.Infinity, playerManager.playerData.groundCollisionLayers))
            {

                if (hit.distance < playerManager.playerData.minDistanceToFall && Vector3.Angle(Vector3.up, hit.normal) < playerManager.playerData.maxSlopeAngle) playerManager.isOnGround = true;
                else (playerManager.isOnGround) = false;

                groundNormal = hit.normal;
                //groundDistance = hit.distance;
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
            if (!isOnGround && physicalHips.velocity.y < 0)
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
        /// Sets the current movement speed multiplier that gets applied to the animator root movement
        /// </summary>
        /// <param name="multiplier">The multiplier</param>
        public void SetMovementSpeedMultiplier(float multiplier)
        {
            currentSpeedMultiplier = multiplier;
        }

        public Vector3 GetGroundNormal()
        {
            return groundNormal;
        }

        public IEnumerator StopMovementForTime(float time)
        {
            while (time > 0)
            {
                yield return null;
                time -= Time.deltaTime;
                animationManager.UpdateMovementAnimatorValues(0, 0, 0);
                StopMovementForTime(time);
            }
        }

        public IEnumerator StopActionsForTime(float time)
        {
            playerManager.disableActions = true;
            yield return new WaitForSeconds(time);
            playerManager.disableActions = false;
        }

        public IEnumerator DisablePlayerControlForTime(float time)
        {
            playerManager.playerIsStuckInAnimation = true;
            yield return new WaitForSeconds(time);
            playerManager.playerIsStuckInAnimation = false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendPositionAndRotationServerRpc(Vector3 position, Quaternion rotation, ulong id)
        {
            SendPositionAndRotationClientRpc(position, rotation, id);
        }

        [ClientRpc]
        private void SendPositionAndRotationClientRpc(Vector3 position, Quaternion rotation, ulong id)
        {
            if (playerManager.OwnerClientId != id) return;
            SetPositionAndRotation(position, rotation);
        }

        private void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            physicalHips.transform.position = position;
            animatedPlayer.transform.rotation = rotation;
        }

        /// <summary>
        /// Makes the player do a 180 animation if the direction changes suddenly (Not used)
        /// </summary>
        private void HandleSharpTurns()
        {

            if (!playerManager.isSprinting) return;

            float angle = Vector3.Angle(movementDirection, Vector3.ProjectOnPlane(physicalHips.velocity, Vector3.up).normalized);
            //if (angle > playerManager.playerData.minimumSharpTurnAngle)
            if (angle > 170)
            {
                //playerManager.animationManager.PlayTargetAnimation("180 Turn", .1f);
            }

        }

    }
}
