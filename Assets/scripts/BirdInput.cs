using UnityEngine;
using UnityEngine.InputSystem;

public class BirdInput : MonoBehaviour
{
    [HideInInspector] public Vector2 moveInput = Vector2.zero;
    [HideInInspector] public bool flapPressed = false;
    [HideInInspector] public bool boostHeld = false;

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnFlap(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
            flapPressed = true;
        if (context.canceled)
            flapPressed = false;
    }

    public void OnBoost(InputAction.CallbackContext context)
    {
        boostHeld = context.ReadValueAsButton();
    }
}
