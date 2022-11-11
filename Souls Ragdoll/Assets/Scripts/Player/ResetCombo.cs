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

            if (!playerManager.attackManager.chainedAttack) //Reset combo if player has not chained an attack
            {
                playerManager.attackManager.nextComboAttackIndex = 0;
                //Enable arms collision
                playerManager.ragdollManager.ToggleCollisionOfArms(true);
            }
            else
            {
                //Continue attacking
                playerManager.isAttacking = true;
                playerManager.playerIsStuckInAnimation = true;
            }

            //Reset
            playerManager.attackManager.chainedAttack = false; //Set chaining to false so the player has to press again
        }

    }
}
