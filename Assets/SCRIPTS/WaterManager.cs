using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WaterManager : MonoBehaviour 
{
    private MeshFilter meshFilter;
    public bool useGerstnerWaves = true;
    public Material waterMaterial;
    
    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }
    
    private void Start()
    {
        if (waterMaterial != null)
        {
            GetComponent<MeshRenderer>().sharedMaterial = waterMaterial;
        }
        
        // Sync material properties with WaveManager (if exists)
        SyncShaderWithWaveManager();
    }
    
    private void Update()
    {
        // Sync material properties every frame to keep waves consistent
        SyncShaderWithWaveManager();
        
        // Only update mesh vertices if we're using CPU-based animation
        if (!useGerstnerWaves)
        {
            Vector3[] vertices = meshFilter.mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                // Transform vertex to world space
                Vector3 worldPos = transform.TransformPoint(vertices[i]);
                
                if (WaveManager.instance != null)
                {
                    // Simple sine wave (legacy approach)
                    vertices[i].y = WaveManager.instance.GetWaveHeight(worldPos.x);
                }
            }
            
            meshFilter.mesh.vertices = vertices;
            meshFilter.mesh.RecalculateNormals();
        }
    }
    
    private void SyncShaderWithWaveManager()
    {
        if (WaveManager.instance == null || waterMaterial == null)
            return;
            
        // Transfer wave parameters from WaveManager to shader
        waterMaterial.SetVector("_WaveA", new Vector4(
            WaveManager.instance.waveADirection.x,
            WaveManager.instance.waveADirection.y,
            WaveManager.instance.waveASteepness,
            WaveManager.instance.waveAWavelength
        ));
        
        waterMaterial.SetVector("_WaveB", new Vector4(
            WaveManager.instance.waveBDirection.x,
            WaveManager.instance.waveBDirection.y,
            WaveManager.instance.waveBSteepness,
            WaveManager.instance.waveBWavelength
        ));
        
        waterMaterial.SetVector("_WaveC", new Vector4(
            WaveManager.instance.waveCDirection.x,
            WaveManager.instance.waveCDirection.y,
            WaveManager.instance.waveCSteepness, 
            WaveManager.instance.waveCWavelength
        ));
        
        waterMaterial.SetFloat("_WaveSpeed", WaveManager.instance.waveSpeed);
    }
}