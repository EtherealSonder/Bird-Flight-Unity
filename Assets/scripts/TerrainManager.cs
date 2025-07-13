using UnityEngine;

[ExecuteAlways]
public class TerrainManager : MonoBehaviour
{
    [Space(10)]
    [Header("Mesh Variables")]
    [Space(5)]
    public int meshWidth;
    public int meshDepth;
    public AnimationCurve meshHeightCurve;

    [Space(10)]
    [Header("Terrain Variables")]
    [Space(5)]
    public float verticalScale;
    public float resolution;
    public int numberOfOctaves;
    public float lacunarity;
    [Range(0, 1)]
    public float persistance;

    private float[,] noiseHeights;

    [Space(10)]
    [Header("Noise Randomization")]
    [Space(5)]
    public int noiseSeed;
    public bool randomizeSeedOnGenerate = false;



    [Space(10)]
    [Header("Erosion Settings")]
    [Space(5)]
    [Tooltip("How many erosion passes to perform")]
    public int erosionIterations = 20;

    [Tooltip("How much soil is moved per step")]
    [Range(0f, 1f)]
    public float erosionFactor = 0.25f;

    [Tooltip("Minimum slope difference required for erosion")]
    [Range(0f, 1f)]
    public float talus = 0.02f;


    [Header("Tiling Settings")]
    public int chunkSize = 128;
    public Transform terrainParent;

    void OnValidate()
    {
        if (meshWidth < 1) meshWidth = 1;
        if (meshDepth < 1) meshDepth = 1;
        
        if (lacunarity < 1) lacunarity = 1;
        if (numberOfOctaves < 0) numberOfOctaves = 0;
    }

    void Start()
    {
        GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        if (!Application.isPlaying && !Application.isEditor) return;

        // Clear old tiles
        if (terrainParent != null)
        {
            for (int i = terrainParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(terrainParent.GetChild(i).gameObject);
            }
        }

        if (randomizeSeedOnGenerate)
        {
            noiseSeed = Random.Range(-100000, 100000);
        }

        // Generate a full shared heightmap (include +1 for edges)
        float[,] globalHeightMap = TerrainGeneration.GenerateHeightMap(
            meshWidth + 1, meshDepth + 1,
            resolution, numberOfOctaves, lacunarity, persistance,
            noiseSeed
        );

        // Apply erosion and normalization to global map
        TerrainGeneration.ApplyThermalErosion(ref globalHeightMap, erosionIterations, talus, erosionFactor);
        globalHeightMap = TerrainGeneration.NormalizeHeightMap(globalHeightMap);

        // Loop through terrain chunks
        int numChunksX = Mathf.CeilToInt((float)meshWidth / chunkSize);
        int numChunksZ = Mathf.CeilToInt((float)meshDepth / chunkSize);

        for (int cx = 0; cx < numChunksX; cx++)
        {
            for (int cz = 0; cz < numChunksZ; cz++)
            {
                int startX = cx * chunkSize;
                int startZ = cz * chunkSize;

                // Force every chunk to try for chunkSize + 1 vertices
                int desiredVertexCountX = chunkSize + 1;
                int desiredVertexCountZ = chunkSize + 1;

                // Clamp to not exceed bounds of the global heightmap
                int safeSizeX = Mathf.Min(desiredVertexCountX, globalHeightMap.GetLength(0) - startX);
                int safeSizeZ = Mathf.Min(desiredVertexCountZ, globalHeightMap.GetLength(1) - startZ);

                // Extract shared slice from global heightmap
                float[,] heightMap = new float[safeSizeX, safeSizeZ];
                for (int x = 0; x < safeSizeX; x++)
                {
                    for (int z = 0; z < safeSizeZ; z++)
                    {
                        heightMap[x, z] = globalHeightMap[startX + x, startZ + z];
                    }
                }

                Mesh mesh = TerrainGeneration.GenerateMesh(heightMap, verticalScale, meshHeightCurve);

                // Create chunk object
                GameObject chunk = new GameObject($"TerrainChunk_{cx}_{cz}");
                chunk.transform.parent = terrainParent;

                // Compute world position based on quads (not vertices)
                float worldX = startX - meshWidth / 2f;
                float worldZ = startZ - meshDepth / 2f;
                chunk.transform.position = new Vector3(worldX, 0, worldZ);

                // Debug chunk dimensions
                int chunkVertexWidth = heightMap.GetLength(0);
                int chunkVertexDepth = heightMap.GetLength(1);
                Debug.Log($"Chunk [{cx},{cz}] at pos ({worldX}, {worldZ}), size: {chunkVertexWidth}x{chunkVertexDepth}");

                // Assign mesh + materials
                MeshFilter mf = chunk.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                MeshRenderer mr = chunk.AddComponent<MeshRenderer>();
                mr.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = true;

                MeshCollider mc = chunk.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
            }
        }
    }


    /*public void GenerateTerrain()
    {
        if (!Application.isPlaying && !Application.isEditor) return;

        if (randomizeSeedOnGenerate)
        {
            noiseSeed = Random.Range(-100000, 100000);
        }
        // Step 1: Generate raw noise
        noiseHeights = TerrainGeneration.GenerateHeightMap(
    meshWidth, meshDepth, resolution,
    numberOfOctaves, lacunarity, persistance,
    noiseSeed
);


        // Step 2: Apply erosion BEFORE normalization
        TerrainGeneration.ApplyThermalErosion(ref noiseHeights, erosionIterations, talus, erosionFactor);

        // Step 3: Normalize AFTER erosion
        noiseHeights = TerrainGeneration.NormalizeHeightMap(noiseHeights);

        // Step 4: Generate and assign the mesh
        Mesh mesh = TerrainGeneration.GenerateMesh(noiseHeights, verticalScale, meshHeightCurve);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }*/


}
