using AlessioBorriello;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class EnableArmsCollisionOnEnter : StateMachineBehaviour
    {
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            PlayerManager playerManager = animator.transform.root.GetComponent<PlayerManager>();

            //Enable arms collision
            playerManager.GetRagdollManager().ToggleCollisionOfArms(true);
            playerManager.GetRagdollManager().ToggleCollisionOfArmsServerRpc(true);
        }
    }
}