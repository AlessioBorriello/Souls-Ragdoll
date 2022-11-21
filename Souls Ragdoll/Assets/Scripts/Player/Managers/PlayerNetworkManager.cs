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
        private InputManager inputManager;

        private Rigidbody physicalHips;
        private GameObject animatedPlayer;
        private AnimationManager animationManager;
        private Animator animator;

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
            inputManager = playerManager.GetInputManager();
            animationManager = playerManager.GetAnimationManager();
            animator = animationManager.GetAnimator();

            physicalHips = playerManager.GetPhysicalHips();
            animatedPlayer = playerManager.GetAnimatedPlayer();
        }

        public override void OnNetworkSpawn()
        {
            netIsAttackingWithLeft.OnValueChanged += (bool previousValue, bool newValue) => animationManager.UpdateAttackingWithLeftValue(newValue);
            netIsBlockingWithLeft.OnValueChanged += (bool previousValue, bool newValue) => animationManager.UpdateBlockingWithLeftValue(newValue);
        }

        void Update()
        {
            if (playerManager.isDead) return;

            if (IsOwner)
            {
                netPosition.Value = physicalHips.transform.position;
                netRotation.Value = animatedPlayer.transform.rotation;
            }
            else
            {
                physicalHips.transform.position = netPosition.Value;
                animatedPlayer.transform.rotation = netRotation.Value;
            }

            animationManager.UpdateMovementAnimatorValues(netNormalMovementAmount.Value, netStrafeMovementAmount.Value, 0);
        }
    
    }
}