using UnityEngine;

public class TerrainMeshData
{
    public Vector3[] vertices;
    public Vector2[] uvs;
    public int[] triangles;

    private int trianglesIndex;

    public TerrainMeshData(int width, int depth)
    {
        //Vertices are the vector coordinates of each vertex
        vertices = new Vector3[width * depth];

        //uvs tell each vertex where to look on the texture map
        uvs = new Vector2[width * depth];

        //The triangles array is a list of integers that reference the vertices array
        //The "internal" space of the array is (width-1)(depth-1)
        //Each space can fit two triangles and each triangle has three vertices so *6
        triangles = new int[(width - 1) * (depth - 1) * 6];

        trianglesIndex = 0;
    }

    //Given three vertices, append a triangle into the triangles array
    public void AddTriangleToMesh(int v1, int v2, int v3)
    {
        //Add the vertex index 1 to triangles then increase the index. Repeat for v2 and v3
        triangles[trianglesIndex++] = v1;
        triangles[trianglesIndex++] = v2;
        triangles[trianglesIndex++] = v3;
    }
}
