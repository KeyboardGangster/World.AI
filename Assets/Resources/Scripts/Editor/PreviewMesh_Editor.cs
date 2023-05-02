using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(PreviewMesh))]
public class PreviewMesh_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        PreviewMesh previewMesh = (PreviewMesh)target;

        if (DrawDefaultInspector() && previewMesh.autoUpdate)
        {
            previewMesh.Preview();
        }

        if (GUILayout.Button("Preview"))
            previewMesh.Preview();

        if (GUILayout.Button("Add Collider"))
            previewMesh.AddColliders();

        if (GUILayout.Button("Clear"))
            previewMesh.DestroyChunks(false);

        if (GUILayout.Button("Print"))
            previewMesh.PrintNoiseData();
    }
}
