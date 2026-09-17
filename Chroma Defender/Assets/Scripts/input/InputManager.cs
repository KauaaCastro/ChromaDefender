using UnityEngine;

public class InputManager : MonoBehaviour
{
    private PlayerControls playerControls;

    public float Movement => playerControls.Gameplay.Movement.ReadValue<float>();
    
    public InputManager()
    {
        playerControls = new PlayerControls();
        playerControls.Gameplay.Enable();
    }
}
