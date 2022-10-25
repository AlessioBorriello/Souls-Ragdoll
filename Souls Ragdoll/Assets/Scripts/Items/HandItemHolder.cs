using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class HandItemHolder : MonoBehaviour
    {

        public bool isLeftHand;

        public GameObject currentItemModel;

        public void UnloadItemModel()
        {
            if(currentItemModel != null) currentItemModel.SetActive(false);
        }

        public void DestroyItemModel()
        {
            if (currentItemModel != null) Destroy(currentItemModel);
        }

        public void LoadItemModel(HandEquippableItem item)
        {

            DestroyItemModel();

            if (item == null)
            {
                UnloadItemModel();
                return;
            }

            GameObject model = Instantiate(item.modelPrefab) as GameObject;
            if(model != null)
            {
                model.transform.parent = transform;
            }

            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            currentItemModel = model;

        }

    }
}