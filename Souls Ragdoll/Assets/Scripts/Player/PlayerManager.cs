using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerManager : MonoBehaviour
    {

        /*
        [SerializeField] public PlayerData playerData; //Player data reference

        /// <summary>
        /// Move the player with the root motion of the animator
        /// </summary>
        public void MovePlayerWithAnimation(float speedMultiplier)
        {
            PhysicalHips.velocity = Vector3.ProjectOnPlane(PlayerAnimationManager.TargetAnimator.velocity * speedMultiplier, GroundNormal);
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
        /// Get the direction of the movement based on the camera
        /// </summary>
        /// <returns>The direction</returns>
        private Vector3 GetMovementDirection()
        {
            Vector3 movementDirection = Vector3.ProjectOnPlane(MainCamera.transform.forward, Vector3.up) * MovementInput.y; //Camera's current z axis * vertical movement (Up, down input)
            movementDirection += Vector3.ProjectOnPlane(MainCamera.transform.right, Vector3.up) * MovementInput.x; //Camera's current z axis * horizontal movement (Right, Left input)
            movementDirection.Normalize();
            movementDirection.y = 0; //Remove y component from the vector (Y component of the vector from the camera should be ignored)
            movementDirection = Vector3.ProjectOnPlane(movementDirection, (isOnGround) ? GroundNormal : Vector3.up);

            if (MovementInput.magnitude == 0) return AnimatedPlayer.transform.forward; //If not moving return the forward

            return movementDirection;
        }

        /// <summary>
        /// Brings the player to a stop,
        /// the higher the drag force of the player data, the faster it stops
        /// </summary>
        public void DecreaseVelocity()
        {
            PhysicalHips.velocity = Vector3.Lerp(PhysicalHips.velocity, Vector3.zero, playerData.dragForce);
        }
        */

    }
}
