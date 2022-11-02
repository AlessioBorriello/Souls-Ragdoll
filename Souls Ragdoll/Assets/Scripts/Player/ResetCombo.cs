using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class ResetCombo : StateMachineBehaviour
    {

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            PlayerManager playerManager = animator.transform.root.GetComponent<PlayerManager>();

            if (!playerManager.attackManager.chainedAttack) playerManager.attackManager.nextComboAttackIndex = 0; //Reset combo if player has not chained an attack
            else
            {
                //Continue attacking
                playerManager.isAttacking = true;
                playerManager.disablePlayerInteraction = true;
            }

            //Reset
            playerManager.attackManager.chainedAttack = false; //Set chaining to false so the player has to press again
            playerManager.attackManager.canCombo = false;
        }

    }
}
