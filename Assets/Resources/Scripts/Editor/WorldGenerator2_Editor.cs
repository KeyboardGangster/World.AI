using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldGenerator2))]
public class WorldGenerator2_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        WorldGenerator2 worldGen = (WorldGenerator2)target;

        if (DrawDefaultInspector())
        {

        }

        if (GUILayout.Button("Generate"))
            worldGen.Generate();
    }
}
