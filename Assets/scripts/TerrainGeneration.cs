using UnityEngine;

//Static classes are unable to have instances. They are useful when the class doesn't need to get or 
// set its own fields. Statics have only one instance and that is the main class'
public static class TerrainGeneration
{
    /*public static float[,] GenerateHeightMap(int width, int depth, float resolution,
                                         int numOctaves, float lacunarity, float persistance,
                                         int seed)

    {
        //Create an empty 2D array to store the height values
        float[,] terrainHeightArray = new float[width, depth];

        //Make sure the resolution is not 0 or less
        resolution = Mathf.Max(resolution, 0.001f);

        //Keep track of the "highest" and "lowest" point
        float maxNoiseVal = float.MinValue;
        float minNoiseVal = float.MaxValue;
        //(max is set to min value so the fisrt value checked is definitely larger than it and similar for min)

        //Iterate through the array and assign each point to some noise
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < depth; j++) {
                
                Each octave (layer of noise) will have a fequency and amplitude. We start these values at 
                1 and then each octave we multiply by the lacunarity and persistance respectively. This will 
                make each layer of noise have higher frequency (more rapid changes) but lower influence on 
                the total noise. This is provided that  lacunarity > 1, 0 < persistance < 1. 
                
                float frequency = 1f;
                float amplitude = 1f;

                //We will accumulate the noise from each octave in this variable
                float totalNoise = 0;

                //We want each octave to sample from a differnt location
                float octaveOffset = 0;

                for (int octave = 0; octave < numOctaves; octave++) {
                    //We don't want to use the integers i and j as we want the gradual changes that come 
                    //from values close to one another in perlin noise.
                    //Frequency controls how far apart our sampling points are (higher means more rapid change)
                    float x = (i / resolution) * frequency + seed + octaveOffset * width;
                    float y = (j / resolution) * frequency + seed + octaveOffset * depth;


                    //We want the noise to be between -1 and 1 to add OR subtract from the height
                    totalNoise += (Mathf.PerlinNoise(x, y) * 2 - 1) * amplitude;

                    //Increase the frquency and amplitude for the next octave
                    frequency *= lacunarity;
                    amplitude *= persistance;

                    //Increase octave offset for the next sanpling layer
                    octaveOffset++;
                }

                //Check if this is the highest or lowest point yet
                if (totalNoise > maxNoiseVal) {
                    maxNoiseVal = totalNoise;
                } else if (totalNoise < minNoiseVal) {
                    minNoiseVal = totalNoise;
                }

                //Assign the noise to the array
                terrainHeightArray[i, j] = totalNoise;
            }
        }

        //We now want to normalize the array to have a traditional noise result from 0 to 1;
        float terrainHeightRange = maxNoiseVal - minNoiseVal;

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < depth; j++) {
                //Shift the values to a range 0 <-> range then divide by range
                terrainHeightArray[i, j] = (terrainHeightArray[i, j] - minNoiseVal) / terrainHeightRange;
            }
        }
        
        //Return the noise values
        return terrainHeightArray;
    }*/


    public static float[,] GenerateHeightMap(int width, int depth, float resolution,
                                         int numOctaves, float lacunarity, float persistance,
                                         int seed, int offsetX = 0, int offsetZ = 0)
    {
        float[,] terrainHeightArray = new float[width, depth];

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

                if (totalNoise > maxNoiseVal) maxNoiseVal = totalNoise;
                if (totalNoise < minNoiseVal) minNoiseVal = totalNoise;

                terrainHeightArray[i, j] = totalNoise;
            }
        }

        float range = maxNoiseVal - minNoiseVal;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                terrainHeightArray[i, j] = (terrainHeightArray[i, j] - minNoiseVal) / range;
            }
        }

        return terrainHeightArray;
    }

    public static float[,] NormalizeHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int depth = heightMap.GetLength(1);

        float min = float.MaxValue;
        float max = float.MinValue;

        // Find min and max values
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                if (heightMap[i, j] < min) min = heightMap[i, j];
                if (heightMap[i, j] > max) max = heightMap[i, j];
            }
        }

        float range = max - min;

        // Normalize
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
