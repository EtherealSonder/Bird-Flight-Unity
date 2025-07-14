// ✅ TerrainStreamer.cs (Final Version)
// ✅ Global erosion + seamless + infinite feel using wrapping

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TerrainStreamer : MonoBehaviour
{
    [Header("References")]
    public TerrainManager terrainManager;
    public Rigidbody playerBody;
    public Transform terrainParent;

    [Header("Streaming Settings")]
    public int viewDistance = 3;
    public int poolInitialSize = 20;

    private Vector2Int currentCenterChunk;
    private Dictionary<Vector2Int, GameObject> activeChunks = new();
    private Queue<Vector2Int> chunkQueue = new();
    private bool isProcessing = false;

    private float[,] globalHeightMap;
    private bool[,] globalLakeMask;
    private int globalWidth, globalDepth;

    private Queue<GameObject> chunkPool = new();

    void Start()
    {
        GenerateGlobalHeightMap();
        InitChunkPool();
        currentCenterChunk = GetChunkCoord(playerBody.position);
        UpdateChunks(force: true);
    }

    void Update()
    {
        Vector2Int newCenterChunk = GetChunkCoord(playerBody.position);
        if (newCenterChunk != currentCenterChunk)
        {
            currentCenterChunk = newCenterChunk;
            UpdateChunks();
        }
    }

    void InitChunkPool()
    {
        for (int i = 0; i < poolInitialSize; i++)
        {
            GameObject pooled = new GameObject("PooledChunk");
            pooled.transform.parent = terrainParent;
            pooled.SetActive(false);
            pooled.AddComponent<MeshFilter>();
            pooled.AddComponent<MeshRenderer>();
            pooled.AddComponent<MeshCollider>();
            chunkPool.Enqueue(pooled);
        }
    }

    GameObject GetPooledChunk()
    {
        if (chunkPool.Count > 0)
        {
            GameObject obj = chunkPool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        GameObject fallback = new GameObject("PooledChunk_Dynamic");
        fallback.transform.parent = terrainParent;
        fallback.AddComponent<MeshFilter>();
        fallback.AddComponent<MeshRenderer>();
        fallback.AddComponent<MeshCollider>();
        return fallback;
    }

    void ReturnChunkToPool(GameObject chunk)
    {
        foreach (Transform child in chunk.transform)
            Destroy(child.gameObject);

        chunk.SetActive(false);
        chunk.transform.SetParent(terrainParent);
        chunkPool.Enqueue(chunk);
    }

    void GenerateGlobalHeightMap()
    {
        globalWidth = terrainManager.meshWidth + 1;
        globalDepth = terrainManager.meshDepth + 1;

        (globalHeightMap, _) = TerrainGeneration.GenerateHeightMap(
            globalWidth, globalDepth,
            terrainManager.resolution,
            terrainManager.numberOfOctaves,
            terrainManager.lacunarity,
            terrainManager.persistance,
            terrainManager.noiseSeed,
            terrainManager.verticalScale,
            terrainManager.meshHeightCurve,
            terrainManager.waterHeightCurve,
            terrainManager.plainsHeightCurve,
            terrainManager.mountainHeightCurve,
            terrainManager.regionNoiseScale,
            terrainManager.waterThresholdFlat,
            terrainManager.waterThresholdMountain,
            terrainManager.plainsThresholdFlat,
            terrainManager.plainsThresholdMountain,
            terrainManager.peakNoisePower
        );

        TerrainGeneration.ApplyThermalErosion(ref globalHeightMap, terrainManager.erosionIterations, terrainManager.talus, terrainManager.erosionFactor);
        globalHeightMap = TerrainGeneration.NormalizeHeightMap(globalHeightMap);
        globalLakeMask = TerrainGeneration.GenerateLakeMask(globalHeightMap, terrainManager.lakeHeightThreshold);
    }

    void UpdateChunks(bool force = false)
    {
        HashSet<Vector2Int> chunksToKeep = new();

        for (int dx = -viewDistance; dx <= viewDistance; dx++)
        {
            for (int dz = -viewDistance; dz <= viewDistance; dz++)
            {
                Vector2Int coord = currentCenterChunk + new Vector2Int(dx, dz);
                chunksToKeep.Add(coord);

                if (!activeChunks.ContainsKey(coord) && !chunkQueue.Contains(coord))
                    chunkQueue.Enqueue(coord);
            }
        }

        List<Vector2Int> toRemove = new();
        foreach (var kv in activeChunks)
        {
            if (!chunksToKeep.Contains(kv.Key))
            {
                ReturnChunkToPool(kv.Value);
                toRemove.Add(kv.Key);
            }
        }

        foreach (var key in toRemove)
            activeChunks.Remove(key);

        if (!isProcessing)
            StartCoroutine(ProcessChunkQueue());
    }

    IEnumerator ProcessChunkQueue()
    {
        isProcessing = true;

        while (chunkQueue.Count > 0)
        {
            Vector2Int coord = chunkQueue.Dequeue();

            var task = Task.Run(() => GenerateChunkDataWrapped(coord));
            while (!task.IsCompleted)
                yield return null;

            ChunkData result = task.Result;
            if (result == null) continue;

            GameObject chunk = GetPooledChunk();
            chunk.name = $"Chunk_{coord.x}_{coord.y}";
            chunk.transform.position = ChunkToWorldPos(coord);

            Mesh mesh = TerrainGeneration.GenerateMesh(result.heightMap, terrainManager.verticalScale, terrainManager.meshHeightCurve);
            if (mesh == null || mesh.vertexCount < 3)
            {
                ReturnChunkToPool(chunk);
                continue;
            }

            chunk.GetComponent<MeshFilter>().sharedMesh = mesh;
            chunk.GetComponent<MeshRenderer>().sharedMaterial = terrainManager.terrainMaterial;
            chunk.GetComponent<MeshCollider>().sharedMesh = mesh;

            Mesh waterMesh = TerrainGeneration.GenerateFlatWaterMesh(result.lakeMask, terrainManager.worldWaterY);
            if (waterMesh != null && waterMesh.vertexCount >= 3)
            {
                GameObject water = new GameObject("Water");
                water.transform.parent = chunk.transform;
                water.transform.localPosition = Vector3.zero;

                var wf = water.AddComponent<MeshFilter>();
                wf.sharedMesh = waterMesh;

                var wr = water.AddComponent<MeshRenderer>();
                wr.sharedMaterial = terrainManager.waterMaterial;
            }

            activeChunks[coord] = chunk;
            yield return null;
        }

        isProcessing = false;
    }

    Vector2Int GetChunkCoord(Vector3 worldPos)
    {
        int size = terrainManager.chunkSize;
        int cx = Mathf.FloorToInt(worldPos.x / size);
        int cz = Mathf.FloorToInt(worldPos.z / size);
        return new Vector2Int(cx, cz);
    }

    Vector3 ChunkToWorldPos(Vector2Int coord)
    {
        int size = terrainManager.chunkSize;
        return new Vector3(coord.x * size, 0, coord.y * size);
    }

    class ChunkData
    {
        public float[,] heightMap;
        public bool[,] lakeMask;
    }

    ChunkData GenerateChunkDataWrapped(Vector2Int coord)
    {
        int chunkSize = terrainManager.chunkSize;
        int offsetX = coord.x * chunkSize;
        int offsetZ = coord.y * chunkSize;

        int safeX = chunkSize + 1;
        int safeZ = chunkSize + 1;

        float[,] heightMap = new float[safeX, safeZ];
        bool[,] lakeMask = new bool[safeX, safeZ];

        bool isFlat = true;
        float first = -1f;

        for (int x = 0; x < safeX; x++)
        {
            for (int z = 0; z < safeZ; z++)
            {
                int gx = WrapIndex(offsetX + x + globalWidth / 2, globalWidth);
                int gz = WrapIndex(offsetZ + z + globalDepth / 2, globalDepth);

                float h = globalHeightMap[gx, gz];
                heightMap[x, z] = h;
                lakeMask[x, z] = globalLakeMask[gx, gz];

                if (first < 0f) first = h;
                if (Mathf.Abs(h - first) > 0.001f) isFlat = false;
            }
        }

        if (isFlat) return null;
        return new ChunkData { heightMap = heightMap, lakeMask = lakeMask };
    }

    int WrapIndex(int i, int max)
    {
        return ((i % max) + max) % max;
    }

}
