using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlessioBorriello
{

    public class InputManager : MonoBehaviour
    {

        //Note, inputs variables ending in "In" are local and private
        //Thos ending in "Input" are instead those readable from the outside

        private PlayerManager playerManager;

        PlayerControls inputAction;

        private Vector2 movementIn;
        private Vector2 cameraIn;

        //Actions
        private bool eastIn;
        private bool eastInReleased;

        public void OnEnable()
        {

            playerManager = GetComponent<PlayerManager>();

            if (inputAction == null)
            {

                inputAction = new PlayerControls();
                //Movement
                inputAction.PlayerGameplay.Movement.performed += inputAction => movementIn = inputAction.ReadValue<Vector2>();

                //Camera movement
                inputAction.PlayerGameplay.CameraMovement.performed += inputAction => cameraIn = inputAction.ReadValue<Vector2>();

                //Actions
                #region East button
                inputAction.PlayerGameplay.EastButton.started += inputAction => eastIn = true;
                inputAction.PlayerGameplay.EastButton.canceled += inputAction =>
                {
                    eastIn = false;
                    eastInReleased = true;
                };
                #endregion

            }


            inputAction.Enable();

        }

        private void OnDisable()
        {
            inputAction.Disable();
        }

        public Vector2 movementInput;
        public void TickMovementInput()
        {
            movementInput = movementIn;
        }

        public Vector2 cameraInput;
        public void TickCameraMovementInput()
        {
            cameraInput = cameraIn;
        }

        public bool eastInput;
        public bool eastInputReleased;
        public void TickActionsInput()
        {
            eastInput = eastIn;
            eastInputReleased = eastInReleased;
        }

        private void LateUpdate()
        {
            eastInReleased = false;
        }

    }

}
