using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGenerator_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        WorldGenerator worldGen = (WorldGenerator)target;

        if (DrawDefaultInspector())
        {

        }

        if (GUILayout.Button("Generate"))
            worldGen.GenerateChunks();

        if (GUILayout.Button("Clear"))
            worldGen.DestroyChunks(false);
    }
}
