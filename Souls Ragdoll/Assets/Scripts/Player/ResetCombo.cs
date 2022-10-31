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

            if (!playerAttackManager.chainedAttack) playerAttackManager.nextComboAttackIndex = 0; //Reset combo if player has not chained an attack
            playerAttackManager.chainedAttack = false; //Set chaining to false so the player has to press again
            playerAttackManager.canCombo = false;
        }

    }
}
