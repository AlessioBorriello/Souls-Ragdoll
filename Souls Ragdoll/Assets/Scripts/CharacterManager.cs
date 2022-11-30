using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{
    public class CharacterManager : NetworkBehaviour
    {
        public Transform lockOnTargetTransform;
        public Transform backstabbedTransform;
        public Transform ripostedTransform;
    }
}
