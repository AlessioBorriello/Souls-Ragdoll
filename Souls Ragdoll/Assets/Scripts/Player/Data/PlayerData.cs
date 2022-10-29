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
        public bool tiltOnDirectionChange = true; //If the player tilts when changing direction
        public float maxTiltAmount = 3.4f; //How much the player can tilt when changing direction
        public float tiltSpeed = 1.5f; //How fast the player tilts
        public float speedNeededToTilt = .096f; //How fast the player must be moving before it starts to tilt
        //public float minimumSharpTurnAngle = 130f; //Angle needed to do a sharp turn animation (180 turn)


        [Header("Ground check")]
        public float minDistanceToFall = .1f; //How far the player has to be from the ground to be actually falling
        public LayerMask groundCollisionLayers; //What is considered ground

        [Header("Falling")]
        public float baseGravityForce = 120f; //The downwards force that is always applied to the player
        public float fallingSpeed = 10f; //The rate at which the velocity will increase when falling
        public float maxFallingSpeed = 34f; //Max magnitude of the velocity when falling
        public float timeBeforeFalling = .2f; //Time that the player must stay in air for before he starts the falling "state"
        public float knockoutLandThreshold = 1.8f; //Time the player can fall before being ko's when landing

        [Header("Roll and Backdash")]
        public float backdashSpeedMultiplier = 1f; //Velocity multiplier when backdashing
        public float rollSpeedMultiplier = 1f; //Velocity multiplier when backdashing

        [Header("KO")]
        public float KOTime = 3.6f; //The time the player will be ko for
        public float maxKOTime = 10f; //Max time the player can be ko for
        public float wakeUpTime = .6f; //Time it takes for the player's body to wake up

        [Header("KO Resistances")]
        public float hipResistance = 120;
        public float legResistance = 200;
        public float shinResistance = 250;
        public float footResistance = 350;
        public float torsoResistance = 120;
        public float armResistance = 180;
        public float forearmResistance = 220;
        public float handResistance = 300;
        public float neckResistance = 150;
        public float headResistance = 80;
    }
}
