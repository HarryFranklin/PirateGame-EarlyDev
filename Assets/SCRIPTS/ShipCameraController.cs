using UnityEngine;

public class ShipCameraController : MonoBehaviour
{
    [Header("Target References")]
    [SerializeField] private Transform shipTransform;
    [SerializeField] private PirateShipController shipController;

    [Header("Follow Settings")]
    [SerializeField] private float heightOffset = 15f;
    [SerializeField] private float followSpeed = 5f;
    
    [Header("Zoom Settings")]
    [SerializeField] private float minHeight = 10f;
    [SerializeField] private float maxHeight = 25f;
    [SerializeField] private float zoomSpeed = 2f;
    
    // Reference values for interpolating zoom
    [SerializeField] private float minSpeedForZoom = 0f;
    [SerializeField] private float maxSpeedForZoom = 6f;
    
    private float currentHeight;

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
        transform.position = new Vector3(shipTransform.position.x, heightOffset, shipTransform.position.z);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Top-down view
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
        
        // Smoothly adjust current height
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * zoomSpeed);
        
        // Calculate target position (only x and z change, y stays according to height)
        Vector3 targetPosition = new Vector3(
            shipTransform.position.x,
            currentHeight,
            shipTransform.position.z
        );
        
        // Smoothly move camera to target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
        
        // Always maintain top-down rotation (90, 0, 0)
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}