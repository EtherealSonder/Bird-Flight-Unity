using UnityEngine;

[ExecuteAlways]
public class TerrainManager : MonoBehaviour
{
    [Header("Mesh Variables")]
    public int meshWidth;
    public int meshDepth;
    public AnimationCurve meshHeightCurve;

    [Header("Height Shaping Curves")]
    public AnimationCurve waterHeightCurve;
    public AnimationCurve plainsHeightCurve;
    public AnimationCurve mountainHeightCurve;

    [Header("Terrain Variables")]
    public float verticalScale;
    public float resolution;
    public int numberOfOctaves;
    public float lacunarity;
    [Range(0, 1)]
    public float persistance;

    [Header("Noise Randomization")]
    public int noiseSeed;
    public bool randomizeSeedOnGenerate = false;

    [Header("Erosion Settings")]
    public int erosionIterations = 20;
    [Range(0f, 1f)]
    public float erosionFactor = 0.25f;
    [Range(0f, 1f)]
    public float talus = 0.02f;

    [Header("Tiling Settings")]
    public int chunkSize = 128;
    public Transform terrainParent;

    [Header("Material Settings")]
    public Material terrainMaterial;

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

        // Clear existing chunks
        if (terrainParent != null)
        {
            for (int i = terrainParent.childCount - 1; i >= 0; i--)
                DestroyImmediate(terrainParent.GetChild(i).gameObject);
        }

        if (randomizeSeedOnGenerate)
            noiseSeed = Random.Range(-100000, 100000);

        // Generate height and biome maps
        (float[,] globalHeightMap, int[,] biomeMap) = TerrainGeneration.GenerateHeightMap(
      meshWidth + 1, meshDepth + 1,
      resolution, numberOfOctaves, lacunarity, persistance,
      noiseSeed,
      verticalScale,
      meshHeightCurve,
      waterHeightCurve, plainsHeightCurve, mountainHeightCurve
  );


        TerrainGeneration.ApplyThermalErosion(ref globalHeightMap, erosionIterations, talus, erosionFactor);
        globalHeightMap = TerrainGeneration.NormalizeHeightMap(globalHeightMap);

        int numChunksX = Mathf.CeilToInt((float)meshWidth / chunkSize);
        int numChunksZ = Mathf.CeilToInt((float)meshDepth / chunkSize);

        for (int cx = 0; cx < numChunksX; cx++)
        {
            for (int cz = 0; cz < numChunksZ; cz++)
            {
                int startX = cx * chunkSize;
                int startZ = cz * chunkSize;

                int desiredX = chunkSize + 1;
                int desiredZ = chunkSize + 1;

                int safeX = Mathf.Min(desiredX, globalHeightMap.GetLength(0) - startX);
                int safeZ = Mathf.Min(desiredZ, globalHeightMap.GetLength(1) - startZ);

                float[,] heightMap = new float[safeX, safeZ];
                for (int x = 0; x < safeX; x++)
                    for (int z = 0; z < safeZ; z++)
                        heightMap[x, z] = globalHeightMap[startX + x, startZ + z];

                Mesh mesh = TerrainGeneration.GenerateMesh(heightMap, verticalScale, meshHeightCurve);

                // Debug actual vertex Y range
                float minY = float.MaxValue;
                float maxY = float.MinValue;

                foreach (Vector3 v in mesh.vertices)
                {
                    if (v.y < minY) minY = v.y;
                    if (v.y > maxY) maxY = v.y;
                }


                GameObject chunk = new GameObject($"TerrainChunk_{cx}_{cz}");
                chunk.transform.parent = terrainParent;
                chunk.transform.position = new Vector3(startX - meshWidth / 2f, 0, startZ - meshDepth / 2f);

                MeshFilter mf = chunk.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                MeshRenderer mr = chunk.AddComponent<MeshRenderer>();
                terrainMaterial.SetFloat("_MinHeight", 0f);
                terrainMaterial.SetFloat("_MaxHeight", verticalScale * meshHeightCurve.Evaluate(1f));
                mr.sharedMaterial = terrainMaterial;
                Debug.Log($"Shader _MaxHeight: {verticalScale * meshHeightCurve.Evaluate(1f)}");

                MeshCollider mc = chunk.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
            }
        }
    }
}
