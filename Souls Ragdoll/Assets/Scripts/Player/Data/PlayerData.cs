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
        public float movingFriction = 0f; //Friction if player is moving
        public float idleFriction = .35f; //Friction if player is not moving

        [Header("Ground check")]
        public float minDistanceToFall = .1f; //How far the player has to be from the ground to be actually falling
        public LayerMask groundCollisionLayers; //What is considered ground

        [Header("Falling")]
        public float baseGravityForce = 120f; //The downwards force that is always applied to the player
        public float fallingSpeed = 10f; //The rate at which the velocity will increase when falling
        public float maxFallingSpeed = 34f; //Max magnitude of the velocity when falling
        public float timeBeforeFalling = .2f; //Time that the player must stay in air for before he starts the falling "state"
        public float knockoutLandThreshold = 1.8f; //Time the player can fall before being ko's when landing
        public float upwardLandingForce = 20f; //When the player gets knocked out this force is applied to the body to make it "jump"

        [Header("Roll and Backdash")]
        public float backdashSpeedMultiplier = 1f; //Velocity multiplier when backdashing
        public float rollSpeedMultiplier = 1f; //Velocity multiplier when backdashing

        [Header("KO")]
        public float KOTime = 3.6f; //The time the player will be ko for
        public float maxKOTime = 10f; //Max time the player can be ko for
    }
}
