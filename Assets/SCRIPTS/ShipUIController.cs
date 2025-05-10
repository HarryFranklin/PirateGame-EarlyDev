using UnityEngine;
using TMPro;

public class ShipUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PirateShipController shipController;
    
    [Header("UI Text Elements")]
    [SerializeField] private TextMeshProUGUI sailStateText;
    [SerializeField] private TextMeshProUGUI anchorStateText;
    [SerializeField] private TextMeshProUGUI speedText;
    
    private void Start()
    {
        if (shipController == null)
        {
            Debug.LogError("Ship Controller reference is missing!");
            return;
        }
        
        // Subscribe to ship state change events
        shipController.OnSailStateChanged += UpdateSailUI;
        shipController.OnAnchorStateChanged += UpdateAnchorUI;
        
        // Initial UI update
        UpdateSailUI();
        UpdateAnchorUI();
        
        Debug.Log("Ship UI Controller initialized");
    }
    
    private void Update()
    {
        // Update speed text
        if (speedText != null)
        {
            float shipSpeed = Mathf.Round(shipController.GetCurrentSpeed() * 10f) / 10f;
            speedText.text = $"Speed: {shipSpeed} knots";
        }
        
        // Update anchor text if anchor is moving
        if (shipController.IsAnchorMoving)
        {
            UpdateAnchorUI();
        }
    }
    
    private void UpdateSailUI()
    {
        // Update sail state text
        if (sailStateText != null)
        {
            // Format the enum name to be more readable (e.g., "HalfDown" -> "Half Down")
            string stateText = shipController.currentSailPosition.ToString();
            stateText = System.Text.RegularExpressions.Regex.Replace(stateText, "([a-z])([A-Z])", "$1 $2");
            sailStateText.text = $"Sails: {stateText}";
            
            // Add debug logging to help see when sail state actually changes
            Debug.Log($"Sail state updated to: {stateText}");
        }
    }
    
    private void UpdateAnchorUI()
    {
        if (anchorStateText != null)
        {
            if (shipController.IsAnchorMoving)
            {
                string progressPercent = Mathf.Floor(shipController.AnchorProgressPercent * 100f).ToString();
                anchorStateText.text = shipController.IsAnchorDown ? 
                    $"Raising Anchor... {progressPercent}%" : 
                    $"Lowering Anchor... {progressPercent}%";
            }
            else
            {
                anchorStateText.text = shipController.IsAnchorDown ? "Anchor: Down" : "Anchor: Up";
            }
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (shipController != null)
        {
            shipController.OnSailStateChanged -= UpdateSailUI;
            shipController.OnAnchorStateChanged -= UpdateAnchorUI;
        }
    }
}