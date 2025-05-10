using UnityEngine;
using UnityEngine.InputSystem;

public class PirateShipController : MonoBehaviour
{
    [Header("Ship Settings")]
    [SerializeField] private float rotationSpeed = 60f; // Degrees per second
    [SerializeField] private float acceleration = 0.5f; // How quickly the ship gains speed
    [SerializeField] private float deceleration = 0.3f; // How quickly the ship loses speed
    [SerializeField] private float anchorDeceleration = 5.0f; // Quick stop when anchor hits ground
    [SerializeField] private float maxManeuverableSpeed = 8.0f; // Maximum speed where ship can still maneuver properly

    [Header("Sail Settings")]
    [SerializeField] private float fullyUpSpeed = 0.0f;    // No movement when sails fully up
    [SerializeField] private float halfUpSpeed = 1.0f;     // Half up sail position speed
    [SerializeField] private float defaultSpeed = 2.5f;    // Default sail position speed
    [SerializeField] private float halfDownSpeed = 4.0f;   // Half down sail position speed
    [SerializeField] private float fullyDownSpeed = 6.0f;  // Fully down sail position speed

    private PlayerInputHandler inputHandler;
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private Vector2 rotationInput;
    private bool isMoving = false;
    private PirateShipCollisionHandler collisionHandler;
    
    // Method to get current speed for UI display
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    // Method to reduce speed (called by collision handler)
    public void ReduceSpeed(float reductionFactor)
    {
        currentSpeed *= (1f - reductionFactor);
        if (currentSpeed < 0f)
            currentSpeed = 0f;
    }

    // Ship state tracking
    public enum SailPosition
    {
        FullyUp,        // -2: Sails completely furled
        HalfUp,         // -1: Sails partially furled
        Default,        //  0: Neutral position
        HalfDown,       // +1: Sails partially unfurled
        FullyDown       // +2: Sails completely unfurled
    }
    
    public SailPosition currentSailPosition = SailPosition.Default;
    
    // Public properties for UI to access ship states
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
    
    // Events for UI updates
    public delegate void ShipStateChangedHandler();
    public event ShipStateChangedHandler OnSailStateChanged;
    public event ShipStateChangedHandler OnAnchorStateChanged;

    private void Awake()
    {
        inputHandler = PlayerInputHandler.Instance;
        if (inputHandler == null)
        {
            Debug.LogError("PlayerInputHandler instance not found! Make sure it's in the scene.");
        }
        
        // Get collision handler component
        collisionHandler = GetComponent<PirateShipCollisionHandler>();
        if (collisionHandler == null)
        {
            Debug.LogWarning("PirateShipCollisionHandler not found! Ship collisions won't work properly.");
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
        if (isAnchorMoving)
        {
            anchorTimer += Time.deltaTime;
            
            // Handle anchor raising
            if (isAnchorDown)
            {
                if (anchorTimer >= anchorRaiseTime)
                {
                    isAnchorDown = false;
                    isAnchorMoving = false;
                    // Notify UI of state change
                    OnAnchorStateChanged?.Invoke();
                }
            }
            // Handle anchor lowering
            else
            {
                if (anchorTimer >= anchorLowerTime)
                {
                    isAnchorDown = true;
                    isAnchorMoving = false;
                    // Notify UI of state change
                    OnAnchorStateChanged?.Invoke();
                }
            }
        }
    }

    // Add input handling flags to prevent multiple sail changes per key press
    private bool sailUpHandled = false;
    private bool sailDownHandled = false;
    private bool anchorHandled = false;

    private void HandleSailInput()
    {
        // Allow sail changes only if anchor is not moving and not down
        bool canChangeSails = !isAnchorMoving && !isAnchorDown;
            
        // Handle sail up input with key press protection
        if (inputHandler.SailUpInputTrigger && canChangeSails)
        {
            if (!sailUpHandled) // Only process the input once until key is released
            {
                RaiseSails();
                sailUpHandled = true;
            }
        }
        else
        {
            sailUpHandled = false; // Reset the flag when key is released
        }
        
        // Handle sail down input with key press protection
        if (inputHandler.SailDownInputTrigger && canChangeSails)
        {
            if (!sailDownHandled) // Only process the input once until key is released
            {
                LowerSails();
                sailDownHandled = true;
            }
        }
        else
        {
            sailDownHandled = false; // Reset the flag when key is released
        }
        
        // Handle anchor input as a toggle with key press protection
        if (inputHandler.AnchorInputTrigger && !isAnchorMoving)
        {
            if (!anchorHandled) // Only process the input once until key is released
            {
                ToggleAnchor();
                anchorHandled = true;
            }
        }
        else
        {
            anchorHandled = false; // Reset the flag when key is released
        }

        // Set target speed based on sail position and anchor state
        // Anchor state always overrides sail settings
        if (isAnchorDown || isAnchorMoving)
        {
            targetSpeed = 0f;
        }
        else
        {
            // Only use sail position for speed if anchor is up
            switch (currentSailPosition)
            {
                case SailPosition.FullyUp:
                    targetSpeed = fullyUpSpeed;
                    break;
                case SailPosition.HalfUp:
                    targetSpeed = halfUpSpeed;
                    break;
                case SailPosition.Default:
                    targetSpeed = defaultSpeed;
                    break;
                case SailPosition.HalfDown:
                    targetSpeed = halfDownSpeed;
                    break;
                case SailPosition.FullyDown:
                    targetSpeed = fullyDownSpeed;
                    break;
            }
        }
    }
    
    private void ToggleAnchor()
    {
        // This function now properly toggles the anchor state
        isAnchorMoving = true;
        anchorTimer = 0f;
        
        // Notify UI of state change
        OnAnchorStateChanged?.Invoke();
    }

    private void RaiseSails()
    {
        // Cycle to a higher sail position (less speed)
        SailPosition previousPosition = currentSailPosition;
        
        switch (currentSailPosition)
        {
            case SailPosition.FullyDown:
                currentSailPosition = SailPosition.HalfDown;
                break;
            case SailPosition.HalfDown:
                currentSailPosition = SailPosition.Default;
                break;
            case SailPosition.Default:
                currentSailPosition = SailPosition.HalfUp;
                break;
            case SailPosition.HalfUp:
                currentSailPosition = SailPosition.FullyUp;
                break;
            // If already fully up, do nothing
        }
        
        if (previousPosition != currentSailPosition)
        {
            // Notify UI of state change
            OnSailStateChanged?.Invoke();
        }
    }

    private void LowerSails()
    {
        // Cycle to a lower sail position (more speed)
        SailPosition previousPosition = currentSailPosition;
        
        switch (currentSailPosition)
        {
            case SailPosition.FullyUp:
                currentSailPosition = SailPosition.HalfUp;
                break;
            case SailPosition.HalfUp:
                currentSailPosition = SailPosition.Default;
                break;
            case SailPosition.Default:
                currentSailPosition = SailPosition.HalfDown;
                break;
            case SailPosition.HalfDown:
                currentSailPosition = SailPosition.FullyDown;
                break;
            // If already fully down, do nothing
        }
        
        if (previousPosition != currentSailPosition)
        {
            // Notify UI of state change
            OnSailStateChanged?.Invoke();
        }
    }

    private void HandleRotation()
    {
        // Get move input
        rotationInput = inputHandler.MoveInput;
        
        // Get rotation speed factor based on current speed
        // Ships are harder to turn at higher speeds
        float rotationFactor = Mathf.Lerp(1.0f, 0.5f, Mathf.Clamp01(currentSpeed / maxManeuverableSpeed));
        
        if (rotationInput != Vector2.zero)
        {
            // FIXED ROTATION: Map WASD to specific angles
            float targetRotation = 0f; // Default forward (W)
            
            // Determine which key has the strongest influence
            if (Mathf.Abs(rotationInput.x) > Mathf.Abs(rotationInput.y))
            {
                // A or D is pressed more strongly
                targetRotation = rotationInput.x < 0 ? 270f : 90f;  // A = 270° (-90°), D = 90°
            }
            else
            {
                // W or S is pressed more strongly
                targetRotation = rotationInput.y > 0 ? 0f : 180f;   // W = 0°, S = 180°
            }
            
            // Get current rotation
            float currentAngle = transform.eulerAngles.y;
            
            // Calculate shortest rotation path
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetRotation);
            
            // Apply rotation
            float rotationThisFrame = rotationSpeed * rotationFactor * Time.deltaTime;
            
            // Clamp rotation to not overshoot the target
            if (Mathf.Abs(angleDifference) < rotationThisFrame)
            {
                transform.rotation = Quaternion.Euler(0, targetRotation, 0);
            }
            else
            {
                // Determine rotation direction (positive = clockwise, negative = counter-clockwise)
                float rotationDirection = Mathf.Sign(angleDifference);
                transform.Rotate(0, rotationDirection * rotationThisFrame, 0);
            }
            
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
    }

    private void HandleMovement()
    {
        // Immediately after anchor drops, slow down rapidly
        if (isAnchorDown && currentSpeed > 0)
        {
            // Apply stronger deceleration when anchor hits ground
            // This should stop the ship in less than half a second
            currentSpeed -= anchorDeceleration * Time.deltaTime;
            if (currentSpeed < 0)
                currentSpeed = 0;
                
            // Use the collision handler to apply movement if available
            Vector3 movement = transform.forward * currentSpeed * Time.deltaTime;
            if (collisionHandler != null)
            {
                collisionHandler.ApplyMovement(movement);
            }
            else
            {
                // Fall back to direct transform movement
                transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.Self);
            }
            return;
        }
        
        // Always move forward if sails are down
        if (targetSpeed > 0)
        {
            // Smoothly adjust current speed towards target speed
            if (currentSpeed < targetSpeed)
            {
                currentSpeed += acceleration * Time.deltaTime;
                if (currentSpeed > targetSpeed)
                    currentSpeed = targetSpeed;
            }
            else if (currentSpeed > targetSpeed)
            {
                currentSpeed -= deceleration * Time.deltaTime;
                if (currentSpeed < targetSpeed)
                    currentSpeed = targetSpeed;
            }

            // Move ship forward in the direction it's facing
            Vector3 movement = transform.forward * currentSpeed * Time.deltaTime;
            if (collisionHandler != null)
            {
                collisionHandler.ApplyMovement(movement);
            }
            else
            {
                // Fall back to direct transform movement
                transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.Self);
            }
        }
        else
        {
            // Slow down to a stop when sails are fully up
            if (currentSpeed > 0)
            {
                currentSpeed -= deceleration * Time.deltaTime;
                if (currentSpeed < 0)
                    currentSpeed = 0;
                
                Vector3 movement = transform.forward * currentSpeed * Time.deltaTime;
                if (collisionHandler != null)
                {
                    collisionHandler.ApplyMovement(movement);
                }
                else
                {
                    // Fall back to direct transform movement
                    transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.Self);
                }
            }
        }
    }
}