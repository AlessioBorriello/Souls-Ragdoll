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
        private NetworkVariable<float> netElapsedTime = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private Vector3 startingEuler;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            elapsedTime = phaseStart;

            startingEuler = rb.transform.eulerAngles;
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                netElapsedTime.Value = elapsedTime;
            }
            else
            {
                elapsedTime = netElapsedTime.Value;
            }
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