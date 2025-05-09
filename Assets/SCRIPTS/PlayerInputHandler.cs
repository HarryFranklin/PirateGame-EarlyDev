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
    [SerializeField] private string mainAttack = "MainAttack";
    [SerializeField] private string secondAttack = "SecondAttack";
    [SerializeField] private string thirdAttack = "ThirdAttack";
    [SerializeField] private string fourthAttack = "FourthAttack";
    [SerializeField] private string sailUp = "SailUp";
    [SerializeField] private string sailDown = "SailDown";

    private InputAction moveAction;
    private InputAction mainAttackAction;
    private InputAction secondAttackAction;
    private InputAction thirdAttackAction;
    private InputAction fourthAttackAction;
    private InputAction sailUpAction;
    private InputAction sailDownAction;

    public Vector2 MoveInput { get; private set; }
    public bool MainAttackInputTrigger { get; private set; }
    public bool SecondAttackInputTrigger { get; private set; }
    public bool ThirdAttackInputTrigger { get; private set; }
    public bool FourthAttackInputTrigger { get; private set; }
    public bool SailUpInputTrigger { get; private set; }
    public bool SailDownInputTrigger { get; private set; }

    public static PlayerInputHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        moveAction = playerControls.FindActionMap(actionMapName).FindAction(move);
        mainAttackAction = playerControls.FindActionMap(actionMapName).FindAction(mainAttack);
        secondAttackAction = playerControls.FindActionMap(actionMapName).FindAction(secondAttack);
        thirdAttackAction = playerControls.FindActionMap(actionMapName).FindAction(thirdAttack);
        fourthAttackAction = playerControls.FindActionMap(actionMapName).FindAction(fourthAttack);
        sailUpAction = playerControls.FindActionMap(actionMapName).FindAction(sailUp);
        sailDownAction = playerControls.FindActionMap(actionMapName).FindAction(sailDown);

        RegisterInputActions();
    }

    void RegisterInputActions()
    {
        moveAction.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => MoveInput = Vector2.zero;

        mainAttackAction.performed += ctx => MainAttackInputTrigger = true;
        mainAttackAction.canceled += ctx => MainAttackInputTrigger = false;

        secondAttackAction.performed += ctx => SecondAttackInputTrigger = true;
        secondAttackAction.canceled += ctx => SecondAttackInputTrigger = false;

        thirdAttackAction.performed += ctx => ThirdAttackInputTrigger = true;
        thirdAttackAction.canceled += ctx => ThirdAttackInputTrigger = false;

        fourthAttackAction.performed += ctx => FourthAttackInputTrigger = true;
        fourthAttackAction.canceled += ctx => FourthAttackInputTrigger = false;

        sailUpAction.performed += ctx => SailUpInputTrigger = true;
        sailUpAction.canceled += ctx => SailUpInputTrigger = false;

        sailDownAction.performed += ctx => SailDownInputTrigger = true;
        sailDownAction.canceled += ctx => SailDownInputTrigger = false;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        mainAttackAction.Enable();
        secondAttackAction.Enable();
        thirdAttackAction.Enable();
        fourthAttackAction.Enable();
        sailUpAction.Enable();
        sailDownAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        mainAttackAction.Disable();
        secondAttackAction.Disable();
        thirdAttackAction.Disable();
        fourthAttackAction.Disable();
        sailUpAction.Disable();
        sailDownAction.Disable();
    }
}
