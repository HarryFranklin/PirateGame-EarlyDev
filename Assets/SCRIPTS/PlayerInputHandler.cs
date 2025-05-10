using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset playerControls;
    
    [Header("Action Map Name Reference")]
    [SerializeField] private string actionMapName = "Player";

    [Header("Action Name Reference")]
    [SerializeField] private string move = "Move";
    [SerializeField] private string sailUp = "SailUp";
    [SerializeField] private string sailDown = "SailDown";
    [SerializeField] private string anchor = "Anchor";
    [SerializeField] private string mainAttack = "MainAttack";
    [SerializeField] private string secondAttack = "SecondAttack";
    [SerializeField] private string thirdAttack = "ThirdAttack";
    [SerializeField] private string fourthAttack = "FourthAttack";

    // Input Actions
    private InputAction moveAction;
    private InputAction sailUpAction;
    private InputAction sailDownAction;
    private InputAction anchorAction;
    private InputAction mainAttackAction;
    private InputAction secondAttackAction;
    private InputAction thirdAttackAction;
    private InputAction fourthAttackAction;

    // Public properties for reading input state
    public Vector2 MoveInput { get; private set; }
    public bool SailUpInputTrigger { get; private set; }
    public bool SailDownInputTrigger { get; private set; }
    public bool AnchorInputTrigger { get; private set; }
    public bool MainAttackInputTrigger { get; private set; }
    public bool SecondAttackInputTrigger { get; private set; }
    public bool ThirdAttackInputTrigger { get; private set; }
    public bool FourthAttackInputTrigger { get; private set; }

    // Singleton pattern
    public static PlayerInputHandler Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitialiseInputActions();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitialiseInputActions()
    {
        // Get input action map
        InputActionMap actionMap = playerControls.FindActionMap(actionMapName);
        
        if (actionMap == null)
        {
            Debug.LogError($"Action map '{actionMapName}' not found in Input Action Asset!");
            return;
        }

        // Initialise all input actions
        moveAction = actionMap.FindAction(move);
        sailUpAction = actionMap.FindAction(sailUp);
        sailDownAction = actionMap.FindAction(sailDown);
        anchorAction = actionMap.FindAction(anchor);
        mainAttackAction = actionMap.FindAction(mainAttack);
        secondAttackAction = actionMap.FindAction(secondAttack);
        thirdAttackAction = actionMap.FindAction(thirdAttack);
        fourthAttackAction = actionMap.FindAction(fourthAttack);

        // Validate all actions were found
        if (moveAction == null) Debug.LogError($"Action '{move}' not found!");
        if (sailUpAction == null) Debug.LogError($"Action '{sailUp}' not found!");
        if (sailDownAction == null) Debug.LogError($"Action '{sailDown}' not found!");
        if (anchorAction == null) Debug.LogError($"Action '{anchor}' not found!");
        if (mainAttackAction == null) Debug.LogError($"Action '{mainAttack}' not found!");
        if (secondAttackAction == null) Debug.LogError($"Action '{secondAttack}' not found!");
        if (thirdAttackAction == null) Debug.LogError($"Action '{thirdAttack}' not found!");
        if (fourthAttackAction == null) Debug.LogError($"Action '{fourthAttack}' not found!");

        // Register callbacks for all actions
        RegisterInputCallbacks();
    }

    private void RegisterInputCallbacks()
    {
        // Movement input (read as Vector2)
        if (moveAction != null)
        {
            moveAction.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
            moveAction.canceled += ctx => MoveInput = Vector2.zero;
        }

        // Sail controls (read as button presses)
        if (sailUpAction != null)
        {
            sailUpAction.performed += ctx => SailUpInputTrigger = true;
            sailUpAction.canceled += ctx => SailUpInputTrigger = false;
        }

        if (sailDownAction != null)
        {
            sailDownAction.performed += ctx => SailDownInputTrigger = true;
            sailDownAction.canceled += ctx => SailDownInputTrigger = false;
        }
        
        // Anchor control
        if (anchorAction != null)
        {
            anchorAction.performed += ctx => AnchorInputTrigger = true;
            anchorAction.canceled += ctx => AnchorInputTrigger = false;
        }

        // Combat inputs
        if (mainAttackAction != null)
        {
            mainAttackAction.performed += ctx => MainAttackInputTrigger = true;
            mainAttackAction.canceled += ctx => MainAttackInputTrigger = false;
        }

        if (secondAttackAction != null)
        {
            secondAttackAction.performed += ctx => SecondAttackInputTrigger = true;
            secondAttackAction.canceled += ctx => SecondAttackInputTrigger = false;
        }

        if (thirdAttackAction != null)
        {
            thirdAttackAction.performed += ctx => ThirdAttackInputTrigger = true;
            thirdAttackAction.canceled += ctx => ThirdAttackInputTrigger = false;
        }

        if (fourthAttackAction != null)
        {
            fourthAttackAction.performed += ctx => FourthAttackInputTrigger = true;
            fourthAttackAction.canceled += ctx => FourthAttackInputTrigger = false;
        }
    }

    private void OnEnable()
    {
        // Enable all actions when this component is enabled
        EnableAllActions();
    }

    private void OnDisable()
    {
        // Disable all actions when this component is disabled
        DisableAllActions();
    }

    private void EnableAllActions()
    {
        moveAction?.Enable();
        sailUpAction?.Enable();
        sailDownAction?.Enable();
        anchorAction?.Enable();
        mainAttackAction?.Enable();
        secondAttackAction?.Enable();
        thirdAttackAction?.Enable();
        fourthAttackAction?.Enable();
    }

    private void DisableAllActions()
    {
        moveAction?.Disable();
        sailUpAction?.Disable();
        sailDownAction?.Disable();
        anchorAction?.Disable();
        mainAttackAction?.Disable();
        secondAttackAction?.Disable();
        thirdAttackAction?.Disable();
        fourthAttackAction?.Disable();
    }
}