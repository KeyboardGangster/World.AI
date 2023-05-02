using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TreeData
{
    public SOTree tree;

    [Range(0f, 1f)]
    public float spawnRate;
}
