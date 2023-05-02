using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BiomeData : System.IComparable<BiomeData>
{
    public SOBiome biome;

    [Header("Biome Distribution")]
    public Vector2 bias;
    public Vector2 random;

    [HideInInspector]
    public float finalFalloffRate;
    [HideInInspector]
    public float finalGrassNoiseScale;

    public void Prepare(float worldScaleRatio, float toyScaleRatio)
    {
        this.finalFalloffRate = this.biome.FalloffRate;
        this.finalFalloffRate *= worldScaleRatio * toyScaleRatio;
        this.finalGrassNoiseScale = this.biome.GrassNoiseScale;
        this.finalGrassNoiseScale /= worldScaleRatio * toyScaleRatio;
    }

    public int CompareTo(BiomeData other)
    {
        int compareValue = bias.x.CompareTo(other.bias.x);

        if (compareValue != 0)
            return compareValue;
        else
            return this.random.x.CompareTo(other.random.x);
    }
}
