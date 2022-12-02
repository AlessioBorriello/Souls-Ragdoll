using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class DisableIFrames : StateMachineBehaviour
    {
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            PlayerManager playerManager = animator.transform.root.GetComponent<PlayerManager>();
            playerManager.areIFramesActive = false;
        }
    }
}