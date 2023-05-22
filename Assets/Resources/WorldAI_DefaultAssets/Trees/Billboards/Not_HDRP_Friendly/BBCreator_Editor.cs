using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BBCreator))]
public class BBCreator_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        BBCreator instance = (BBCreator)target;

        if (DrawDefaultInspector())
        {

        }

        if (GUILayout.Button("Create Billboard") && instance.billBoardMaterial != null)
            instance.CreateBillboard();
    }
}
