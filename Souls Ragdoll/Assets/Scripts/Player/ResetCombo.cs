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
            playerManager.GetCombatManager().GetWeaponManager().ResetCombo();

            //Close hitbox to be opened again in animation
            DamageColliderControl hitbox = playerManager.GetInventoryManager().GetCurrentItemDamageColliderControl(playerManager.GetWeaponManager().IsAttackingWithLeft());
            hitbox.ToggleCollider(false);
        }

    }
}
