using AlessioBorriello;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Lift : NetworkBehaviour
{
    [SerializeField] private Transform endTransform;
    private Rigidbody liftRigidBody;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private Vector3 endPosition;
    private Quaternion endRotation;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    [SerializeField] private float travelTime;
    [SerializeField] private float waitTime;

    private float elapsedTime = 0;
    private bool hasWaitedTime = false;
    private bool hasArrived = false;

    private float positionDistance;
    private float positionSpeed;
    private float rotationDistance;
    private float rotationSpeed;

    private Vector3 deltaPosition;
    private Vector3 lastPosition;

    private Vector3 deltaRotation;
    private Vector3 lastRotation;

    private void Awake()
    {
        liftRigidBody = GetComponent<Rigidbody>();

        startPosition = liftRigidBody.position;
        startRotation = liftRigidBody.rotation;

        //To calculate delta movement
        lastPosition = liftRigidBody.position;
        lastRotation = liftRigidBody.rotation.eulerAngles;


        endPosition = endTransform.position;
        endRotation = endTransform.rotation;

        targetPosition = endPosition;
        targetRotation = endRotation;

        positionDistance = Vector3.Distance(startPosition, endPosition);
        rotationDistance = Quaternion.Angle(startRotation, endRotation);

        positionSpeed = positionDistance / travelTime;
        rotationSpeed = rotationDistance / travelTime;
    }

    public override void OnNetworkSpawn()
    {
        SyncLiftServerRpc();
    }

    private void Update()
    {

        elapsedTime += Time.deltaTime;
        if (elapsedTime >= waitTime)
        {
            hasWaitedTime = true;
            elapsedTime = 0;
        }

        if (hasWaitedTime && !hasArrived) elapsedTime = 0;
    }

    private void FixedUpdate()
    {
        hasArrived = HasLiftArrivedAtTarget();

        if (hasArrived)
        {
            //Change the targets
            targetPosition = ChangeTargetPosition();
            targetRotation = ChangeTargetRotation();
            hasWaitedTime = false;
        }
        else
        {
            if (hasWaitedTime)
            {
                MoveLift();
                elapsedTime = 0;
            }
        }

        deltaPosition = liftRigidBody.position - lastPosition;
        deltaPosition.y = 0;
        lastPosition = liftRigidBody.position;

        deltaRotation = liftRigidBody.rotation.eulerAngles - lastRotation;
        lastRotation = liftRigidBody.rotation.eulerAngles;
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            PlayerManager passenger = other.GetComponentInParent<PlayerManager>();
            if (passenger == null) return;

            if (!passenger.IsOwner) return;

            passenger.GetPhysicalHips().position += deltaPosition;

            passenger.GetAnimatedPlayer().transform.Rotate(deltaRotation / 10);
        }    
    }

    private void MoveLift()
    {
        //Move lift
        MoveToTarget(targetPosition);
        RotateToTarget(targetRotation);
    }

    private Vector3 ChangeTargetPosition()
    {
        if(targetPosition == endPosition) //If the target was the end
        {
            return startPosition;
        }
        else //If the target was the start
        {
            return endPosition;
        }
    }

    private Quaternion ChangeTargetRotation()
    {
        if (targetRotation == endRotation) //If the target was the end
        {
            return startRotation;
        }
        else //If the target was the start
        {
            return endRotation;
        }
    }

    private bool Equals(Vector3 positionOne, Vector3 positionTwo, Quaternion rotationOne, Quaternion rotationTwo)
    {
        bool samePosition = positionOne == positionTwo; //Check position
        bool sameRotation = rotationOne == rotationTwo; //Check rotation
        return samePosition && sameRotation;
    }

    private bool HasLiftArrivedAtTarget()
    {
        if(Equals(liftRigidBody.position, targetPosition, liftRigidBody.rotation, targetRotation))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void MoveToTarget(Vector3 targetPosition)
    {
        Vector3 newPosition = Vector3.MoveTowards(liftRigidBody.position, targetPosition, positionSpeed * Time.fixedDeltaTime);

        //Move
        liftRigidBody.MovePosition(newPosition);
    }

    private void RotateToTarget(Quaternion targetRotation)
    {
        liftRigidBody.MoveRotation(Quaternion.RotateTowards(liftRigidBody.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
    }

    [ServerRpc(RequireOwnership = false)]
    private void SyncLiftServerRpc()
    {
        SyncLiftClientRpc(elapsedTime, hasWaitedTime, liftRigidBody.transform.position, liftRigidBody.transform.rotation, targetPosition, targetRotation);
    }

    [ClientRpc]
    private void SyncLiftClientRpc(float elapsedTime, bool hasWaitedTime, Vector3 position, Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation)
    {
        //Sync up
        this.elapsedTime = elapsedTime;
        this.hasWaitedTime = hasWaitedTime;
        this.liftRigidBody.transform.position = position;
        this.liftRigidBody.transform.rotation = rotation;
        this.targetPosition = targetPosition;
        this.targetRotation = targetRotation;
    }

}
