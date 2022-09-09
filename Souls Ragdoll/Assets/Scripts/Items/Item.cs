using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    //Base class for all items
    public class Item : ScriptableObject
    {
        public string nameString;
        public Sprite iconSprite;
        public string descriptionText;
    }
}
