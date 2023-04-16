using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGenerator2_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        WorldGenerator worldGen = (WorldGenerator)target;

        if (DrawDefaultInspector())
        {

        }

        if (GUILayout.Button("Sort Biomes"))
            worldGen.SortBiomeData();

        if (GUILayout.Button("Generate"))
            worldGen.Generate();
    }
}
