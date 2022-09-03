using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private Transform targetTransform; //Transform of the target
        [SerializeField] Transform cameraPitchTransform; //Transform of the pitch camera (Used to look up and down)
        [SerializeField] private float cameraFollowSpeed = .3f; //How fast the camera will reach the target
        [SerializeField] private float cameraPitchSpeed = 5f; //How fast the camera goes up and down
        [SerializeField] private float cameraPivotSpeed = 5f; //How fast the camera goes left and right
        [SerializeField] private float cameraCollisionOffset = .2f; //How much the camera will push away from a collision
        [SerializeField] private LayerMask collisionLayers; //What the camera will collide with
        [SerializeField] private InputManager inputManager; //Input manager of the player

        public bool followTarget = true; //If the camera should follow the player

        private float cameraPitchAngle; //Up and down angle
        private float cameraPivotAngle; //Left and right angle

        private Transform cameraTransform; //Transform of the actual camera
        private float defaultCameradistance; //Camera distance when not obstructed

        //Pitch limits
        private float minPitchAngle = -45;
        private float maxPitchAngle = 52;

        private Vector3 cameraFollowVelocity; //Vector storing the current velocity of the camera

        private void Awake()
        {
            cameraTransform = Camera.main.transform;
            defaultCameradistance = cameraTransform.localPosition.z; //Get the distance
        }

        private void LateUpdate()
        {
            if (followTarget)
            {
                FollowTarget();
                HandleCameraCollisions();
            }

            HandleCamera(inputManager.cameraInput);

        }

        public void HandleCamera(Vector2 input)
        {
            RotateCamera(input);
        }

        private void FollowTarget()
        {
            //Get the position the camera has to move to
            transform.position = Vector3.SmoothDamp(transform.position, new Vector3(targetTransform.position.x, targetTransform.position.y - .5f, targetTransform.position.z), ref cameraFollowVelocity, cameraFollowSpeed);
        }

        private void RotateCamera(Vector2 input)
        {
            GetCameraAngles(input);
            RotateCameraPitch(); //Rotate the camera on the vertical axis
            RotateCameraPivot(); //Rotate the camera on the horizontal axis

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
            float cameraDistance = defaultCameradistance;
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
