using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    [SerializeField] private Transform fireTransform;
    [SerializeField] private Transform cylinderTransform;
    [SerializeField] private GameObject cannonBall;
    [SerializeField] private float strenght = 10f;
    [SerializeField] private float reloadTime = 3f;
    [SerializeField] private float startOffsetTime = 0f;

    private void Awake()
    {
        StartCoroutine(Reload(reloadTime + startOffsetTime));
    }

    IEnumerator Reload(float reloadTime)
    {
        yield return new WaitForSeconds(reloadTime); //Wait for the reload
        Shoot();
        StartCoroutine(Reload(reloadTime)); //Fire again
    }

    private void Shoot()
    {
        Vector3 shotDirection = cylinderTransform.up;
        GameObject b = Instantiate(cannonBall, fireTransform.position, Quaternion.identity);
        b.GetComponent<Rigidbody>().AddForce(shotDirection * strenght, ForceMode.VelocityChange);
    }

}
