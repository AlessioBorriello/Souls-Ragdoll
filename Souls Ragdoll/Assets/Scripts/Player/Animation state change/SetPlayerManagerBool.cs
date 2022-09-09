using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class SetPlayerManagerBool : StateMachineBehaviour
    {
        [Header("On enter state")]
        public bool onEnter = false;
        public string onEnterBoolName;
        public bool onEnterBoolValue;

        [Header("On exit state")]
        public bool onExit = false;
        public string onExitBoolName;
        public bool onExitBoolValue;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!onEnter) return;

            PlayerManager pm = animator.GetComponentInParent<PlayerManager>();
            typeof(PlayerManager).GetField(onEnterBoolName).SetValue(pm, onEnterBoolValue);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!onExit) return;

            PlayerManager pm = animator.GetComponentInParent<PlayerManager>();
            typeof(PlayerManager).GetField(onExitBoolName).SetValue(pm, onExitBoolValue);
        }

    }
}
