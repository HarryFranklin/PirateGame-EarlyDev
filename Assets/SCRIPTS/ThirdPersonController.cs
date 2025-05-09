using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Speeds")]
    // 4 Sail Modes - 0 = Fully Up, 1 = Default, 2 = Half Down, 3 = Fully Down
    [SerializeField] private float fullyUpSpeed = 0.25f;
    [SerializeField] private float defaultSpeed = 1f;
    [SerializeField] private float halfDownSpeed = 1.5f;
    [SerializeField] private float fullyDownSpeed = 2f;

    private CharacterController characterController;
    private Camera mainCamera;
    private PlayerInputHandler inputHandler;
    private Vector3 currentMovement;
    public float speed = 1f;

    public enum SailPosition
    {
        FullyUp,
        Default,
        HalfDown,
        FullyDown
    }

    // ✅ Add this to track the current sail mode
    public SailPosition currentSailPosition = SailPosition.Default;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        inputHandler = PlayerInputHandler.Instance;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        // ✅ Correctly use the enum variable
        switch (currentSailPosition)
        {
            case SailPosition.FullyUp:
                speed = fullyUpSpeed;
                break;
            case SailPosition.Default:
                speed = defaultSpeed;
                break;
            case SailPosition.HalfDown:
                speed = halfDownSpeed;
                break;
            case SailPosition.FullyDown:
                speed = fullyDownSpeed;
                break;
        }

        Vector3 inputDirection = new Vector3(inputHandler.MoveInput.x, 0, inputHandler.MoveInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        worldDirection.Normalize();

        currentMovement.x = worldDirection.x * speed;
        currentMovement.z = worldDirection.z * speed;

        characterController.Move(currentMovement * Time.deltaTime);
    }

    void HandleRotation()
    {
        if (inputHandler.MoveInput != Vector2.zero)
        {
            Vector3 direction = new Vector3(inputHandler.MoveInput.x, 0, inputHandler.MoveInput.y);
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
}
