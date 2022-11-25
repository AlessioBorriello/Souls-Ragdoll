using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerNetworkManager : NetworkBehaviour
    {
        private PlayerManager playerManager;

        private Rigidbody physicalHips;
        private GameObject animatedPlayer;
        private AnimationManager animationManager;

        private NetworkVariable<Vector3> netPosition = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> netRotation = new(writePerm: NetworkVariableWritePermission.Owner);

        public NetworkVariable<float> netNormalMovementAmount = new(0, writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<float> netStrafeMovementAmount = new(0, writePerm: NetworkVariableWritePermission.Owner);

        public NetworkVariable<bool> netIsAttackingWithLeft = new(false, writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> netIsBlockingWithLeft = new(false, writePerm: NetworkVariableWritePermission.Owner);

        //Stats
        public NetworkVariable<int> netCurrentHealth = new(0, writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<int> netMaxHealth = new(0, writePerm: NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            animationManager = playerManager.GetAnimationManager();

            physicalHips = playerManager.GetPhysicalHips();
            animatedPlayer = playerManager.GetAnimatedPlayer();
        }

        public override void OnNetworkSpawn()
        {
            netIsAttackingWithLeft.OnValueChanged += (bool previousValue, bool newValue) => animationManager.UpdateAttackingWithLeftValue(newValue);
            netIsBlockingWithLeft.OnValueChanged += (bool previousValue, bool newValue) => animationManager.UpdateBlockingWithLeftValue(newValue);
        }

        public override void OnDestroy()
        {
            netIsAttackingWithLeft.OnValueChanged -= (bool previousValue, bool newValue) => animationManager.UpdateAttackingWithLeftValue(newValue);
            netIsBlockingWithLeft.OnValueChanged -= (bool previousValue, bool newValue) => animationManager.UpdateBlockingWithLeftValue(newValue);
        }

        private Vector3 posVel;
        private float rotVel = 15;
        private void Update()
        {
            if (playerManager.isDead) return;

            if (IsOwner)
            {
                netPosition.Value = physicalHips.transform.position;
                netRotation.Value = animatedPlayer.transform.rotation;
            }
            else
            {
                if(Vector3.Distance(physicalHips.transform.position, netPosition.Value) > 1.2f) physicalHips.transform.position = netPosition.Value;
                else physicalHips.transform.position = Vector3.SmoothDamp(physicalHips.transform.position, netPosition.Value, ref posVel, .1f);

                animatedPlayer.transform.rotation = Quaternion.Slerp(animatedPlayer.transform.rotation, netRotation.Value, rotVel);
            }

            animationManager.UpdateMovementAnimatorValues(netNormalMovementAmount.Value, netStrafeMovementAmount.Value, 0);
        }

    }
}