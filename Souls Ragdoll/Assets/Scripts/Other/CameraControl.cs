using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Windows;
using static UnityEngine.GraphicsBuffer;

namespace AlessioBorriello
{
    public class CameraControl : MonoBehaviour
    {
        [SerializeField] private PlayerManager playerManager;
        private InputManager inputManager;
        private Rigidbody physicalHips;

        [Header("Camera transforms")]
        [SerializeField] private Transform cameraFollowTarget; //Transform of the target
        [SerializeField] Transform cameraPitchTransform; //Transform of the pitch camera (Used to look up and down)
        private Transform cameraTransform; //Transform of the actual camera

        [Header("Camera options")]
        [SerializeField] private float cameraHeight = 1.8f;
        [SerializeField] private float defaultCameraDistance = 6f; //Camera distance when not obstructed
        [SerializeField] private float cameraFollowSpeed = 20f; //How fast the camera will reach the target
        [SerializeField] private float cameraPitchSpeed = 5f; //How fast the camera goes up and down
        [SerializeField] private float cameraPivotSpeed = 5f; //How fast the camera goes left and right
        [SerializeField] private float cameraCollisionOffset = .2f; //How much the camera will push away from a collision
        [SerializeField] private LayerMask collisionLayers; //What the camera will collide with
        [SerializeField] private bool followTarget = true; //If the camera should follow the player

        [Header("Lock on options")]
        [SerializeField] private float lockedOnCameraHeight = 1.8f;
        [SerializeField] private float lockedOnDefaultCameraDistance = 8f; //Camera distance when not obstructed and locked on
        [SerializeField] private float cameraLockOnPitchSpeed = 5f; //How fast the camera goes up and down when locked on
        [SerializeField] private float cameraLockOnPivotSpeed = 5f; //How fast the camera goes left and right when locked on
        [HideInInspector] public Transform lockedTarget; //The target locked on to

        //Camera angles
        private float cameraPitchAngle; //Up and down angle
        private float cameraPivotAngle; //Left and right angle

        //Angle limits
        private float minPitchAngle = -45;
        private float maxPitchAngle = 52;

        private void Awake()
        {
            cameraTransform = Camera.main.transform;
        }

        private void Start()
        {
            if (playerManager == null) return;

            inputManager = playerManager.GetInputManager();
            physicalHips = playerManager.GetPhysicalHips();
        }

        private void Update()
        {
            if (playerManager == null || cameraFollowTarget == null) return;
            if (!playerManager.isClient || playerManager.isDead || playerManager.isKnockedOut) return;

            //Handle lock on inputs
            HandleLockOnControls(inputManager.cameraInput, inputManager.rightStickInputPressed);
        }

        private void LateUpdate()
        {
            if (playerManager == null || cameraFollowTarget == null) return;
            if (!playerManager.isClient) return;

            //Move the camera
            HandleCameraMovement(inputManager.cameraInput);
        }

        /// <summary>
        /// Handle camera movement and rotation
        /// </summary>
        public void HandleCameraMovement(Vector2 cameraInput)
        {
            //Move camera to it's height
            float height = (playerManager.isLockingOn) ? lockedOnCameraHeight : cameraHeight;
            cameraPitchTransform.localPosition = new Vector3(0, Mathf.Lerp(cameraPitchTransform.localPosition.y, height, 6 * Time.deltaTime), 0);

            if (followTarget)
            {
                //Follow the camera target
                FollowCameraTarget();

                //Handle collision with environment
                HandleCameraCollisions();
            }

            //Rotate camera based on inputs
            RotateCamera(cameraInput);
        }

        /// <summary>
        /// Check for camera lock on controls, like locking on, changing target and losing target
        /// </summary>
        public void HandleLockOnControls(Vector2 cameraInput, bool stickPressed)
        {
            //Find target
            if (playerManager.canLockOn && stickPressed)
            {
                LockOnPressed();
            }

            //Lose and change target
            if(lockedTarget != null && playerManager.isLockingOn) //If there is a target already
            {
                HandleLosingLockOnTarget(); //Check if the target is lost
                HandleChangingTarget(cameraInput); //Check if the player changes target
            }
        }

        /// <summary>
        /// Defines conditions where the lock on on a target is lost
        /// </summary>
        private void HandleLosingLockOnTarget()
        {
            float distanceFromTarget = Vector3.Distance(physicalHips.position, lockedTarget.position);
            Vector3 directionToTarget = (lockedTarget.position - physicalHips.position).normalized;

            bool isObstructed = (Physics.Raycast(physicalHips.position, directionToTarget, Vector3.Distance(physicalHips.position, lockedTarget.position), playerManager.playerData.obstructionMask));
            if (distanceFromTarget > playerManager.playerData.maxLoseTargetDistance || isObstructed)
            {
                lockedTarget = null;
                playerManager.lockedTarget = null;
                playerManager.isLockingOn = false;
            }
        }

        private bool canChangeTarget = true;
        /// <summary>
        /// Tries to change target when the camera input is flicked in a certain direction
        /// </summary>
        private void HandleChangingTarget(Vector2 cameraInput)
        {
            //If the camera input is > .9f and the player has not already changed a character with that input
            if (cameraInput.magnitude > .9f && canChangeTarget)
            {
                //Cannot change again until the input gets reset to 0
                canChangeTarget = false;

                //Define the direction the player wants to change
                bool changeLeft = (cameraInput.x > 0) ? false : true;

                //Get new target
                Transform newTarget = GetLockOnTarget(changeLeft);

                //If it was found, set it as the locked target
                if (newTarget != null && newTarget != lockedTarget)
                {
                    lockedTarget = newTarget;
                    playerManager.lockedTarget = newTarget;
                }

            }

            //If changed already and the input is reset to neutral
            if(cameraInput.magnitude == 0 && !canChangeTarget)
            {
                //Allow another change
                canChangeTarget = true;
            }

        }

        /// <summary>
        /// Tries to lock on a target if the target is null or sets it as null if it was not
        /// </summary>
        private void LockOnPressed()
        {
            //If not locked on
            if (lockedTarget == null)
            {
                //Try to get a target
                lockedTarget = GetLockOnTarget();
                playerManager.lockedTarget = lockedTarget;

                //If target is found
                if (lockedTarget != null) playerManager.isLockingOn = true;
                else //Otherwise just point camera forward
                {
                    CenterCamera();
                }
            }
            else //If a target was already locked on to
            {
                //Remove lock on reference
                lockedTarget = null;
                playerManager.lockedTarget = null;
                playerManager.isLockingOn = false;
            }
        }

        /// <summary>
        /// Centers camera to the player's facing direction
        /// </summary>
        private void CenterCamera()
        {
            //Get player and camera forward
            Vector3 playerForward = Vector3.ProjectOnPlane(physicalHips.transform.forward, Vector3.up);
            Vector3 cameraForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

            //Get angle between camera and player
            float angle = Vector3.Angle(cameraForward, playerForward);
            //Check if the camera should move right or left
            angle = (Vector3.Cross(cameraForward, playerForward).y < 0) ? angle : -angle;

            //Move camera pivot
            cameraPivotAngle -= angle;
        }

        /// <summary>
        /// Tries to get a target to lock on to
        /// </summary>
        /// <param name="changingLeft">Only if trying to switch target, defines in which direction the player wants the new target</param>
        /// <returns>The target (if found)</returns>
        private Transform GetLockOnTarget(bool changingLeft = false)
        {

            //Check for colliders in a radius
            Collider[] collidersInsideRadius = Physics.OverlapSphere(physicalHips.position, playerManager.playerData.maxLockOnDistance, playerManager.playerData.targetMask);

            //No colliders found
            if (collidersInsideRadius.Length == 0) return null;

            List<Transform> possibleTargets = new List<Transform>();
            List<int> foundIds = new List<int>();

            foreach (Collider c in collidersInsideRadius)
            {
                //If already managed or if it's the caller's own colliders we are checking
                if (foundIds.Contains(c.transform.root.GetInstanceID()) || c.transform.root == playerManager.transform.root) continue;
                foundIds.Add(c.transform.root.GetInstanceID());

                //Get target from that collider, if it does not exist, it cannot be locked on to
                Transform newPossibleTarget = GetTargetTransformFromCollider(c);
                if (newPossibleTarget == null) continue;

                //If already locked on, we are changing target
                if (lockedTarget != null)
                {
                    //Check if we can change target to this new possible target
                    if (!CanChangeLockToTarget(newPossibleTarget, changingLeft)) continue;
                }

                //Add only those who are in front and are not obstructed
                if (IsLockable(newPossibleTarget, physicalHips.transform)) possibleTargets.Add(newPossibleTarget);

            }

            //Get the best target based on distance and angle difference from the camera
            return GetBestTarget(physicalHips.transform.position, possibleTargets);

        }

        /// <summary>
        /// Gets the target transform from a given collider (if found)
        /// </summary>
        /// <param name="coll">The collider to get the target transform from</param>
        /// <returns>The target transform (if found)</returns>
        private Transform GetTargetTransformFromCollider(Collider coll)
        {
            //Try to get the character manager from the collider
            CharacterManager possibleTarget = coll.transform.root.GetComponent<CharacterManager>();
            //If not found
            if (possibleTarget == null) return null;

            //Get target transform otherwise
            return possibleTarget.lockOnTargetTransform;
        }

        /// <summary>
        /// Tries to get the best possible target given the camera angle and distance
        /// </summary>
        /// <param name="hipsPosition">Position of the player</param>
        /// <param name="targets">List of the possible targets</param>
        /// <returns>The best target</returns>
        private Transform GetBestTarget(Vector3 hipsPosition, List<Transform> targets)
        {
            //If there are no targets
            if (targets.Count == 0) return null;

            //Default best target to the first one
            Transform bestTarget = targets[0];

            //If this target is the only one, simply return it
            if (targets.Count == 1) return bestTarget;

            //Get direction to the current best target
            Vector3 direction = (bestTarget.position - transform.position).normalized;
            //Set the minimum angle to the angle between the camera forward and the direction of the target
            float minAngle = Vector3.Angle(transform.forward, direction);

            //Set the minimum distance to the distance between the player's position and the target position
            float minDistance = Vector3.Distance(hipsPosition, bestTarget.position);

            foreach (Transform possibleNewBestTarget in targets)
            {
                //If the target is already the best
                if (possibleNewBestTarget == bestTarget) continue;

                //Get direction and angle for the new possible best target
                direction = (possibleNewBestTarget.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, direction);

                //Get distance from the new possible best target and how much closer it is (IF it is) in %
                float distance = Vector3.Distance(hipsPosition, possibleNewBestTarget.position);
                float percentageDistance = (minDistance - distance) / minDistance;

                //If the new possible target is significantly closer (35% closer) or if it's more in line with the camera (in the center)
                if (percentageDistance > .35f || angle < minAngle)
                {
                    //Set as new best target and update minimums
                    bestTarget = possibleNewBestTarget;
                    minAngle = angle;
                    minDistance = distance;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Checks if the given target can be locked on to
        /// </summary>
        /// <param name="targetTransform">Transform of the target to check</param>
        /// <param name="hipsTransform">Player's hips transform</param>
        /// <returns>If the target can be locked on to</returns>
        private bool IsLockable(Transform targetTransform, Transform hipsTransform)
        {
            //Get direction to the target
            Vector3 direction = (targetTransform.position - transform.position).normalized;

            //Check if the angle between the forward of the camera and the direction to the targets is small enough (if it's in front of the camera)
            bool isInFront = (Vector3.Angle(transform.forward, direction) < playerManager.playerData.maxLockOnAngle / 2);
            if (!isInFront) return false;

            //Check if the target is not obstructed
            bool isObstructed = (Physics.Raycast(hipsTransform.position, direction, Vector3.Distance(hipsTransform.position, targetTransform.position), playerManager.playerData.obstructionMask));
            if (isObstructed) return false;

            //If all the checks are passed
            return true;
        }

        /// <summary>
        /// Checks if the target trying to switch to is on the left or right and if that is equal
        /// to the direction the player wants to switch to then the target can be switched to and return true
        /// </summary>
        /// <param name="targetTransform">Transform of the target</param>
        /// <param name="changingLeft">What direction the player wants to switch to</param>
        /// <returns>If the target is in the direction the player wants to switch target to</returns>
        private bool CanChangeLockToTarget(Transform targetTransform, bool changingLeft)
        {
            //If it's the same target as the one already locked
            if (targetTransform == lockedTarget) return false;

            //Get direction to new potential target and locked on target
            Vector3 directionToLockedTarget = (lockedTarget.position - transform.position).normalized;
            Vector3 directionToPotentialTarget = (targetTransform.position - transform.position).normalized;

            //Use cross to define if the new possible target is on the left or the right of the camera
            bool isOnLeft = (Vector3.Cross(directionToPotentialTarget, directionToLockedTarget).y < 0) ? false : true;

            //Return if the target is in the same direction the player wants to switch to
            return (isOnLeft == changingLeft);
        }

        /// <summary>
        /// Move camera to the camera target
        /// </summary>
        private void FollowCameraTarget()
        {
            //Get the position the camera has to move to
            Vector3 targetPosition = new Vector3(cameraFollowTarget.position.x, cameraFollowTarget.position.y - .5f, cameraFollowTarget.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPosition, cameraFollowSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Rotate camera based on input (if not locked on)
        /// </summary>
        /// <param name="input">Camera movement input</param>
        private void RotateCamera(Vector2 input)
        {
            if (!playerManager.isLockingOn) SetCameraAngles(input); //Get camera angles if not locked on
            else SetCameraAnglesLockOn(); //Get camera angles if locked on

            //Clamp pitch angle
            cameraPitchAngle = Mathf.Clamp(cameraPitchAngle, minPitchAngle, maxPitchAngle);

            RotateCameraPitch(); //Rotate the camera on the vertical axis
            RotateCameraPivot(); //Rotate the camera on the horizontal axis

        }

        /// <summary>
        /// Set camera angles the camera will go to if locked on
        /// </summary>
        private void SetCameraAnglesLockOn()
        {
            if (lockedTarget == null) return; //If not locked on

            //Get angles
            float horizontalAngle = GetHorizontalLockOnAngle();
            float verticalAngle = GetVerticalLockOnAngle();

            //Change the pitch and pivot angles based on the camera movement input
            cameraPitchAngle -= (verticalAngle * cameraLockOnPitchSpeed * Time.deltaTime); //Vertical angle
            cameraPivotAngle -= (horizontalAngle * cameraLockOnPivotSpeed * Time.deltaTime); //Horizontal angle

        }

        /// <summary>
        /// Get horizontal angle to the target
        /// </summary>
        /// <returns>The horizontal angle to the target</returns>
        private float GetHorizontalLockOnAngle()
        {
            //Get direction to the target
            Vector3 targetDirection = (lockedTarget.position - transform.position).normalized;
            targetDirection.y = 0;

            //Get angle amount from the camera forward to the target direction
            float horizontalAngle = Vector3.Angle(transform.forward, targetDirection) * Mathf.Deg2Rad;

            //Return positive or negative angle based on the direction the camera will have to rotate to to align to the target (left or right)
            return (Vector3.Cross(transform.forward, targetDirection).y < 0) ? horizontalAngle : -horizontalAngle;
        }

        /// <summary>
        /// Get vertical angle to the target
        /// </summary>
        /// <returns>The vertical angle to the target</returns>
        private float GetVerticalLockOnAngle()
        {
            //Get direction to the target and forward of the camera pitch (only the y components)
            Vector3 targetDirection = new Vector3(0, ((lockedTarget.position - cameraPitchTransform.position).normalized).y, 1);
            Vector3 forward = new Vector3(0, cameraPitchTransform.forward.y, 1);

            //Get angle fro the current forward of the camera pitch and the target direction
            float verticalAngle = Vector3.Angle(forward, targetDirection) * Mathf.Deg2Rad;

            //Return positive or negative angle based on the direction the camera will have to rotate to to align to the target (up or down)
            return (Vector3.Cross(forward, targetDirection).x > 0) ? -verticalAngle : verticalAngle;
        }

        /// <summary>
        /// Set camera angles the camera will go to if not locked on
        /// </summary>
        /// <param name="input">Camera movement input</param>
        private void SetCameraAngles(Vector2 input)
        {
            //Change the pitch and pivot angles based on the camera movement input
            cameraPitchAngle -= (input.y * cameraPitchSpeed * Time.deltaTime); //Vertical angle
            cameraPivotAngle += (input.x * cameraPivotSpeed * Time.deltaTime); //Horizontal angle
        }

        /// <summary>
        /// Rotate camera vertically
        /// </summary>
        private void RotateCameraPitch()
        {
            //Get rotation based on the camera pitch angle
            Quaternion targetRotation = Quaternion.Euler(new Vector3(cameraPitchAngle, 0f, 0f));
            cameraPitchTransform.transform.localRotation = Quaternion.Slerp(cameraPitchTransform.transform.localRotation, targetRotation, .2f); //Rotate the camera nested inside
        }

        /// <summary>
        /// Rotate camera horizontally
        /// </summary>
        private void RotateCameraPivot()
        {
            //Get rotation based on the camera pivot angle
            Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, cameraPivotAngle, 0f));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, .2f); //Rotate
        }

        /// <summary>
        /// Makes the camera move if it collides with something
        /// </summary>
        private void HandleCameraCollisions()
        {
            //Get direction the camera is looking at
            Vector3 direction = (cameraTransform.position - cameraPitchTransform.position).normalized;

            //Check for collision and get the new distance in case of collision
            float cameraDistance = CheckForCollisions(direction);

            //Move camera to the new camera distance
            cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, cameraTransform.localPosition.y, Mathf.Lerp(cameraTransform.localPosition.z, cameraDistance, .2f)); //Move the camera to the new target
        }

        /// <summary>
        /// Gets the new distance the camera will have to go to if there is a collision with it
        /// </summary>
        /// <param name="direction">Direction the camera is looking at</param>
        /// <returns>The distance the camera will go to</returns>
        private float CheckForCollisions(Vector3 direction)
        {
            //Set distance as the default value
            float cameraDistance = (playerManager.isLockingOn)? -lockedOnDefaultCameraDistance : -defaultCameraDistance;
            
            RaycastHit hit;
            //Cast a sphere to see if the camera is hitting something
            if (Physics.SphereCast(cameraPitchTransform.position, .2f, direction, out hit, Mathf.Abs(cameraDistance), collisionLayers))
            {
                //If there was a hit
                float distance = Vector3.Distance(cameraPitchTransform.position, hit.point); //Get distance from the hit to the camera
                cameraDistance = -(distance - cameraCollisionOffset); //Adjust camera position based on the distance (minus a small offset)
            }

            return cameraDistance;
        }

        public void SetCameraPlayerManager(PlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        public void SetCameraInputManager(InputManager inputManager)
        {
            this.inputManager = inputManager;
        }

        public void SetCameraPhysicalHips(Rigidbody hips)
        {
            this.physicalHips = hips;
        }

        public void SetCameraFollowTransform(Transform target)
        {
            this.cameraFollowTarget = target;
        }

    }
}
