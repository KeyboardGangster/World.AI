using UnityEngine;

/// <summary>
/// Used during generation. Holds the data needed to create a noise-map.
/// </summary>
[System.Serializable]
public class NoiseData
{
    public SONoise noise;

    [HideInInspector]
    public float finalNoiseScale;
    [HideInInspector]
    public float finalHeightAddend;
    [HideInInspector]
    public float finalHeightMultiplier;
    
    public void Prepare(float worldScaleRatio, float toyScaleRatio, float waterLevel)
    {
        //args.availableBiomes[i].noiseData = BiomeGenerator.GetBiomeNoiseData(biomes[i].so.biome);

        this.finalNoiseScale = this.noise.NoiseScale;
        this.finalHeightMultiplier = this.noise.HeightMultiplier;
        this.finalHeightAddend = this.noise.HeightAddend;

        this.finalNoiseScale /= worldScaleRatio * toyScaleRatio;
        this.finalHeightAddend = this.noise.HeightAddend / toyScaleRatio;
        this.finalHeightAddend += waterLevel;
        this.finalHeightMultiplier /= toyScaleRatio;
    }

    /*public static NoiseData CreateNew(Vector2 offset, float heightMultiplier, float heightAddend, float noiseScale, int octaves, float lacunarity, float persistance, int seed)
{
    NoiseData data = new NoiseData();
    SONoise so = new SONoise();

    so.octaves = octaves;
    so.lacunarity = lacunarity;
    so.persistance = persistance;
    so.heightMultiplier = heightMultiplier;
    so.heightAddend = heightAddend;
    so.noiseScale = noiseScale;
    so.pattern = new NoisePattern[so.octaves];
    so.seed = seed;

    data.offset = offset;
    data.so = so;
    return data;
}*/
}
