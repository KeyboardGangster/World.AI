using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BiomeData : System.IComparable<BiomeData>
{
    public string name;

    public Vector2 bias;
    public Vector2 random;
    public float falloffRate;
    public AnimationCurve weightBias;

    public Color feature;
    public Biomes biome;
    public NoiseData noiseData;

    public TerrainLayer baseTerrainLayer;
    public bool overrideSteepTerrainLayer;
    public bool overrideInWaterTerrainLayer;

    public int CompareTo(BiomeData other)
    {
        int compareValue = bias.x.CompareTo(other.bias.x);

        if (compareValue != 0)
            return compareValue;
        else
            return this.random.x.CompareTo(other.random.x);
    }
}
