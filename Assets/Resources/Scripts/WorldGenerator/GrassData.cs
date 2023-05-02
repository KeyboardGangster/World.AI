using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GrassData
{
    public SOGrass grass;

    [Range(0f, 1f)]
    public float spawnRate;
    [Range(0, 5)]
    public int density;
    public bool biased;
}
