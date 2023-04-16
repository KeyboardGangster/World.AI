using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BiomeData : System.IComparable<BiomeData>
{
    [Header("Biome Type")]
    public Biomes biome;
    public float falloffRate;

    [Header("Biome Population")]
    public Vector2 bias;
    public Vector2 random;

    [HideInInspector]
    public NoiseData noiseData;

    [Header("Biome Details (Should be hidden in the future)")]
    public TerrainLayer baseTerrainLayer;
    public bool overrideSteepTerrainLayer;
    public bool overrideInWaterTerrainLayer;

    public GameObject[] trees;
    public float treePropability;

    public int CompareTo(BiomeData other)
    {
        int compareValue = bias.x.CompareTo(other.bias.x);

        if (compareValue != 0)
            return compareValue;
        else
            return this.random.x.CompareTo(other.random.x);
    }
}
