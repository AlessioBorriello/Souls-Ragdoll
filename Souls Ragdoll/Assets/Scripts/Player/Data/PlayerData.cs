using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    [CreateAssetMenu(fileName = "newPlayerData", menuName = "Data/Player Data/Base Data")]
    public class PlayerData : ScriptableObject
    {

        [Header("Ragdoll")]
        public float hipsJointDriveForce; //The force of the spring in the hips joint
        public float jointDriveForce; //The force of the springs in the rest of the body

        [Header("Movement")]
        public float walkSpeedMultiplier = 1f; //Velocity multiplier when walking
        public float runSpeedMultiplier = 1f; //Velocity multiplier when running
        public float sprintSpeedMultiplier = 1f; //Velocity multiplier when sprinting
        public float sprintThreshold = .2f; //The roll button has to be pressed for this amount of time to sprint
        public float rotationSpeed = 10f; //Rotation speed
        public float inAirRotationSpeed = 4f; //Rotation speed while in air
        public float maxSlopeAngle = 30f; //Max angle of a slope before it is considered unclimbable
        public float dragForce = .5f; //Force that stops the player when not moving

        [Header("Ground check")]
        public float groundCheckSphereRadius = .2f; //How large is the casted sphere
        public float minDistanceToFall = .1f; //How far the player has to be from the ground to be actually falling
        public float minDistanceToFallWhileRolling = .8f; //How far the player has to be from the ground to be actually falling while in the roll state
        public LayerMask groundCollisionLayers; //What is considered ground

        [Header("Falling")]
        public float fallingSpeed = 10f; //The rate at which the velocity will increase when falling
        public float maxFallingSpeed = 34f; //Max magnitude of the velocity when falling
        public float fallPushStrenght = 15f; //Strenght of the push given to a player when he starts falling, it's in the facing direction
        public float hardLandThreshold = 1f; //Time the player can fall before going into a hard landing state
        public float knockoutLandThreshold = 1.8f; //Time the player can fall before being ko's when landing

        [Header("Roll and Backdash")]
        public float backdashSpeedMultiplier = 1f; //Velocity multiplier when backdashing
        public float rollSpeedMultiplier = 1f; //Velocity multiplier when backdashing

        [Header("KO")]
        public float KOTime = 4f; //The time the player will be ko for
        public float startingPushForce = 5f; //The strenght that will push the ragdoll for a small time after being ko'd upwards
        public float timeOfBeingPushed = .3f; //For how much will the push force be applied
    }
}
