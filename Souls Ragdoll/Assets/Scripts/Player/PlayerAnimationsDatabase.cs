using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Animancer.Validate;

namespace AlessioBorriello
{

    public class PlayerAnimationsDatabase : MonoBehaviour
    {

        [SerializeField] private List<ClipTransition> playerActionsAnimations = new List<ClipTransition>();
        [SerializeField] private List<ClipTransition> playerStaggerAnimations = new List<ClipTransition>();

        //Dictionary of animations
        private Dictionary<string, ClipTransition> animationsDictionary = new Dictionary<string, ClipTransition>();

        private void Awake()
        {
            //Add action animations to dictionary
            foreach(ClipTransition animation in playerActionsAnimations)
            {
                string name = animation.Name;
                animationsDictionary.Add(name, animation);
            }

            //Add attack animations to dictionary
            foreach (ClipTransition animation in playerStaggerAnimations)
            {
                string name = animation.Name;
                animationsDictionary.Add(name, animation);
            }
        }

        public ClipTransition GetClipTransition(string animationName)
        {
            //Debug.Log(animationName);
            if (!animationsDictionary.TryGetValue(animationName, out ClipTransition animation)) 
            {
                Debug.Log($"Animation {animationName} not found!");
                return null;
            }
            else return animation;
        
        }
    }
}
