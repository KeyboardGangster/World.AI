using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldGeneratorInterface), true)]
public class WorldGeneratorInterface_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        WorldGeneratorInterface worldGen = (WorldGeneratorInterface)target;

        if (DrawDefaultInspector())
        {

        }

        if (GUILayout.Button("Generate"))
            worldGen.GenerateWorld();
    }
}
