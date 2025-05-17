using UnityEngine;
using UnityEngine.InputSystem;

public class PirateShipController : MonoBehaviour
{
    [Header("Ship Settings")]
    [SerializeField] private float rotationSpeed = 60f; // Degrees per second
    [SerializeField] private float acceleration = 0.5f;
    [SerializeField] private float deceleration = 0.3f;
    [SerializeField] private float anchorDeceleration = 5.0f;
    [SerializeField] private float maxManeuverableSpeed = 8.0f;

    [Header("Sail Settings")]
    [SerializeField] private float fullyUpSpeed = 0.0f;
    [SerializeField] private float halfUpSpeed = 1.0f;
    [SerializeField] private float defaultSpeed = 2.5f;
    [SerializeField] private float halfDownSpeed = 4.0f;
    [SerializeField] private float fullyDownSpeed = 6.0f;
    [SerializeField] private Vector3 modelRotationOffset = new Vector3(0f, -90f, 0f);
    
    [SerializeField] private PlayerInputHandler inputHandler;
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;

    public float GetCurrentSpeed() => currentSpeed;

    public void ReduceSpeed(float reductionFactor)
    {
        currentSpeed *= (1f - reductionFactor);
        if (currentSpeed < 0f) currentSpeed = 0f;
    }

    public enum SailPosition
    {
        FullyUp, HalfUp, Default, HalfDown, FullyDown
    }

    public SailPosition currentSailPosition = SailPosition.Default;

    public bool IsAnchorDown => isAnchorDown;
    public bool IsAnchorMoving => isAnchorMoving;
    public float AnchorProgressPercent => isAnchorMoving ?
        (isAnchorDown ? anchorTimer / anchorRaiseTime : anchorTimer / anchorLowerTime) :
        (isAnchorDown ? 1f : 0f);

    [Header("Anchor Settings")]
    [SerializeField] private float anchorLowerTime = 1.0f;
    [SerializeField] private float anchorRaiseTime = 3.0f;

    private bool isAnchorDown = false;
    private bool isAnchorMoving = false;
    private float anchorTimer = 0f;

    public delegate void ShipStateChangedHandler();
    public event ShipStateChangedHandler OnSailStateChanged;
    public event ShipStateChangedHandler OnAnchorStateChanged;

    private void Start()
    {
        inputHandler = PlayerInputHandler.Instance;
        if (inputHandler == null)
        {
            Debug.LogError("PlayerInputHandler instance not found!");
        }
    }

    private void Update()
    {
        HandleSailInput();
        HandleAnchorState();
        HandleRotation();
        HandleMovement();
    }

    private void HandleAnchorState()
    {
        if (!isAnchorMoving) return;

        anchorTimer += Time.deltaTime;

        if (isAnchorDown)
        {
            if (anchorTimer >= anchorRaiseTime)
            {
                isAnchorDown = false;
                isAnchorMoving = false;
                OnAnchorStateChanged?.Invoke();
            }
        }
        else
        {
            if (anchorTimer >= anchorLowerTime)
            {
                isAnchorDown = true;
                isAnchorMoving = false;
                OnAnchorStateChanged?.Invoke();
            }
        }
    }

    private bool sailUpHandled = false;
    private bool sailDownHandled = false;
    private bool anchorHandled = false;

    private void HandleSailInput()
    {
        bool canChangeSails = !isAnchorMoving && !isAnchorDown;

        if (inputHandler.SailUpInputTrigger && canChangeSails)
        {
            if (!sailUpHandled)
            {
                RaiseSails();
                sailUpHandled = true;
            }
        }
        else sailUpHandled = false;

        if (inputHandler.SailDownInputTrigger && canChangeSails)
        {
            if (!sailDownHandled)
            {
                LowerSails();
                sailDownHandled = true;
            }
        }
        else sailDownHandled = false;

        if (inputHandler.AnchorInputTrigger && !isAnchorMoving)
        {
            if (!anchorHandled)
            {
                ToggleAnchor();
                anchorHandled = true;
            }
        }
        else anchorHandled = false;

        if (isAnchorDown || isAnchorMoving)
        {
            targetSpeed = 0f;
        }
        else
        {
            switch (currentSailPosition)
            {
                case SailPosition.FullyUp: targetSpeed = fullyUpSpeed; break;
                case SailPosition.HalfUp: targetSpeed = halfUpSpeed; break;
                case SailPosition.Default: targetSpeed = defaultSpeed; break;
                case SailPosition.HalfDown: targetSpeed = halfDownSpeed; break;
                case SailPosition.FullyDown: targetSpeed = fullyDownSpeed; break;
            }
        }
    }

    private void ToggleAnchor()
    {
        isAnchorMoving = true;
        anchorTimer = 0f;
        OnAnchorStateChanged?.Invoke();
    }

    private void RaiseSails()
    {
        SailPosition prev = currentSailPosition;

        switch (currentSailPosition)
        {
            case SailPosition.FullyDown: currentSailPosition = SailPosition.HalfDown; break;
            case SailPosition.HalfDown: currentSailPosition = SailPosition.Default; break;
            case SailPosition.Default: currentSailPosition = SailPosition.HalfUp; break;
            case SailPosition.HalfUp: currentSailPosition = SailPosition.FullyUp; break;
        }

        if (prev != currentSailPosition) OnSailStateChanged?.Invoke();
    }

    private void LowerSails()
    {
        SailPosition prev = currentSailPosition;

        switch (currentSailPosition)
        {
            case SailPosition.FullyUp: currentSailPosition = SailPosition.HalfUp; break;
            case SailPosition.HalfUp: currentSailPosition = SailPosition.Default; break;
            case SailPosition.Default: currentSailPosition = SailPosition.HalfDown; break;
            case SailPosition.HalfDown: currentSailPosition = SailPosition.FullyDown; break;
        }

        if (prev != currentSailPosition) OnSailStateChanged?.Invoke();
    }

    private void HandleRotation()
    {
        Vector2 moveInput = inputHandler.MoveInput;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleMovement()
    {
        if (isAnchorDown && currentSpeed > 0)
        {
            currentSpeed -= anchorDeceleration * Time.deltaTime;
            if (currentSpeed < 0) currentSpeed = 0;
        }
        else if (targetSpeed > 0)
        {
            if (currentSpeed < targetSpeed)
            {
                currentSpeed += acceleration * Time.deltaTime;
                if (currentSpeed > targetSpeed) currentSpeed = targetSpeed;
            }
            else if (currentSpeed > targetSpeed)
            {
                currentSpeed -= deceleration * Time.deltaTime;
                if (currentSpeed < targetSpeed) currentSpeed = targetSpeed;
            }
        }
        else
        {
            if (currentSpeed > 0)
            {
                currentSpeed -= deceleration * Time.deltaTime;
                if (currentSpeed < 0) currentSpeed = 0;
            }
        }

        if (currentSpeed > 0)
        {
            transform.Translate(Vector3.right * currentSpeed * Time.deltaTime, Space.Self);
        }
    }
}