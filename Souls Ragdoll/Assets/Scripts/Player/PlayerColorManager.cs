using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

namespace AlessioBorriello
{
    public class PlayerColorManager : NetworkBehaviour
    {
        [SerializeField] private List<Color> colors = new List<Color>();
        private Renderer playerRenderer;
        private NetworkVariable<Color> netColor = new(writePerm: NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            playerRenderer = GetComponent<Renderer>();
            netColor.OnValueChanged += OnValueChanged;
        }

        private void OnValueChanged(Color prev, Color next) => SetPlayerColor(next);

        public override void OnDestroy()
        {
            netColor.OnValueChanged -= OnValueChanged;
        }

        public override void OnNetworkSpawn()
        {
            if(IsOwner)
            {
                Color c = colors[Random.Range(0, colors.Count)];
                netColor.Value = c;
            }
            else
            {
                SetPlayerColor(netColor.Value);
            }
        }

        private void SetPlayerColor(Color color)
        {
            playerRenderer.material.color = color;
        }
    }
}