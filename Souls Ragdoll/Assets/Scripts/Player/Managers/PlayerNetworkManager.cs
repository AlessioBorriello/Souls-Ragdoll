using System.Collections;
using System.Collections.Generic;
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

        //private NetworkVariable<Vector3> netPosition = new(writePerm: NetworkVariableWritePermission.Owner);
        //private NetworkVariable<Quaternion> netRotation = new(writePerm: NetworkVariableWritePermission.Owner);

        private NetworkVariable<float> netNormalMovementAmount = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> netStrafeMovementAmount = new(writePerm: NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            inputManager = playerManager.GetInputManager();
            animationManager = playerManager.GetAnimationManager();
            animator = animationManager.GetAnimator();

            physicalHips = playerManager.GetPhysicalHips();
            animatedPlayer = playerManager.GetAnimatedPlayer();
        }

        void Update()
        {
            if(IsOwner)
            {
                //Read
                //netPosition.Value = physicalHips.transform.position;
                //netRotation.Value = animatedPlayer.transform.rotation;

                netNormalMovementAmount.Value = animator.GetFloat("NormalMovementAmount");
                netStrafeMovementAmount.Value = animator.GetFloat("StrafeMovementAmount");
            }
            else
            {
                //Write
                //physicalHips.transform.position = netPosition.Value;
                //animatedPlayer.transform.rotation = netRotation.Value;

                animationManager.UpdateMovementAnimatorValues(netNormalMovementAmount.Value, netStrafeMovementAmount.Value, 0);
            }
        }
    }
}