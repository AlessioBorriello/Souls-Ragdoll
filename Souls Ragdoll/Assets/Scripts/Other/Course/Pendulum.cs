using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{
    public class Pendulum : NetworkBehaviour
    {
        private Rigidbody rb;
        [SerializeField] float maxAngleDeflection = 45f;
        [SerializeField] float phaseStart = 0f;
        [SerializeField] float speed = 2f;

        public float elapsedTime;
        private Vector3 startingEuler;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            elapsedTime = phaseStart;

            startingEuler = rb.transform.eulerAngles;
        }

        public override void OnNetworkSpawn()
        {
            SetElapsedTimeServerRpc();
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
}