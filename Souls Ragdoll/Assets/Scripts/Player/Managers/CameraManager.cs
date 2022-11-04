using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;
using static UnityEngine.GraphicsBuffer;

namespace AlessioBorriello
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] public PlayerManager playerManager;

        [Header("Camera options")]
        [SerializeField] private Transform cameraFollowTarget; //Transform of the target
        [SerializeField] Transform cameraPitchTransform; //Transform of the pitch camera (Used to look up and down)
        [SerializeField] private float cameraHeight = 1.8f;
        [SerializeField] private float defaultCameraDistance = 6f; //Camera distance when not obstructed
        [SerializeField] private float cameraFollowSpeed = .3f; //How fast the camera will reach the target
        [SerializeField] private float cameraPitchSpeed = 5f; //How fast the camera goes up and down
        [SerializeField] private float cameraPivotSpeed = 5f; //How fast the camera goes left and right
        [SerializeField] private float cameraCollisionOffset = .2f; //How much the camera will push away from a collision
        [SerializeField] private LayerMask collisionLayers; //What the camera will collide with
        [SerializeField] private bool followTarget = true; //If the camera should follow the player

        private Transform lockedTarget;

        private float cameraPitchAngle; //Up and down angle
        private float cameraPivotAngle; //Left and right angle

        private Transform cameraTransform; //Transform of the actual camera

        //Pitch limits
        private float minPitchAngle = -45;
        private float maxPitchAngle = 52;

        private Vector3 cameraFollowVelocity; //Vector storing the current velocity of the camera

        private void Awake()
        {
            cameraTransform = Camera.main.transform;
        }

        public void HandleCameraMovement()
        {
            cameraPitchTransform.localPosition = new Vector3(0, Mathf.Lerp(cameraPitchTransform.localPosition.y, cameraHeight, .2f), 0);

            if (followTarget)
            {
                FollowTarget();
                HandleCameraCollisions();
            }

            RotateCamera(playerManager.inputManager.cameraInput);
        }

        public void HandleLockOnControls()
        {
            //Find target
            if (playerManager.canLockOn && playerManager.inputManager.rightStickInputPressed)
            {
                LockOnPressed();
            }

            //Lose target
            if(lockedTarget != null && playerManager.isLockedOn)
            {
                HandleLosingLockOnTarget();
                HandleChangingTarget();
            }
        }

        private void HandleLosingLockOnTarget()
        {
            float distanceFromTarget = Vector3.Distance(playerManager.physicalHips.position, lockedTarget.position);
            Vector3 directionToTarget = (lockedTarget.position - playerManager.physicalHips.position).normalized;

            bool isObstructed = (Physics.Raycast(playerManager.physicalHips.position, directionToTarget, Vector3.Distance(playerManager.physicalHips.position, lockedTarget.position), playerManager.playerData.obstructionMask));
            if (distanceFromTarget > playerManager.playerData.maxLoseTargetDistance || isObstructed)
            {
                lockedTarget = null;
                playerManager.isLockedOn = false;
            }
        }

        private bool canChangeTarget = true;
        private void HandleChangingTarget()
        {
            if (playerManager.inputManager.cameraInput.magnitude > .9f && canChangeTarget)
            {
                canChangeTarget = false;
                bool changeLeft = (playerManager.inputManager.cameraInput.x > 0) ? false : true;

                Transform newTarget = GetLockOnTarget(changeLeft);
                if(newTarget != null && newTarget != lockedTarget) lockedTarget = newTarget;

            }

            if(playerManager.inputManager.cameraInput.magnitude == 0 && !canChangeTarget)
            {
                canChangeTarget = true;
            }

        }

        private void LockOnPressed()
        {
            if (lockedTarget == null)
            {
                lockedTarget = GetLockOnTarget();
                if (lockedTarget != null) playerManager.isLockedOn = true;
            }
            else
            {
                lockedTarget = null;
                playerManager.isLockedOn = false;
            }
        }

        private Transform GetLockOnTarget(bool changingLeft = false)
        {

            Collider[] collidersInsideRadius = Physics.OverlapSphere(playerManager.physicalHips.position, playerManager.playerData.maxLockOnDistance, playerManager.playerData.targetMask);

            if (collidersInsideRadius.Length == 0) return null;

            List<Transform> possibleTargets = new List<Transform>();
            List<int> foundIds = new List<int>();

            foreach (Collider c in collidersInsideRadius)
            {
                int id = c.transform.root.GetInstanceID();
                //If already managed or if it's the caller's own colliders we are checking
                if (foundIds.Contains(id) || c.transform.root == playerManager.transform.root) continue;

                foundIds.Add(id);

                CharacterManager possibleTarget = c.transform.root.GetComponent<CharacterManager>();
                if (possibleTarget == null) continue;

                Transform target = possibleTarget.lockOnTargetTransform;

                //If already locked on, we are changing target
                if (lockedTarget != null)
                {
                    //If it's the same target as the one already locked
                    if (target == lockedTarget) continue;

                    //If it's on the opposite direction of the one we wanted to change to
                    Vector3 directionToLockedTarget = (lockedTarget.position - transform.position).normalized;
                    Vector3 directionToPotentialTarget = (target.position - transform.position).normalized;

                    bool isOnLeft = (Vector3.Cross(directionToPotentialTarget, directionToLockedTarget).y < 0) ? false : true;

                    if (isOnLeft != changingLeft) continue;
                }

                //Add only those who are in front and are not obstructed
                if (IsLockable(target, playerManager.physicalHips.transform)) possibleTargets.Add(target);

            }

            return GetNearestTarget(playerManager.physicalHips.transform.position, possibleTargets);

        }

        private Transform GetNearestTarget(Vector3 hipsPosition, List<Transform> targets)
        {
            if (targets.Count == 0) return null;

            Transform nearestTarget = targets[0];

            Vector3 direction = (nearestTarget.position - transform.position).normalized;
            float minAngle = Vector3.Angle(transform.forward, direction);

            float minDistance = Vector3.Distance(hipsPosition, nearestTarget.position);

            foreach (Transform target in targets)
            {
                if (target == nearestTarget) continue;

                direction = (target.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, direction);

                float distance = Vector3.Distance(hipsPosition, target.position);
                float percentageDistance = (minDistance - distance) / minDistance;

                //If a target is significantly closer (35% closer) or if it's more in line with the camera (in the center)
                if (percentageDistance > .35f || angle < minAngle)
                {
                    //Update target
                    nearestTarget = target;
                    minAngle = angle;
                    minDistance = distance;
                }
            }

            return nearestTarget;
        }

        private bool IsLockable(Transform targetTransform, Transform hipsTransform)
        {
            //Find unfit targets
            Vector3 direction = (targetTransform.position - transform.position).normalized;

            bool isInFront = (Vector3.Angle(transform.forward, direction) < playerManager.playerData.maxLockOnAngle / 2);
            if (!isInFront) return false;

            bool isObstructed = (Physics.Raycast(hipsTransform.position, direction, Vector3.Distance(hipsTransform.position, targetTransform.position), playerManager.playerData.obstructionMask));
            if (isObstructed) return false;

            return true;
        }

        private void FollowTarget()
        {
            //Get the position the camera has to move to
            transform.position = Vector3.SmoothDamp(transform.position, new Vector3(cameraFollowTarget.position.x, cameraFollowTarget.position.y - .5f, cameraFollowTarget.position.z), ref cameraFollowVelocity, cameraFollowSpeed);
        }

        private void RotateCamera(Vector2 input)
        {
            if (!playerManager.isLockedOn) GetCameraAngles(input);
            else GetCameraAnglesLockOn();

            RotateCameraPitch(); //Rotate the camera on the vertical axis
            RotateCameraPivot(); //Rotate the camera on the horizontal axis

        }

        private void GetCameraAnglesLockOn()
        {
            if (lockedTarget == null) return;

            float horizontalAngle = GetHorizontalLockOnAngle();
            float verticalDifference = GetVerticalLockOnAngle();

            cameraPitchAngle -= (verticalDifference * cameraPitchSpeed * Time.deltaTime); //Vertical angle
            cameraPivotAngle -= (horizontalAngle * cameraPivotSpeed * Time.deltaTime); //Horizontal angle

            //Clamp pitch angle
            cameraPitchAngle = Mathf.Clamp(cameraPitchAngle, minPitchAngle, maxPitchAngle);

        }

        private float GetHorizontalLockOnAngle()
        {
            Vector3 targetDirection = (lockedTarget.position - transform.position).normalized;
            targetDirection.y = 0;

            float horizontalAngle = Vector3.Angle(transform.forward, targetDirection) * Mathf.Deg2Rad;

            return (Vector3.Cross(transform.forward, targetDirection).y < 0) ? horizontalAngle : -horizontalAngle;
        }

        private float GetVerticalLockOnAngle()
        {
            Vector3 targetDirection = new Vector3(0, ((lockedTarget.position - cameraPitchTransform.position).normalized).y, 1);
            Vector3 forward = new Vector3(0, cameraPitchTransform.forward.y, 1);

            float verticalAngle = Vector3.Angle(forward, targetDirection) * Mathf.Deg2Rad;

            return (Vector3.Cross(forward, targetDirection).x > 0) ? -verticalAngle : verticalAngle;
        }

        private void GetCameraAngles(Vector2 input)
        {
            cameraPitchAngle -= (input.y * cameraPitchSpeed * Time.deltaTime); //Vertical angle
            cameraPivotAngle += (input.x * cameraPivotSpeed * Time.deltaTime); //Horizontal angle

            //Clamp pitch angle
            cameraPitchAngle = Mathf.Clamp(cameraPitchAngle, minPitchAngle, maxPitchAngle);
        }

        private void RotateCameraPitch()
        {
            Quaternion rotation = Quaternion.Euler(new Vector3(cameraPitchAngle, 0f, 0f));
            cameraPitchTransform.transform.localRotation = rotation; //Rotate the camera nested inside
        }

        private void RotateCameraPivot()
        {
            Quaternion rotation = Quaternion.Euler(new Vector3(0f, cameraPivotAngle, 0f));
            transform.rotation = rotation;
        }

        private void HandleCameraCollisions()
        {
            Vector3 direction = (cameraTransform.position - cameraPitchTransform.position).normalized; //Direction of the ray, going outwards from where the camera is looking

            float cameraDistance = CheckForCollisions(direction); //Check for collision and get the new distance in case of collision

            //if (Mathf.Abs(cameraDistance) < .2f) cameraDistance -= .2f;

            cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, cameraTransform.localPosition.y, Mathf.Lerp(cameraTransform.localPosition.z, cameraDistance, .2f)); //Move the camera to the new target
        }

        private float CheckForCollisions(Vector3 direction)
        {
            float cameraDistance = -defaultCameraDistance;
            RaycastHit hit;

            //Cast a sphere to see if the camera is hitting something
            if (Physics.SphereCast(cameraPitchTransform.position, .2f, direction, out hit, Mathf.Abs(cameraDistance), collisionLayers))
            {

                float distance = Vector3.Distance(cameraPitchTransform.position, hit.point); //Get distance from the hit to the camera
                cameraDistance = -(distance - cameraCollisionOffset); //Adjust camera position based on the distance (minus a small offset)

            }

            return cameraDistance;
        }

    }
}
