using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Catapult : NetworkBehaviour
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
    private bool retracting = false;
    private bool hasWaitedTime = false;

    private float elapsedTime = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pivotStartRotation = rb.rotation;
        pivotEndRotation = GetPivotEndRotation();

        float distance = Quaternion.Angle(pivotStartRotation, pivotEndRotation);
        launchSpeed = distance / launchTime;
        retractSpeed = distance / retractTime;
    }

    public override void OnNetworkSpawn()
    {
        SetElapsedTimeServerRpc();
    }

    private void Update()
    {
        if (retracting) return;

        elapsedTime += Time.deltaTime;
        if(elapsedTime >= waitTime)
        {
            hasWaitedTime = true;
        }
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
                retracting = false;
            }
            else //Hasn't reached the end
            {
                launching = true;
                Launch();
            }
        }
        else //If retracting
        {
            if (Equals(rb.rotation, pivotStartRotation)) //When it has reached the start
            {
                launching = true; //Start launch
                retracting = false;
                hasWaitedTime = false; //Hasn't waited the time
                elapsedTime = 0;
            }
            else //Hasn't reached the start
            {
                retracting = true;
                Retract();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetElapsedTimeServerRpc()
    {
        SetElapsedTimeClientRpc(elapsedTime);
    }

    [ClientRpc]
    private void SetElapsedTimeClientRpc(float time)
    {
        elapsedTime = time;
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
