using UnityEngine;

public class WaveManager : MonoBehaviour 
{
    public static WaveManager instance;
    
    // Wave parameters (matching shader)
    [Header("Wave A")]
    public Vector2 waveADirection = new Vector2(1, 0);
    public float waveASteepness = 0.5f;
    public float waveAWavelength = 10f;
    
    [Header("Wave B")]
    public Vector2 waveBDirection = new Vector2(0, 1);
    public float waveBSteepness = 0.25f;
    public float waveBWavelength = 20f;
    
    [Header("Wave C")]
    public Vector2 waveCDirection = new Vector2(1, 1);
    public float waveCSteepness = 0.15f;
    public float waveCWavelength = 10f;
    
    [Header("General Settings")]
    public float waveSpeed = 1.0f;
    
    // Keep simple sine wave for backward compatibility
    [Header("Simple Wave (Legacy)")]
    public float amplitude = 1.0f;
    public float length = 2.0f;
    public float speed = 1.0f;
    public float offset = 0f;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying this one.");
            Destroy(this);
        }
    }
    
    private void Update()
    {
        // Update simple wave offset
        offset += Time.deltaTime * speed;
    }
    
    // Legacy method for simple sine wave
    public float GetWaveHeight(float x)
    {
        return amplitude * Mathf.Sin((x / length) + offset);
    }
    
    // New method using Gerstner waves (position-based, matching shader)
    public float GetGerstnerWaveHeight(Vector3 position)
    {
        float height = 0f;
        
        // Wave A
        height += GerstnerWaveHeight(position, waveADirection, waveASteepness, waveAWavelength);
        
        // Wave B
        height += GerstnerWaveHeight(position, waveBDirection, waveBSteepness, waveBWavelength);
        
        // Wave C
        height += GerstnerWaveHeight(position, waveCDirection, waveCSteepness, waveCWavelength);
        
        return height;
    }
    
    private float GerstnerWaveHeight(Vector3 p, Vector2 direction, float steepness, float wavelength)
    {
        direction.Normalize();
        
        float k = 2 * Mathf.PI / wavelength;
        float c = Mathf.Sqrt(9.8f / k);
        float f = k * (Vector2.Dot(direction, new Vector2(p.x, p.z)) - c * Time.time * waveSpeed);
        float a = steepness / k;
        
        return a * Mathf.Sin(f);
    }
    
    // Get the full Gerstner wave position (for more advanced usage)
    public Vector3 GetGerstnerWavePosition(Vector3 position)
    {
        Vector3 result = position;
        
        result += GerstnerWave(position, waveADirection, waveASteepness, waveAWavelength);
        result += GerstnerWave(position, waveBDirection, waveBSteepness, waveBWavelength);
        result += GerstnerWave(position, waveCDirection, waveCSteepness, waveCWavelength);
        
        return result;
    }
    
    private Vector3 GerstnerWave(Vector3 p, Vector2 direction, float steepness, float wavelength)
    {
        direction.Normalize();
        
        float k = 2 * Mathf.PI / wavelength;
        float c = Mathf.Sqrt(9.8f / k);
        float f = k * (Vector2.Dot(direction, new Vector2(p.x, p.z)) - c * Time.time * waveSpeed);
        float a = steepness / k;
        
        return new Vector3(
            direction.x * (a * Mathf.Cos(f)),
            a * Mathf.Sin(f),
            direction.y * (a * Mathf.Cos(f))
        );
    }
}