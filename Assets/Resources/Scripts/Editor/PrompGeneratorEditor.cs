using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PromptGenerator))]
public class PromptGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PromptGenerator promptGenerator = (PromptGenerator)target;

        if (DrawDefaultInspector())
        {

        }

        if (GUILayout.Button("Generate"))
            promptGenerator.Main();
    }
}
