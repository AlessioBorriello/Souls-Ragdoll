using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Cannon : NetworkBehaviour
{
    [SerializeField] private Transform fireTransform;
    [SerializeField] private Transform cylinderTransform;
    [SerializeField] private GameObject cannonBall;
    [SerializeField] private float strenght = 10f;
    [SerializeField] private float reloadTime = 3f;
    [SerializeField] private bool cannonEnabled = true;

    private float elapsedTime = 0;

    public override void OnNetworkSpawn()
    {
        SetElapsedTimeServerRpc();
    }

    private void Update()
    {
        if (!cannonEnabled) return;

        elapsedTime += Time.deltaTime;
        if (elapsedTime >= reloadTime)
        {
            Shoot();
            elapsedTime = 0;
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

    private void Shoot()
    {
        Vector3 shotDirection = cylinderTransform.up;
        GameObject b = Instantiate(cannonBall, fireTransform.position, Quaternion.identity);
        b.GetComponent<Rigidbody>().AddForce(shotDirection * strenght, ForceMode.VelocityChange);
    }

}
