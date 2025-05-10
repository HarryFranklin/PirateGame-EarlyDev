using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class PirateShipCollisionHandler : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private float headOnCollisionAngle = 30f; // Angle threshold for head-on collision
    [SerializeField] private float bounceForce = 2.0f; // Force applied when ship bounces off obstacles
    [SerializeField] private float collisionDamageThreshold = 2.0f; // Min speed to cause damage
    [SerializeField] private float headOnSpeedReduction = 0.9f; // How much to reduce speed on head-on collision
    [SerializeField] private float obliqueSpeedReduction = 0.3f; // How much to reduce speed on oblique collision
    [SerializeField] private LayerMask collisionLayers; // Layers that the ship can collide with

    // Reference to the ship controller
    private PirateShipController shipController;
    // Reference to the rigidbody component
    private Rigidbody shipRigidbody;
    // Reference to the collider component
    private BoxCollider shipCollider;
    
    // The last collision contact normal - used for calculating deflection
    private Vector3 lastContactNormal;
    // Time of the last collision - used to prevent multiple collision responses
    private float lastCollisionTime = 0f;
    // Cooldown between collision responses
    private float collisionCooldown = 0.2f;

    private void Awake()
    {
        // Get the references to required components
        shipController = GetComponent<PirateShipController>();
        shipRigidbody = GetComponent<Rigidbody>();
        shipCollider = GetComponent<BoxCollider>();
        
        if (shipController == null)
        {
            Debug.LogError("PirateShipController component not found!");
        }
        
        // Configure rigidbody for proper ship physics
        ConfigureRigidbody();
        
        // Configure collider
        ConfigureCollider();
    }

    private void ConfigureRigidbody()
    {
        // We want to control the ship's movement manually but use the physics system for collisions
        shipRigidbody.useGravity = false; // No gravity - ship floats on water
        shipRigidbody.isKinematic = false; // Non-kinematic to detect collisions
        shipRigidbody.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
        shipRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection
        shipRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | 
                                   RigidbodyConstraints.FreezeRotationZ |
                                   RigidbodyConstraints.FreezePositionY; // Only move on XZ plane, only rotate around Y
        
        // Set mass for realistic ship feel
        shipRigidbody.mass = 1000f; // Ships are heavy
        shipRigidbody.linearDamping = 0.5f; // Water resistance
        shipRigidbody.angularDamping = 0.5f; // Resistance to rotation
    }

    private void ConfigureCollider()
    {
        shipCollider.isTrigger = false; // We want physical collisions
    }

    // Call this from PirateShipController to apply forces after collisions
    public void ApplyMovement(Vector3 movement)
    {
        // Directly move the rigidbody for better physics interaction
        shipRigidbody.MovePosition(shipRigidbody.position + movement);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if we should process this collision based on cooldown
        if (Time.time < lastCollisionTime + collisionCooldown)
            return;
        
        lastCollisionTime = Time.time;
        
        // Get the collision contact point
        ContactPoint contact = collision.GetContact(0);
        lastContactNormal = contact.normal;
        
        // Calculate the angle between the ship's forward direction and the collision normal
        float collisionAngle = Vector3.Angle(transform.forward, -contact.normal);
        
        // Calculate the ship's speed at the moment of impact
        float impactSpeed = shipController.GetCurrentSpeed();
        
        // Determine if this is a head-on collision or an oblique collision
        if (collisionAngle <= headOnCollisionAngle)
        {
            // Head-on collision - stop the ship
            HandleHeadOnCollision(impactSpeed);
        }
        else
        {
            // Oblique collision - deflect the ship
            HandleObliqueCollision(contact.normal, impactSpeed);
        }
        
        // Log collision details
        Debug.Log($"Ship collision: Angle={collisionAngle}, Speed={impactSpeed}, Head-on={collisionAngle <= headOnCollisionAngle}");
    }

    private void HandleHeadOnCollision(float impactSpeed)
    {
        // For head-on collisions, apply a strong speed reduction
        shipController.ReduceSpeed(headOnSpeedReduction);
        
        // Apply a small backwards force
        Vector3 bounceDirection = -transform.forward;
        ApplyCollisionForce(bounceDirection, impactSpeed * 0.5f);
        
        // TODO: Apply damage to the ship based on impact speed
        if (impactSpeed > collisionDamageThreshold)
        {
            // Call a method to apply damage 
            // shipController.ApplyDamage(impactSpeed);
        }
    }

    private void HandleObliqueCollision(Vector3 contactNormal, float impactSpeed)
    {
        // For oblique collisions, apply a moderate speed reduction
        shipController.ReduceSpeed(obliqueSpeedReduction);
        
        // Calculate deflection direction (reflect the forward vector off the collision surface)
        Vector3 deflectionDirection = Vector3.Reflect(transform.forward, contactNormal).normalized;
        
        // Only use the horizontal component of the deflection
        deflectionDirection.y = 0;
        deflectionDirection.Normalize();
        
        // Apply force in the deflection direction
        ApplyCollisionForce(deflectionDirection, impactSpeed * bounceForce);
        
        // Optional: Rotate the ship slightly towards the deflection direction
        // A smooth rotation towards the deflection direction could be applied here
    }

    private void ApplyCollisionForce(Vector3 direction, float force)
    {
        // Apply an immediate impulse force
        shipRigidbody.AddForce(direction * force, ForceMode.Impulse);
    }
}