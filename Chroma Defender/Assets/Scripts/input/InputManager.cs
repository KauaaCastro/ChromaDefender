using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class InputManager
{
    private PlayerControls playerControls;

    private InputAction lightAttackAction;
    private InputAction heavyAttackAction;
    private InputAction specialAction;
    private InputAction rollAction;
    private InputAction parryAction;
    private InputAction switchPolarityAction;
    public event Action OnJump;
    public event Action OnLightAttack;
    public event Action OnHeavyAttack;
    public event Action OnSpecialAttack;
    public event Action OnRoll;
    public event Action OnParryStarted;
    public event Action OnParryCanceled;
    public event Action OnSwitchPolarity;

    public float Movement => playerControls.Gameplay.Movement.ReadValue<float>();
    
    public InputManager()
    {
        playerControls = new PlayerControls();
        playerControls.Gameplay.Enable();
        playerControls.Gameplay.Jump.performed += OnJumpPerformed;

        // Ataque Leve: Botão Esquerdo do Mouse
        lightAttackAction = new InputAction("LightAttack", InputActionType.Button, "<Mouse>/leftButton");
        lightAttackAction.performed += _ => OnLightAttack?.Invoke();
        lightAttackAction.Enable();

        // Ataque Pesado: Botão Direito do Mouse
        heavyAttackAction = new InputAction("HeavyAttack", InputActionType.Button, "<Mouse>/rightButton");
        heavyAttackAction.performed += _ => OnHeavyAttack?.Invoke();
        heavyAttackAction.Enable();

        // Especial (Weapon Art): Tecla F ou E
        specialAction = new InputAction("SpecialAttack", InputActionType.Button);
        specialAction.AddBinding("<Keyboard>/f");
        specialAction.AddBinding("<Keyboard>/e");
        specialAction.performed += _ => OnSpecialAttack?.Invoke();
        specialAction.Enable();

        // Rolar / Esquiva: Tecla K ou Left Shift
        rollAction = new InputAction("Roll", InputActionType.Button);
        rollAction.AddBinding("<Keyboard>/k");
        rollAction.AddBinding("<Keyboard>/leftShift");
        rollAction.performed += _ => OnRoll?.Invoke();
        rollAction.Enable();

        // Bloqueio / Parry: Tecla Q ou Botão do Meio do Mouse
        parryAction = new InputAction("Parry", InputActionType.Button);
        parryAction.AddBinding("<Keyboard>/q");
        parryAction.AddBinding("<Mouse>/middleButton");
        parryAction.started += _ => OnParryStarted?.Invoke();
        parryAction.canceled += _ => OnParryCanceled?.Invoke();
        parryAction.Enable();

        // Troca de Polaridade (Fogo / Gelo): Tecla Tab ou Tecla C
        switchPolarityAction = new InputAction("SwitchPolarity", InputActionType.Button);
        switchPolarityAction.AddBinding("<Keyboard>/tab");
        switchPolarityAction.AddBinding("<Keyboard>/c");
        switchPolarityAction.performed += _ => OnSwitchPolarity?.Invoke();
        switchPolarityAction.Enable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        OnJump?.Invoke();
    }
}
