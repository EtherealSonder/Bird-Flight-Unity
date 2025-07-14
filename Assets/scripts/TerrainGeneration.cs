using System.Collections.Generic;
using UnityEngine;

public static class TerrainGeneration
{
    public static (float[,], int[,]) GenerateHeightMap(
    int width, int depth, float resolution,
    int numOctaves, float lacunarity, float persistance, int seed,
    float verticalScale,
    AnimationCurve meshHeightCurve,
    AnimationCurve waterCurve, AnimationCurve plainsCurve, AnimationCurve mountainCurve,
    float regionNoiseScale, // NEW PARAMETER
    float waterThresholdFlat, float waterThresholdMountain,
    float plainsThresholdFlat, float plainsThresholdMountain,
    int offsetX = 0, int offsetZ = 0)
    {
        float[,] terrainHeightArray = new float[width, depth];
        int[,] biomeMap = new int[width, depth];
        float[,] falloffMask = GenerateRadialFalloff(width, depth);

        resolution = Mathf.Max(resolution, 0.001f);
        float maxNoiseVal = float.MinValue;
        float minNoiseVal = float.MaxValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                float frequency = 1f;
                float amplitude = 1f;
                float totalNoise = 0f;
                float octaveOffset = 0f;

                for (int octave = 0; octave < numOctaves; octave++)
                {
                    float x = ((i + offsetX) / resolution) * frequency + seed + octaveOffset * width;
                    float y = ((j + offsetZ) / resolution) * frequency + seed + octaveOffset * depth;

                    totalNoise += (Mathf.PerlinNoise(x, y) * 2 - 1) * amplitude;

                    frequency *= lacunarity;
                    amplitude *= persistance;
                    octaveOffset++;
                }

                totalNoise -= falloffMask[i, j];
                terrainHeightArray[i, j] = totalNoise;

                if (totalNoise > maxNoiseVal) maxNoiseVal = totalNoise;
                if (totalNoise < minNoiseVal) minNoiseVal = totalNoise;
            }
        }

        float range = maxNoiseVal - minNoiseVal;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                float normalized = (terrainHeightArray[i, j] - minNoiseVal) / range;
                float shaped = meshHeightCurve.Evaluate(normalized);
                float worldY = shaped * verticalScale;

                // Sample low-frequency region noise
                float regionNoise = Mathf.PerlinNoise((i + seed) * regionNoiseScale, (j + seed) * regionNoiseScale);
                float flatBias = regionNoise; // 0 = mountain-prone, 1 = flat-prone

                // Blend thresholds based on region
                float waterThreshold = Mathf.Lerp(waterThresholdMountain, waterThresholdFlat, flatBias);
                float plainsThreshold = Mathf.Lerp(plainsThresholdMountain, plainsThresholdFlat, flatBias);

                // Assign biome based on blended thresholds
                if (worldY < waterThreshold)
                {
                    biomeMap[i, j] = 0;
                    shaped = waterCurve.Evaluate(normalized);
                }
                else if (worldY < plainsThreshold)
                {
                    biomeMap[i, j] = 1;
                    shaped = plainsCurve.Evaluate(normalized);
                }
                else
                {
                    biomeMap[i, j] = 2;
                    shaped = mountainCurve.Evaluate(normalized);
                }

                terrainHeightArray[i, j] = shaped;
            }
        }

        return (terrainHeightArray, biomeMap);
    }



    public static float[,] GenerateRadialFalloff(int width, int depth)
    {
        float[,] mask = new float[width, depth];
        float centerX = width / 2f;
        float centerY = depth / 2f;
        float maxDist = Mathf.Sqrt(centerX * centerX + centerY * centerY);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < depth; y++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float normDist = dist / maxDist;
                mask[x, y] = Mathf.Pow(normDist, 2.5f);
            }
        }

        return mask;
    }

    public static float[,] NormalizeHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int depth = heightMap.GetLength(1);

        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                if (heightMap[i, j] < min) min = heightMap[i, j];
                if (heightMap[i, j] > max) max = heightMap[i, j];
            }
        }

        float range = max - min;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                heightMap[i, j] = (heightMap[i, j] - min) / range;
            }
        }

        return heightMap;
    }

    public static Mesh GenerateMesh(float[,] terrainHeights, float verticalScale, AnimationCurve meshHeightCurve)
    {
        int terrainWidth = terrainHeights.GetLength(0);
        int terrainDepth = terrainHeights.GetLength(1);

        float widthOffset = (terrainWidth - 1) / 2f;
        float depthOffset = (terrainDepth - 1) / 2f;

        TerrainMeshData meshData = new TerrainMeshData(terrainWidth, terrainDepth);

        for (int i = 0; i < meshData.vertices.Length; i++)
        {
            int row = i / terrainWidth;
            int column = i % terrainWidth;

            float height = verticalScale * meshHeightCurve.Evaluate(terrainHeights[column, row]);

            meshData.vertices[i] = new Vector3(
                column - widthOffset,
                height,
                row - depthOffset
            );

            meshData.uvs[i] = new Vector2((float)column / (terrainWidth - 1), (float)row / (terrainDepth - 1));

            if (column < terrainWidth - 1 && row < terrainDepth - 1)
            {
                meshData.AddTriangleToMesh(i, i + terrainWidth + 1, i + 1);
                meshData.AddTriangleToMesh(i, i + terrainWidth, i + terrainWidth + 1);
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = meshData.vertices;
        mesh.uv = meshData.uvs;
        mesh.triangles = meshData.triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    public static bool[,] GenerateLakeMask(float[,] heightMap, float lakeThreshold)
    {
        int width = heightMap.GetLength(0);
        int depth = heightMap.GetLength(1);

        bool[,] lakeMask = new bool[width, depth];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (heightMap[x, z] < lakeThreshold)
                    lakeMask[x, z] = true;
            }
        }

        return lakeMask;
    }

    public static Mesh GenerateFlatWaterMesh(bool[,] lakeMask, float baseHeight)
    {
        int width = lakeMask.GetLength(0);
        int height = lakeMask.GetLength(1);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float offsetX = (width - 1) / 2f;
        float offsetZ = (height - 1) / 2f;

        for (int x = 0; x < width - 1; x++)
        {
            for (int z = 0; z < height - 1; z++)
            {
                if (lakeMask[x, z] && lakeMask[x + 1, z] && lakeMask[x, z + 1] && lakeMask[x + 1, z + 1])
                {
                    Vector3 v00 = new Vector3(x - offsetX, baseHeight, z - offsetZ);
                    Vector3 v10 = new Vector3(x + 1 - offsetX, baseHeight, z - offsetZ);
                    Vector3 v01 = new Vector3(x - offsetX, baseHeight, z + 1 - offsetZ);
                    Vector3 v11 = new Vector3(x + 1 - offsetX, baseHeight, z + 1 - offsetZ);

                    int startIndex = vertices.Count;

                    vertices.Add(v00); // 0
                    vertices.Add(v10); // 1
                    vertices.Add(v01); // 2
                    vertices.Add(v11); // 3

                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 1));

                    triangles.Add(startIndex + 0);
                    triangles.Add(startIndex + 2);
                    triangles.Add(startIndex + 1);

                    triangles.Add(startIndex + 1);
                    triangles.Add(startIndex + 2);
                    triangles.Add(startIndex + 3);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        return mesh;
    }


    public static void ApplyThermalErosion(ref float[,] heightMap, int iterations, float talus = 0.02f, float erosionFactor = 0.5f)
    {
        int width = heightMap.GetLength(0);
        int depth = heightMap.GetLength(1);

        for (int it = 0; it < iterations; it++)
        {
            float[,] newHeights = (float[,])heightMap.Clone();

            for (int x = 1; x < width - 1; x++)
            {
                for (int z = 1; z < depth - 1; z++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0) continue;

                            int nx = x + dx;
                            int nz = z + dz;

                            float delta = heightMap[x, z] - heightMap[nx, nz];

                            if (delta > talus)
                            {
                                float transfer = erosionFactor * (delta - talus);
                                newHeights[x, z] -= transfer;
                                newHeights[nx, nz] += transfer;
                            }
                        }
                    }
                }
            }

            heightMap = newHeights;
        }
    }


   



}
