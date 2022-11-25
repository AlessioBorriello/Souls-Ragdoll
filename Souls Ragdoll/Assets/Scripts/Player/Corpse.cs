using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class Corpse : MonoBehaviour
    {
        [SerializeField] Rigidbody hip;
        [SerializeField] float despawnTime = 20f;

        private Renderer corpseRenderer;
        private Rigidbody[] corpseParts;
        private Color color;

        private void Awake()
        {
            corpseRenderer = GetComponentInChildren<Renderer>();
            corpseParts = hip.GetComponentsInChildren<Rigidbody>(); //Get all the rigid bodies
        }

        private void Start()
        {
            StartCoroutine(Despawn(despawnTime));
        }

        private IEnumerator Despawn(float time)
        {
            yield return new WaitForSeconds(time);
            Destroy(this.gameObject);
        }

        public void SetUp(Color color, Rigidbody[] bodyParts)
        {
            this.color = color;
            corpseRenderer.material.color = this.color;

            for(int i = 0; i < bodyParts.Length; i++)
            {
                corpseParts[i].transform.position = bodyParts[i].transform.position;
                corpseParts[i].transform.rotation = bodyParts[i].transform.rotation;
            }
        }
    }
}