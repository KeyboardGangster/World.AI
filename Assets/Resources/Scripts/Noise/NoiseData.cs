using UnityEngine;

/// <summary>
/// Used during generation. Holds the data needed to create a noise-map.
/// </summary>
[System.Serializable]
public struct NoiseData
{
    public Vector2 offset;
    public int octaves;
    public float lacunarity;
    public float persistance;

    public float heightAddend;
    public float heightMultiplier;
    public float noiseScale;

    public NoisePattern[] pattern;

    public int seed;

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="offset">Custom offset taken into account during generation.</param>
    /// <param name="heightMultiplier">By how much to multiply the noise-map.</param>
    /// <param name="noiseScale">The scale of the noise.</param>
    /// <param name="octaves">The amount of octaves for the generation.</param>
    /// <param name="lacunarity">The lacunarity effecting the frequency for each octave.</param>
    /// <param name="persistance">The persistance effectin the amplitude for each octave.</param>
    /// <param name="seed">The seed with which to calculate internal offsets.</param>
    public NoiseData(Vector2 offset, float heightMultiplier, float heightAddend, float noiseScale, int octaves, float lacunarity, float persistance, int seed)
    {
        this.offset = offset;
        this.octaves = octaves;
        this.lacunarity = lacunarity;
        this.persistance = persistance;
        this.heightAddend = heightAddend;
        this.heightMultiplier = heightMultiplier;
        this.noiseScale = noiseScale;
        this.seed = seed;

        this.pattern = new NoisePattern[octaves];

        for(int i = 0; i < this.pattern.Length; i++)
        {
            this.pattern[i] = NoisePattern.Default;
        }
    }

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="offset">Custom offset taken into account during generation.</param>
    /// <param name="heightMultiplier">By how much to multiply the noise-map.</param>
    /// <param name="noiseScale">The scale of the noise.</param>
    /// <param name="octaves">The amount of octaves for the generation.</param>
    /// <param name="lacunarity">The lacunarity effecting the frequency for each octave.</param>
    /// <param name="persistance">The persistance effectin the amplitude for each octave.</param>
    /// <param name="seed">The seed with which to calculate internal offsets.</param>
    public NoiseData(Vector2 offset, float heightMultiplier, float heightAddend, float noiseScale, int octaves, float lacunarity, float persistance, int seed, NoisePattern[] pattern)
    {
        this.offset = offset;
        this.octaves = octaves;
        this.lacunarity = lacunarity;
        this.persistance = persistance;
        this.heightAddend = heightAddend;
        this.heightMultiplier = heightMultiplier;
        this.noiseScale = noiseScale;
        this.seed = seed;
        this.pattern = pattern;

    }

    /// <summary>
    /// Initializes a new instance of this class, using an existing instance.
    /// </summary>
    /// <param name="n">The existing instance.</param>
    /// <param name="heightAddend">Modification to height. Will be added to n.heightAddend.</param>
    /// <param name="noiseScaleModifier">Modification to noise-scale. Will be added to n.noiseScale.</param>
    /// <param name="seedModifier">Modification to seed. Will be added to n.seed.</param>
    public NoiseData(NoiseData n, float heightAddend, float noiseScaleModifier, int seedModifier)
    {
        this.offset = n.offset;
        this.octaves = n.octaves;
        this.lacunarity = n.lacunarity;
        this.persistance = n.persistance;
        this.heightAddend = n.heightAddend + heightAddend;
        this.heightMultiplier = n.heightMultiplier;
        this.noiseScale = n.noiseScale + noiseScaleModifier;
        this.seed = n.seed + seedModifier;
        this.pattern = n.pattern;
    }
}
