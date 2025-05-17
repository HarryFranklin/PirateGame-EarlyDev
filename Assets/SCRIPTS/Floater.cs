using UnityEngine;

public class Floater : MonoBehaviour 
{
    public Rigidbody rb;
    public float depthBeforeSubmerged = 1.0f;
    public float displacementAmount = 3.0f;
    public int floaterCount = 1;
    public float waterDrag = 0.99f;
    public float waterAngularDrag = 0.5f;
    [Tooltip("Use advanced Gerstner waves instead of simple sine waves")]
    public bool useGerstnerWaves = true;
    
    private void FixedUpdate()
    {
        if (rb == null)
            return;
            
        rb.AddForceAtPosition(Physics.gravity / floaterCount, transform.position, ForceMode.Acceleration);
        
        float waveHeight;
        
        if (!useGerstnerWaves || WaveManager.instance == null)
        {
            // Legacy simple sine wave
            waveHeight = WaveManager.instance != null ? 
                WaveManager.instance.GetWaveHeight(transform.position.x) : 
                0f;
        }
        else
        {
            // Use Gerstner waves
            waveHeight = WaveManager.instance.GetGerstnerWaveHeight(transform.position);
        }
        
        if (transform.position.y < waveHeight)
        {
            float displacementMultiplier = Mathf.Clamp01((waveHeight - transform.position.y) / depthBeforeSubmerged) * displacementAmount;
            
            // Apply buoyancy force
            rb.AddForceAtPosition(
                new Vector3(0f, Mathf.Abs(Physics.gravity.y) * displacementMultiplier, 0f), 
                transform.position, 
                ForceMode.Acceleration
            );
            
            // Apply drag when in water
            rb.AddForce(
                displacementMultiplier * -rb.linearVelocity * waterDrag * Time.fixedDeltaTime, 
                ForceMode.VelocityChange
            );
            
            // Apply angular drag when in water
            rb.AddTorque(
                displacementMultiplier * -rb.angularVelocity * waterAngularDrag * Time.fixedDeltaTime, 
                ForceMode.VelocityChange
            );
        }
    }
}