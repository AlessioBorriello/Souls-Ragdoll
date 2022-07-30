using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ActiveRagdollManager : MonoBehaviour
{

    [SerializeField] private Transform animatedHips; //Hips of the animated character
    [SerializeField] private Rigidbody physicalHips; //Hips of the physical character

    public Transform[] AnimatedBones { get; private set; } //Set of transforms of the animated bones starting from the animated hips
    public ConfigurableJoint[] Joints { get; private set; } //Set of joints of the physical bones starting from the physical hips
    private Quaternion[] initialJointRotations; //Set of starting rotations of the joints
    private Collider[] Colliders; //Set of colliders of the joints

    private Animator animator; //Animator of the animated character

    [Header("Ragdoll forces")]
    public float hipsJointDriveForce; //The force of the spring in the hips joint
    public float jointDriveForce; //The force of the springs in the rest of the body

    private void Awake()
    {
        if (AnimatedBones == null) AnimatedBones = animatedHips.GetComponentsInChildren<Transform>(); //Get transforms of the animated bones starting from the hips
        if (Joints == null) Joints = physicalHips.GetComponentsInChildren<ConfigurableJoint>(); //Get joints of the physical bones starting from the hips
        if (Colliders == null) Colliders = physicalHips.GetComponentsInChildren<Collider>(); //Get all the colliders

        Joints[0].configuredInWorldSpace = true;
        SetupColliders();
    }

    private void Start()
    {
        animator = GetComponentInChildren<Animator>(); //Get animator
        initialJointRotations = GetJointsStartingLocalRotations(Joints); //Get initial rotations of athe joints

        SetJointsDriveForces(hipsJointDriveForce, jointDriveForce); //Set up joint drive forces
    }

    void FixedUpdate()
    {
        UpdateJointTargets(Joints, AnimatedBones, initialJointRotations); //Update te joints target rotation to match the relative animated bone rotation
        SyncPosition(); //Sync the position of the 2 hips
        //SyncRotation();
    }

    public void SetJointsDriveForces(float hipsForce, float jointForce)
    {
        //Set hips joint drive
        JointDrive jointDrive = new JointDrive(); //Create new drive
        jointDrive.maximumForce = float.PositiveInfinity; //Set the max force to infinite (It is set to 0 otherwise and the ragdoll falls)

        jointDrive.positionSpring = hipsForce; //Set the force of the spring for the hips
        SetJointDrive(Joints[0], jointDrive); //Set the drive

        //Set rest of the joint drives
        jointDrive.positionSpring = jointForce; //Set the force of the spring for the rest of the body
        for (int i = 1; i < Joints.Length; i++)
        {
            SetJointDrive(Joints[i], jointDrive); //Set the drives
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
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Hip], Colliders[(int)bodyParts.Legl]); //Hip and Leg.l
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Hip], Colliders[(int)bodyParts.Legr]); //Hip and Leg.r
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Hip], Colliders[(int)bodyParts.Torso]); //Hip and Torso
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Hip], Colliders[(int)bodyParts.Arml]); //Hip and Arm.l
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Hip], Colliders[(int)bodyParts.Armr]); //Hip and Arm.r
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Hip], Colliders[(int)bodyParts.Forearml]); //Hip and Forearm.l
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Hip], Colliders[(int)bodyParts.Forearmr]); //Hip and Forearm.r

        //Left leg
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Legl], Colliders[(int)bodyParts.Shinl]); //Leg.l and Shin.l
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Shinl], Colliders[(int)bodyParts.Footl]); //Shin.l and Foot.l

        //Right leg
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Legr], Colliders[(int)bodyParts.Shinr]); //Leg.r and Shin.r
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Shinr], Colliders[(int)bodyParts.Footr]); //Shin.r and Foot.r

        //Torso
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Torso], Colliders[(int)bodyParts.Arml]); //Torso and Arm.l
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Torso], Colliders[(int)bodyParts.Armr]); //Torso and Arm.r
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Torso], Colliders[(int)bodyParts.Neck]); //Torso and Neck
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Torso], Colliders[(int)bodyParts.Head]); //Torso and Head
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Torso], Colliders[(int)bodyParts.Forearml]); //Torso and Forearm.l
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Torso], Colliders[(int)bodyParts.Forearmr]); //Torso and Forearm.r

        //Left arm
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Arml], Colliders[(int)bodyParts.Forearml]); //Arm.l and Forearm.l
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Forearml], Colliders[(int)bodyParts.Handl]); //Forearm.l and Hand.l

        //Right arm
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Armr], Colliders[(int)bodyParts.Forearmr]); //Arm.r and Forearm.r
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Forearmr], Colliders[(int)bodyParts.Handr]); //Forearm.r and Hand.r

        //Neck
        Physics.IgnoreCollision(Colliders[(int)bodyParts.Neck], Colliders[(int)bodyParts.Head]); //Neck and Head
    }

    enum bodyParts
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