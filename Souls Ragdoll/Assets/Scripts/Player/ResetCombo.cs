using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class ResetCombo : StateMachineBehaviour
    {

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            PlayerAttackManager playerAttackManager = animator.transform.root.GetComponent<PlayerAttackManager>();
            if (!playerAttackManager.chainedAttack) playerAttackManager.nextComboAttackIndex = 0;
            playerAttackManager.chainedAttack = false;
        }

    }
}
