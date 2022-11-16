using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AlessioBorriello
{
    public class ActiveRagdollManager : MonoBehaviour
    {
        private PlayerManager playerManager; //Player manager
        private Animator animator; //Animator of the animated character

        private Transform animatedHips; //Hips of the animated character
        private Rigidbody physicalHips; //Hips of the physical character

        private Transform[] animatedBones; //Set of transforms of the animated bones starting from the animated hips
        private ConfigurableJoint[] joints; //Set of joints of the physical bones starting from the physical hips
        private Quaternion[] initialJointRotations; //Set of starting rotations of the joints
        private Collider[] colliders; //Set of colliders of the joints
        private Rigidbody[] bodies; //Set of rigidbodies of the player

        private List<GameObject> arms = new List<GameObject>();
        private LayerMask characterLayer;
        private LayerMask ignoreCharacterLayer;

        private float knockedOutTimer = 0;
        private float safenetKnockedOutTimer = 0; //Used to check if the player has been knocked out for too much
        
        private float targetHipsForce;
        private float targetJointForce;


        private void Start()
        {
            Initialize();
        }

        void FixedUpdate()
        {
            UpdateJointTargets(joints, animatedBones, initialJointRotations); //Update the joints target rotation to match the relative animated bone rotation
            SyncPosition(); //Sync the position of the 2 hips
        }

        private void Initialize()
        {
            playerManager = GetComponent<PlayerManager>(); //Get player manager
            animator = playerManager.GetAnimationManager().GetAnimator(); //Get animator

            //Get hips
            physicalHips = playerManager.GetPhysicalHips();
            animatedHips = playerManager.GetAnimatedHips();

            SetUpBonesAndJoints();

            joints[(int)BodyParts.Hip].configuredInWorldSpace = true; //Set hips in world space

            SetupColliders();

            initialJointRotations = GetJointsStartingLocalRotations(joints); //Get initial rotations of athe joints

            targetHipsForce = playerManager.playerData.hipsJointDriveForce;
            targetJointForce = playerManager.playerData.jointDriveForce;

            SetJointsDriveForces(targetHipsForce, targetJointForce); //Set up joint drive forces

            //Get arms
            GetArms();

            //Get layers
            characterLayer = LayerMask.NameToLayer("Character");
            ignoreCharacterLayer = LayerMask.NameToLayer("IgnoreCharacter");
        }

        private void SetUpBonesAndJoints()
        {
            if (animatedBones == null) animatedBones = animatedHips.GetComponentsInChildren<Transform>(); //Get transforms of the animated bones starting from the hips
            if (joints == null) joints = physicalHips.GetComponentsInChildren<ConfigurableJoint>(); //Get joints of the physical bones starting from the hips
            if (colliders == null) colliders = physicalHips.GetComponentsInChildren<Collider>(); //Get all the colliders
            if (bodies == null) bodies = physicalHips.GetComponentsInChildren<Rigidbody>(); //Get all the rigid bodies
        }

        public IEnumerator SetJointsDriveForcesOverTime(float targetHipsForce, float targetJointForce, float time)
        {

            float timeElapsed = 0;
            float startHipsForce = physicalHips.GetComponent<ConfigurableJoint>().angularXDrive.positionSpring;
            float startJointForce = physicalHips.transform.GetChild(0).GetComponent<ConfigurableJoint>().angularXDrive.positionSpring;

            while (timeElapsed < time)
            {
                SetJointsDriveForces(Mathf.Lerp(startHipsForce, targetHipsForce, timeElapsed / time), Mathf.Lerp(startJointForce, targetJointForce, timeElapsed / time));
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            SetJointsDriveForces(targetHipsForce, targetJointForce);

        }

        public void SetJointsDriveForces(float hipsForce, float jointForce)
        {
            //Set hips joint drive
            JointDrive jointDrive = new JointDrive(); //Create new drive
            jointDrive.maximumForce = float.PositiveInfinity; //Set the max force to infinite (It is set to 0 otherwise and the ragdoll falls)

            jointDrive.positionSpring = hipsForce; //Set the force of the spring for the hips
            SetJointDrive(joints[0], jointDrive); //Set the drive

            //Set rest of the joint drives
            jointDrive.positionSpring = jointForce; //Set the force of the spring for the rest of the body
            for (int i = 1; i < joints.Length; i++)
            {
                SetJointDrive(joints[i], jointDrive); //Set the drives
            }
        }

        private void SetJointDrive(ConfigurableJoint joint, JointDrive jointDrive)
        {
            //Set drive of the joint
            joint.angularXDrive = jointDrive;
            joint.angularYZDrive = jointDrive;
        }

        private Quaternion[] GetJointsStartingLocalRotations(ConfigurableJoint[] joints)
        {
            Quaternion[] startingRotations = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++) //For all the joints
            {
                startingRotations[i] = joints[i].transform.localRotation; //Get local rotation of each joint
            }
            return startingRotations;
        }

        private void UpdateJointTargets(ConfigurableJoint[] joints, Transform[] targetBonesRotations, Quaternion[] initialRotations)
        {
            SetTargetRotation(joints[0], targetBonesRotations[0].rotation, initialRotations[0]); //Update target rotation of the hips

            for (int i = 1; i < joints.Length; i++) //Start from 1 to ignore hip
            {
                SetTargetRotationLocal(joints[i], targetBonesRotations[i].localRotation, initialRotations[i]); //Update target rotation
            }
        }

        private void SyncPosition()
        {
            //Sync the 2 hips position
            animator.transform.position = physicalHips.position + (animator.transform.position - animatedHips.position);
        }

        private void SyncRotation()
        {
            //Sync the 2 hips rotation
            animator.transform.rotation = physicalHips.rotation;
        }

        private void SetTargetRotationLocal(ConfigurableJoint joint, Quaternion targetLocalRotation, Quaternion startLocalRotation)
        {
            if (joint.configuredInWorldSpace)
            {
                Debug.LogError("SetTargetRotationLocal should not be used with joints that are configured in world space. For world space joints, use SetTargetRotation.", joint);
            }
            SetTargetRotationInternal(joint, targetLocalRotation, startLocalRotation, Space.Self);
        }

        private void SetTargetRotation(ConfigurableJoint joint, Quaternion targetWorldRotation, Quaternion startWorldRotation)
        {
            if (!joint.configuredInWorldSpace)
            {
                Debug.LogError("SetTargetRotation must be used with joints that are configured in world space. For local space joints, use SetTargetRotationLocal.", joint);
            }
            SetTargetRotationInternal(joint, targetWorldRotation, startWorldRotation, Space.World);
        }

        private void SetTargetRotationInternal(ConfigurableJoint joint, Quaternion targetRotation, Quaternion startRotation, Space space)
        {
            // Calculate the rotation expressed by the joint's axis and secondary axis
            var right = joint.axis;
            var forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
            var up = Vector3.Cross(forward, right).normalized;
            Quaternion worldToJointSpace = Quaternion.LookRotation(forward, up);

            // Transform into world space
            Quaternion resultRotation = Quaternion.Inverse(worldToJointSpace);

            // Counter-rotate and apply the new local rotation.
            // Joint space is the inverse of world space, so we need to invert our value
            if (space == Space.World)
            {
                resultRotation *= startRotation * Quaternion.Inverse(targetRotation);
            }
            else
            {
                resultRotation *= Quaternion.Inverse(targetRotation) * startRotation;
            }

            // Transform back into joint space
            resultRotation *= worldToJointSpace;

            // Set target rotation to our newly calculated rotation
            joint.targetRotation = resultRotation;
        }

        private void SetupColliders()
        {
            //Hips
            Physics.IgnoreCollision(colliders[(int)BodyParts.Hip], colliders[(int)BodyParts.Legl]); //Hip and Leg.l
            Physics.IgnoreCollision(colliders[(int)BodyParts.Hip], colliders[(int)BodyParts.Legr]); //Hip and Leg.r
            Physics.IgnoreCollision(colliders[(int)BodyParts.Hip], colliders[(int)BodyParts.Torso]); //Hip and Torso
            Physics.IgnoreCollision(colliders[(int)BodyParts.Hip], colliders[(int)BodyParts.Arml]); //Hip and Arm.l
            Physics.IgnoreCollision(colliders[(int)BodyParts.Hip], colliders[(int)BodyParts.Armr]); //Hip and Arm.r

            //Left leg
            Physics.IgnoreCollision(colliders[(int)BodyParts.Legl], colliders[(int)BodyParts.Shinl]); //Leg.l and Shin.l
            Physics.IgnoreCollision(colliders[(int)BodyParts.Shinl], colliders[(int)BodyParts.Footl]); //Shin.l and Foot.l

            //Right leg
            Physics.IgnoreCollision(colliders[(int)BodyParts.Legr], colliders[(int)BodyParts.Shinr]); //Leg.r and Shin.r
            Physics.IgnoreCollision(colliders[(int)BodyParts.Shinr], colliders[(int)BodyParts.Footr]); //Shin.r and Foot.r

            //Torso
            Physics.IgnoreCollision(colliders[(int)BodyParts.Torso], colliders[(int)BodyParts.Arml]); //Torso and Arm.l
            Physics.IgnoreCollision(colliders[(int)BodyParts.Torso], colliders[(int)BodyParts.Armr]); //Torso and Arm.r
            Physics.IgnoreCollision(colliders[(int)BodyParts.Torso], colliders[(int)BodyParts.Neck]); //Torso and Neck
            Physics.IgnoreCollision(colliders[(int)BodyParts.Torso], colliders[(int)BodyParts.Head]); //Torso and Head

            //Left arm
            Physics.IgnoreCollision(colliders[(int)BodyParts.Arml], colliders[(int)BodyParts.Forearml]); //Arm.l and Forearm.l
            Physics.IgnoreCollision(colliders[(int)BodyParts.Forearml], colliders[(int)BodyParts.Handl]); //Forearm.l and Hand.l

            //Right arm
            Physics.IgnoreCollision(colliders[(int)BodyParts.Armr], colliders[(int)BodyParts.Forearmr]); //Arm.r and Forearm.r
            Physics.IgnoreCollision(colliders[(int)BodyParts.Forearmr], colliders[(int)BodyParts.Handr]); //Forearm.r and Hand.r

            //Neck
            Physics.IgnoreCollision(colliders[(int)BodyParts.Neck], colliders[(int)BodyParts.Head]); //Neck and Head
        }

        /// <summary>
        /// Starts a timer every time the player's body is still,
        /// if the player's body moves, the timer is reset,
        /// if it reacheas 0, he wakes up
        /// </summary>
        public void HandleWakeUp()
        {

            if (!playerManager.isKnockedOut) return;

            if (IsPlayerVelocityApproxZero(.08f)) //If the player body has stopped
            {
                knockedOutTimer -= Time.deltaTime; //Decreases timer
                if (knockedOutTimer <= 0) //If timer is up
                {
                    WakeUp(playerManager.playerData.wakeUpTime); //Wake up
                }
            }
            else //The player's body has moved
            {
                knockedOutTimer = playerManager.playerData.KOTime; //Reset timer
            }

            safenetKnockedOutTimer -= Time.deltaTime;
            if(safenetKnockedOutTimer <= 0) WakeUp(playerManager.playerData.wakeUpTime); //Safety net wake up

        }

        /// <summary>
        /// Wake the player up
        /// </summary>
        public void WakeUp(float time)
        {
            //Wake up
            playerManager.isKnockedOut = false;
            playerManager.playerIsStuckInAnimation = false;
            StartCoroutine(SetJointsDriveForcesOverTime(playerManager.playerData.hipsJointDriveForce, playerManager.playerData.jointDriveForce, time));

            knockedOutTimer = 0;
            safenetKnockedOutTimer = 0;
        }

        /// <summary>
        /// Knockout the player, set the joint drive forces to 0
        /// </summary>
        public void KnockOut(float time = 0)
        {
            playerManager.isKnockedOut = true;
            playerManager.playerIsStuckInAnimation = true;

            if (time == 0) SetJointsDriveForces(0, 0);
            else SetJointsDriveForcesOverTime(0, 0, time);

            knockedOutTimer = playerManager.playerData.KOTime;
            safenetKnockedOutTimer = playerManager.playerData.maxKOTime;

            //Changes friction of the feet so that they don't slide around (set it to idle friction)
            playerManager.shouldSlide = false;
        }

        public void Die()
        {
            playerManager.isDead = true;

            SetJointsDriveForces(0, 0);

            //Changes friction of the feet so that they don't slide around (set it to idle friction)
            playerManager.shouldSlide = false;
        }

        /// <summary>
        /// Checks if the player's velocity is approx 0
        /// </summary>
        private bool IsPlayerVelocityApproxZero(float approximation)
        {
            return (Mathf.Abs(physicalHips.velocity.magnitude) < approximation);
        }

        /// <summary>
        /// Adds a force to all of the player's rigidbodies
        /// </summary>
        public void AddForceToPlayer(Vector3 force, ForceMode mode)
        {
            //Hips
            physicalHips.AddForce(force, mode);

            //Rest
            foreach(Rigidbody rb in bodies) {
                rb.AddForce(force, mode);
            }

        }

        /// <summary>
        /// Adds a force to a specific rigidbody
        /// </summary>
        public void AddForceToBodyPart(Rigidbody part, Vector3 force, ForceMode mode)
        {
            part.AddForce(force, mode);
        }

        private void GetArms()
        {
            arms.Add(GetBodyPart(BodyParts.Armr).gameObject);
            arms.Add(GetBodyPart(BodyParts.Forearmr).gameObject);
            arms.Add(GetBodyPart(BodyParts.Handr).gameObject);

            arms.Add(GetBodyPart(BodyParts.Arml).gameObject);
            arms.Add(GetBodyPart(BodyParts.Forearml).gameObject);
            arms.Add(GetBodyPart(BodyParts.Handl).gameObject);
        }

        public Rigidbody GetBodyPart(BodyParts part)
        {
            return bodies[(int)part];
        }

        public void ToggleCollisionOfArms(bool enable)
        {
            foreach (GameObject part in arms)
            {
                part.layer = (enable)? characterLayer : ignoreCharacterLayer;
            }
        }

    }

    public enum BodyParts
    {
        Hip,
        Legl,
        Shinl,
        Footl,
        Legr,
        Shinr,
        Footr,
        Torso,
        Arml,
        Forearml,
        Handl,
        Armr,
        Forearmr,
        Handr,
        Neck,
        Head
    }

}