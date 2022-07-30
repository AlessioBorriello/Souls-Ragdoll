using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lift : MonoBehaviour
{
    [SerializeField] private Transform endTransform;
    private Rigidbody liftRigidBody;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private Vector3 endPosition;
    private Quaternion endRotation;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    [SerializeField] private float time;
    [SerializeField] private float waitTime;
    private bool hasWaitedTime = false;

    private float positionDistance;
    private float positionSpeed;
    private float rotationDistance;
    private float rotationSpeed;

    private void Awake()
    {
        liftRigidBody = GetComponent<Rigidbody>();

        startPosition = liftRigidBody.position;
        startRotation = liftRigidBody.rotation;

        endPosition = endTransform.position;
        endRotation = endTransform.rotation;

        targetPosition = endPosition;
        targetRotation = endRotation;

        positionDistance = Vector3.Distance(startPosition, endPosition);
        rotationDistance = Quaternion.Angle(startRotation, endRotation);

        positionSpeed = positionDistance / time;
        rotationSpeed = rotationDistance / time;
    }

    private void FixedUpdate()
    {
        if (!LiftArrivedAtTarget() && hasWaitedTime) //Lift is not at the target and has waited the wait time
        {
            MoveLift();
        }
        else //Lift arrived
        {
            StartCoroutine(WaitBeforeStarting(waitTime)); //Wait
            //Change targets
            targetPosition = ChangeTargetPosition();
            targetRotation = ChangeTargetRotation();
        }
    }

    private void MoveLift()
    {
        //Move lift
        MoveToTarget(targetPosition);
        RotateToTarget(targetRotation);
    }

    IEnumerator WaitBeforeStarting(float delay)
    {
        yield return new WaitForSeconds(delay); //Wait for the delay
        hasWaitedTime = true;
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

    private bool LiftArrivedAtTarget()
    {
        if(Equals(liftRigidBody.position, targetPosition, liftRigidBody.rotation, targetRotation))
        {
            hasWaitedTime = false;
            return true;
        }
        else
        {
            return false;
        }
    }

    private void MoveToTarget(Vector3 targetPosition)
    {
        liftRigidBody.MovePosition(Vector3.MoveTowards(liftRigidBody.position, targetPosition, positionSpeed * Time.deltaTime));
    }

    private void RotateToTarget(Quaternion targetRotation)
    {
        liftRigidBody.MoveRotation(Quaternion.RotateTowards(liftRigidBody.rotation, targetRotation, rotationSpeed * Time.deltaTime));
    }

}
