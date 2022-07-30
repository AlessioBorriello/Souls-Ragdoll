using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Catapult : MonoBehaviour
{
    private Rigidbody rb;
    private Quaternion pivotStartRotation;
    private Quaternion pivotEndRotation;

    [SerializeField] private float launchTime = 1f;
    [SerializeField] private float retractTime = 4f;
    [SerializeField] private float waitTime = 6f;

    private float launchSpeed;
    private float retractSpeed;
    private bool launching = true;
    private bool hasWaitedTime = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pivotStartRotation = rb.rotation;
        pivotEndRotation = GetPivotEndRotation();

        float distance = Quaternion.Angle(pivotStartRotation, pivotEndRotation);
        launchSpeed = distance / launchTime;
        retractSpeed = distance / retractTime;

        StartCoroutine(WaitBeforeStarting(waitTime)); //Start coroutine to wait the time
    }

    void FixedUpdate()
    {
        if (!hasWaitedTime) //If it hasnt waited the waitTime
        {
            return; //Do not continue
        }

        if (launching) //If launching
        {
            if(Equals(rb.rotation, pivotEndRotation)) //When it has reached the end
            {
                launching = false; //Not launching anymore
                hasWaitedTime = false; //Hasn't waited the time
                StartCoroutine(WaitBeforeStarting(waitTime)); //Start coroutine to wait the time
            }
            else //Hasn't reached the end
            {
                Launch();
            }
        }
        else //If retracting
        {
            if (Equals(rb.rotation, pivotStartRotation)) //When it has reached the start
            {
                launching = true; //Start launch
                hasWaitedTime = false; //Hasn't waited the time
                StartCoroutine(WaitBeforeStarting(waitTime/3)); //Start coroutine to wait the time
            }
            else //Hasn't reached the start
            {
                Retract();
            }
        }
    }

    IEnumerator WaitBeforeStarting(float delay)
    {
        yield return new WaitForSeconds(delay); //Wait for the delay
        hasWaitedTime = true;
    }

    private void Launch()
    {
        rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, pivotEndRotation, launchSpeed * Time.deltaTime));
    }

    private void Retract()
    {
        rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, pivotStartRotation, retractSpeed * Time.deltaTime));
    }

    private bool Equals(Quaternion rotationOne, Quaternion rotationTwo)
    {
        return rotationOne == rotationTwo;
    }

    private Quaternion GetPivotEndRotation()
    {
        Quaternion endRotation = pivotStartRotation;
        Vector3 rotationEulers = endRotation.eulerAngles;
        endRotation = Quaternion.Euler(new Vector3(rotationEulers.x + 60f, rotationEulers.y, rotationEulers.z));
        return endRotation;
    }

}
