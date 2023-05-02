using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PropagationData
{
    public SORock[] rocks;

    [Range(0f, 1f)]
    public float spawnRate;

    public float startingSize;
    [Range(0f, 1f)]
    public float sizeDecrease;
    //public float reach;
    [Range(1, 5f)]
    public int minIterations;
    [Range(1, 5f)]
    public int maxIterations;

    public PropagationData()
    {
        spawnRate = 0.3f;
        startingSize = 8f;
        sizeDecrease = 0.6f;
        minIterations = 2;
        maxIterations = 3;
    }
}
