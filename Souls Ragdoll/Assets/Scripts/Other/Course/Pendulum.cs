using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pendulum : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] float maxAngleDeflection = 45f;
    [SerializeField] float phaseStart = 0f;
    [SerializeField] float speed = 2f;

    private float elapsedTime;
    private Vector3 startingEuler;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        elapsedTime = phaseStart;

        startingEuler = rb.transform.eulerAngles;
    }

    void FixedUpdate()
    {
        Swing();
    }

    private void Swing()
    {
        elapsedTime += Time.deltaTime;
        float angle = maxAngleDeflection * Mathf.Sin(elapsedTime * speed);
        rb.MoveRotation(Quaternion.Euler(startingEuler.x + angle, startingEuler.y, startingEuler.z));
    }
}
