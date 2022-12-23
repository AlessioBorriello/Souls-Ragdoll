using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlessioBorriello
{

    public class InputManager : MonoBehaviour
    {
        private class InputAction
        {
            public string name;
            public float creationTime;

            public InputAction(string name)
            {
                this.name = name;
                this.creationTime = Time.time;
            }
        }

        private PlayerManager playerManager;
        private PlayerControls controls;

        private Stack<InputAction> actions = new Stack<InputAction>();
        private float queueTime = .2f;

        //Analogs
        [Header("Analogs")]
        public Vector2 movementInput;
        public Vector2 cameraInput;

        [Header("D-Pad")]
        public Vector2 dPadInput;

        #region Sticks
        [Header("Right stick")]
        public bool rightStickInput;
        public bool rightStickInputReleased;
        public bool rightStickInputPressed;
        #endregion

        #region Buttons
        //East
        public bool eastInput;
        public bool eastInputReleased;
        public bool eastInputPressed;

        //South
        public bool southInput;
        public bool southInputReleased;
        public bool southInputPressed;

        //North
        public bool northInput;
        public bool northInputReleased;
        public bool northInputPressed;
        #endregion

        #region Triggers - Bumpers
        //Bumper - right
        public bool rbInput;
        public bool rbInputReleased;
        public bool rbInputPressed;

        //Trigger - right
        public bool rtInput;
        public bool rtInputReleased;
        public bool rtInputPressed;

        //Bumper - left
        public bool lbInput;
        public bool lbInputReleased;
        public bool lbInputPressed;

        //Trigger - left
        public bool ltInput;
        public bool ltInputReleased;
        public bool ltInputPressed;
        #endregion

        private void Awake()
        {
            playerManager = GetComponent<PlayerManager>();
            queueTime = playerManager.playerData.inputQueueTime;
        }

        private void Start()
        {
            if (!playerManager.isClient) this.enabled = false;
        }

        public void OnEnable()
        {

            if (controls == null)
            {

                controls = new PlayerControls();

                //Movement
                controls.PlayerGameplay.Movement.performed += inputAction => movementInput = inputAction.ReadValue<Vector2>();

                //Camera movement
                controls.PlayerGameplay.CameraMovement.performed += inputAction => cameraInput = inputAction.ReadValue<Vector2>().normalized;

                #region Sticks
                //Right stick
                controls.PlayerGameplay.LockOn.started += inputAction =>
                {
                    rightStickInput = true;
                    rightStickInputPressed = true;
                    //actions.Push(new InputAction("RightStickPressed"));
                };
                controls.PlayerGameplay.LockOn.canceled += inputAction =>
                {
                    rightStickInput = false;
                    rightStickInputReleased = true;
                    actions.Push(new InputAction("RightStickReleased"));
                };
                #endregion

                #region Buttons
                //East button
                controls.PlayerGameplay.EastButton.started += inputAction =>
                {
                    eastInput = true;
                    actions.Push(new InputAction("EastButtonPressed"));
                };
                controls.PlayerGameplay.EastButton.canceled += inputAction =>
                {
                    eastInput = false;
                    actions.Push(new InputAction("EastButtonReleased"));
                };

                //South button
                controls.PlayerGameplay.SouthButton.started += inputAction =>
                {
                    southInput = true;
                    actions.Push(new InputAction("SouthButtonPressed"));
                };
                controls.PlayerGameplay.SouthButton.canceled += inputAction =>
                {
                    southInput = false;
                    actions.Push(new InputAction("SouthButtonReleased"));
                };

                //North button
                controls.PlayerGameplay.NorthButton.started += inputAction =>
                {
                    northInput = true;
                    actions.Push(new InputAction("NorthButtonPressed"));
                };
                controls.PlayerGameplay.NorthButton.canceled += inputAction =>
                {
                    northInput = false;
                    actions.Push(new InputAction("NorthButtonReleased"));
                };
                #endregion

                #region Bumpers and Triggers
                //Right bumper
                controls.PlayerGameplay.RightLightButton.started += inputAction =>
                {
                    rbInput = true;
                    actions.Push(new InputAction("RightLightButtonPressed"));
                };
                controls.PlayerGameplay.RightLightButton.canceled += inputAction =>
                {
                    rbInput = false;
                    actions.Push(new InputAction("RightLightButtonReleased"));
                };

                //Left bumper
                controls.PlayerGameplay.LeftLightButton.started += inputAction =>
                {
                    lbInput = true;
                    actions.Push(new InputAction("LeftLightButtonPressed"));
                };
                controls.PlayerGameplay.LeftLightButton.canceled += inputAction =>
                {
                    lbInput = false;
                    actions.Push(new InputAction("LeftLightButtonReleased"));
                };

                //Right trigger
                controls.PlayerGameplay.RightHeavyButton.started += inputAction =>
                {
                    rtInput = true;
                    actions.Push(new InputAction("RightHeavyButtonPressed"));
                };
                controls.PlayerGameplay.RightHeavyButton.canceled += inputAction =>
                {
                    rtInput = false;
                    actions.Push(new InputAction("RightHeavyButtonReleased"));
                };

                //Left trigger
                controls.PlayerGameplay.LeftHeavyButton.started += inputAction =>
                {
                    ltInput = true;
                    actions.Push(new InputAction("LeftHeavyButtonPressed"));
                };
                controls.PlayerGameplay.LeftHeavyButton.canceled += inputAction =>
                {
                    ltInput = false;
                    actions.Push(new InputAction("LeftHeavyButtonReleased"));
                };
                #endregion

                #region Dpad
                controls.PlayerGameplay.DPadUp.started += inputAction => actions.Push(new InputAction("DPadUpPressed"));
                controls.PlayerGameplay.DPadDown.started += inputAction => actions.Push(new InputAction("DPadDownPressed"));
                controls.PlayerGameplay.DPadRight.started += inputAction => actions.Push(new InputAction("DPadLeftPressed"));
                controls.PlayerGameplay.DPadLeft.started += inputAction => actions.Push(new InputAction("DPadRightPressed"));
                #endregion

            }

            controls.Enable();

        }

        private void Update()
        {
            if (!playerManager.IsOwner) return;

            if (actions.Count > 0 && 
                !playerManager.isStuckInAnimation && 
                playerManager.consumeInputs &&
                !playerManager.disableActions) ConsumeInputs(actions);
        }

        private void LateUpdate()
        {
            //ResetAllInputValues();
        }

        private void OnDisable()
        {
            controls.Disable();
        }

        private void ConsumeInputs(Stack<InputAction> actions)
        {
            //While there are still actions to process
            while (actions.Any())
            {
                //Get action
                InputAction action = actions.Pop();
                //If the action is not too old, process it
                if (action.creationTime > Time.time - queueTime) ProcessInput(action.name);
            }
        }

        private void ProcessInput(string inputName)
        {
            switch (inputName)
            {
                #region Sticks
                //Right stick
                case "RightStickPressed": rightStickInputPressed = true; break;
                case "RightStickReleased": rightStickInputReleased = true; break;
                #endregion

                #region Buttons
                //East button
                case "EastButtonPressed": eastInputPressed = true; break;
                case "EastButtonReleased": eastInputReleased = true; break;

                //South button
                case "SouthButtonPressed": southInputPressed = true; break;
                case "SouthButtonReleased": southInputReleased = true; break;

                //North button
                case "NorthButtonPressed": northInputPressed = true; break;
                case "NorthButtonReleased": northInputReleased = true; break;
                #endregion

                #region Bumpers and Triggers
                //Right bumper
                case "RightLightButtonPressed": rbInputPressed = true; break;
                case "RightLightButtonReleased": rbInputReleased = true; break;

                //Left bumper
                case "LeftLightButtonPressed": lbInputPressed = true; break;
                case "LeftLightButtonReleased": lbInputReleased = true; break;

                //Right trigger
                case "RightHeavyButtonPressed": rtInputPressed = true; break;
                case "RightHeavyButtonReleased": rtInputReleased = true; break;

                //Left trigger
                case "LeftHeavyButtonPressed": ltInputPressed = true; break;
                case "LeftHeavyButtonReleased": ltInputReleased = true; break;
                #endregion

                #region DPad()
                case "DPadUpPressed": dPadInput = new Vector2(0, -1); break;
                case "DPadDownPressed": dPadInput = new Vector2(0, 1); break;
                case "DPadRightPressed": dPadInput = new Vector2(-1, 0); break;
                case "DPadLeftPressed": dPadInput = new Vector2(1, 0); break;
                #endregion

                default: break;
            }
        }

        public void ResetAllInputValues()
        {
            #region Sticks
            rightStickInputPressed = false;
            rightStickInputReleased = false;
            #endregion

            #region Buttons
            //East button
            eastInputPressed = false;
            eastInputReleased = false;

            //South button
            southInputPressed = false;
            southInputReleased = false;
            #endregion

            #region Bumpers and Triggers
            //Right bumper
            rbInputPressed = false;
            rbInputReleased = false;

            //Left bumper
            lbInputPressed = false;
            lbInputReleased = false;

            //Right trigger
            rtInputPressed = false;
            rtInputReleased = false;

            //Left trigger
            ltInputPressed = false;
            ltInputReleased = false;
            #endregion

            #region DPad
            dPadInput = Vector2.zero;
            #endregion
        }
    
        private IEnumerator SetValueAtNextFrame<T>(Action<T> action, T newValue)
        {
            //Use: StartCoroutine(SetValueAtNextFrame<int>(newValue => var = newValue, 99)); (99 is the new value)
            yield return null;
            action(newValue);
        }

    }
}