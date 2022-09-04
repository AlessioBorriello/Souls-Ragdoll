using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnablePlayerInteraction : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("DisablePlayerInteraction", false);
    }
}
