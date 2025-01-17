using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour {
    public GameObject head;
    public float sensivity = 0.025f;
    private EntityMovement movement;
    private Vector2 lookDirection;

    private void Start() {
        movement = GetComponent<EntityMovement>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Move(InputAction.CallbackContext context) {
        movement.QueueMove(context.ReadValue<Vector2>());
    }

    public void Look(InputAction.CallbackContext context) {
        lookDirection += context.ReadValue<Vector2>() * sensivity;
        movement.QueueSetRotate(lookDirection.x);
    }

    public void Jump(InputAction.CallbackContext context) {
        movement.QueueJump();
    }

    public void Sprint(InputAction.CallbackContext context) {
        movement.ToggleSprint(context.performed);
    }

    public void Crouch(InputAction.CallbackContext context) {
        movement.ToggleCrouch(context.performed);
    }

    private void Update() {
        head.transform.localRotation = Quaternion.Euler(-lookDirection.y, 0f, 0f);
    }
}
