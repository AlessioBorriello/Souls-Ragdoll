using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlessioBorriello
{
    public class InputManager : MonoBehaviour
    {

        //Note, inputs variables ending in "In" are local and private
        //Thos ending in "Input" are instead those readable from the outside

        PlayerControls inputAction;

        private Vector2 movementIn;
        private Vector2 cameraIn;
        private bool eastIn;

        public void OnEnable()
        {
            if(inputAction == null)
            {

                inputAction = new PlayerControls();
                inputAction.PlayerGameplay.Movement.performed += inputAction => movementIn = inputAction.ReadValue<Vector2>();
                inputAction.PlayerGameplay.CameraMovement.performed += inputAction => cameraIn = inputAction.ReadValue<Vector2>();

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
        public void TickActionsInput()
        {

            eastIn = inputAction.PlayerGameplay.Roll.phase == UnityEngine.InputSystem.InputActionPhase.Started; //When the key is pressed
            eastInput = eastIn;
        }
    }
}
