using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainManager))]
public class TerrainManagerEditor : Editor
{
    private TerrainManager manager;

    private void OnEnable()
    {
        manager = (TerrainManager)target;
    }

    public override void OnInspectorGUI()
    {
        // Draw all serialized fields (your terrain/mesh/erosion settings)
        DrawDefaultInspector();

        // Always show the Generate Terrain button
        if (GUILayout.Button("Generate Terrain"))
        {
            manager.GenerateTerrain();
        }
    }
}
