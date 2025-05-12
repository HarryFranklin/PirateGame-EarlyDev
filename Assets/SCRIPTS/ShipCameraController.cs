using UnityEngine;

public class ShipCameraController : MonoBehaviour
{
    [Header("Target References")]
    [SerializeField] private Transform shipTransform;
    [SerializeField] private PirateShipController shipController;

    [Header("Follow Settings")]
    [SerializeField] private float heightOffset = 15f;
    [SerializeField] private float minZOffset = 4.0f;
    [SerializeField] private float maxZOffset = 5.5f;
    [SerializeField] private float followSpeed = 5f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float startingRotationX = 85f;
    [SerializeField] private float midSpeedRotationX = 60f;
    [SerializeField] private float rotationSpeed = 2f;
    
    [Header("Zoom Settings")]
    [SerializeField] private float minHeight = 10f;
    [SerializeField] private float maxHeight = 25f;
    [SerializeField] private float zoomSpeed = 2f;
    
    // Reference values for interpolating zoom and rotation
    [SerializeField] private float minSpeedForZoom = 0f;
    [SerializeField] private float maxSpeedForZoom = 6f;
    
    private float currentHeight;
    private float zPosition;
    private float currentRotationX;

    private void Start()
    {
        if (shipTransform == null)
        {
            Debug.LogError("Ship Transform reference is missing on ShipCameraController!");
            enabled = false;
            return;
        }
        
        if (shipController == null)
        {
            Debug.LogError("PirateShipController reference is missing on ShipCameraController!");
            enabled = false;
            return;
        }
        
        // Initialise camera position and rotation
        currentHeight = heightOffset;
        currentRotationX = startingRotationX;
        zPosition = shipTransform.position.z - minZOffset;
        transform.position = new Vector3(
            shipTransform.position.x, 
            heightOffset, 
            shipTransform.position.z - minZOffset
        );
        transform.rotation = Quaternion.Euler(currentRotationX, 0f, 0f);
    }

    private void LateUpdate()
    {
        if (shipTransform == null)
            return;
            
        // Get the current ship speed
        float shipSpeed = shipController.GetCurrentSpeed();
        
        // Calculate target height based on ship speed (interpolate between min and max)
        float targetHeight = Mathf.Lerp(minHeight, maxHeight, 
            Mathf.InverseLerp(minSpeedForZoom, maxSpeedForZoom, shipSpeed));
        
        // Calculate target z-offset based on ship speed
        float targetZOffset = Mathf.Lerp(minZOffset, maxZOffset, 
            Mathf.InverseLerp(minSpeedForZoom, maxSpeedForZoom, shipSpeed));
        
        // Calculate target rotation based on ship speed
        float targetRotationX;
        if (shipSpeed <= minSpeedForZoom)
        {
            // Slow or not moving - go back to starting rotation
            targetRotationX = startingRotationX;
        }
        else if (shipSpeed >= maxSpeedForZoom)
        {
            // At max speed - use mid-speed rotation
            targetRotationX = midSpeedRotationX;
        }
        else
        {
            // Interpolate between starting and mid-speed rotations
            targetRotationX = Mathf.Lerp(startingRotationX, midSpeedRotationX, 
                Mathf.InverseLerp(minSpeedForZoom, maxSpeedForZoom, shipSpeed));
        }
        
        // Smoothly adjust current height, z-offset, and rotation
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * zoomSpeed);
        zPosition = Mathf.Lerp(zPosition, shipTransform.position.z - targetZOffset, Time.deltaTime * followSpeed);
        currentRotationX = Mathf.Lerp(currentRotationX, targetRotationX, Time.deltaTime * rotationSpeed);
        
        // Calculate target position 
        Vector3 targetPosition = new Vector3(
            shipTransform.position.x,
            currentHeight,
            zPosition
        );
        
        // Smoothly move camera to target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
        
        // Update camera rotation
        transform.rotation = Quaternion.Euler(currentRotationX, 0f, 0f);
    }
}