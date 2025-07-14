using UnityEngine;

[ExecuteInEditMode]
public class TerrainHeightExporter : MonoBehaviour
{
    [Header("Input from TerrainManager")]
    public TerrainManager terrainManager;
    public Material waterMaterial;

    [Header("Texture Settings")]
    public int textureResolution = 512;
    public bool saveToDisk = false;

    void Start()
    {
        BakeAndAssignHeightTexture();
    }

    public void BakeAndAssignHeightTexture()
    {
        if (terrainManager == null || waterMaterial == null)
        {
            Debug.LogError("Missing references.");
            return;
        }

        // Generate global height map again
        (float[,] heightMap, _) = TerrainGeneration.GenerateHeightMap(
            terrainManager.meshWidth + 1,
            terrainManager.meshDepth + 1,
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

        TerrainGeneration.ApplyThermalErosion(ref heightMap, terrainManager.erosionIterations, terrainManager.talus, terrainManager.erosionFactor);
        heightMap = TerrainGeneration.NormalizeHeightMap(heightMap);

        Texture2D heightTex = BakeHeightMapToTexture(heightMap);

        // Apply to shader
        waterMaterial.SetTexture("_TerrainHeightTex", heightTex);
        waterMaterial.SetFloat("_WorldWaterY", terrainManager.worldWaterY);

        Debug.Log("Assigned baked height texture to water material.");
    }

    Texture2D BakeHeightMapToTexture(float[,] heights)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RFloat, false, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float h = Mathf.Clamp01(heights[x, z]);
                tex.SetPixel(x, z, new Color(h, h, h));
            }
        }

        tex.Apply();

#if UNITY_EDITOR
        if (saveToDisk)
        {
            byte[] bytes = tex.EncodeToPNG();
            string path = Application.dataPath + "/TerrainHeightTex_Baked.png";
            System.IO.File.WriteAllBytes(path, bytes);
            Debug.Log("Saved baked height texture to disk: " + path);
        }
#endif

        return tex;
    }
}
