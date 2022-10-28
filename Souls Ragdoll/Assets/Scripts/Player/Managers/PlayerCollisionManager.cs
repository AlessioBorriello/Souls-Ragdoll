using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerCollisionManager : MonoBehaviour
    {
        public List<int> inContact = new List<int>();

        public bool EnterCollision(int colliderId)
        {

            bool firstCollision = false;
            if(!inContact.Contains(colliderId)) //If no player collider is in contact with the other collider
            {
                firstCollision = true;
            }else { 
            }

            inContact.Add(colliderId);
            return firstCollision;
        }

        public void ExitCollision(int colliderId)
        {

            if (inContact.Contains(colliderId))
            {
                inContact.Remove(colliderId);
            }

        }

    }
}
