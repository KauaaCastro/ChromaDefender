using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class InputManager
{
    private PlayerControls playerControls;

    public event Action OnJump;

    public float Movement => playerControls.Gameplay.Movement.ReadValue<float>();
    
    public InputManager()
    {
        playerControls = new PlayerControls();
        playerControls.Gameplay.Enable();

        playerControls.Gameplay.Jump.performed += OnJumpPerformed;
    }

    private void OnJumpPerformed (InputAction.CallbackContext context)
    {
        OnJump?.Invoke();
    }
}
