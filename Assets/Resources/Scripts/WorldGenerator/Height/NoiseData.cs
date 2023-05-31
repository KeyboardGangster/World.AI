using UnityEngine;

/// <summary>
/// Used during generation. Holds the data needed to create a noise-map.
/// </summary>
[System.Serializable]
public class NoiseData : HeightData
{
    private SONoise noise;

    private Vector2[] octaveOffsets;
    private float maxValue;

    public NoiseData(SOHeight so) : base(so)
    {
        this.noise = (SONoise) so;
    }

    public override float GetHeight(WorldGeneratorArgs args, int x, int y)
    {
        float amplitude = 1;
        float frequency = 1;
        float noiseValue = 0;
        float halfMaxValue = (maxValue / 2f);

        for (int o = 0; o < this.noise.Octaves; o++)
        {
            float noise = Mathf.PerlinNoise(
                (x + octaveOffsets[o].x) / this.scale * frequency,
                (y + octaveOffsets[o].y) / this.scale * frequency
            );

            noise = ModifyNoise(noise, halfMaxValue, this.noise.Pattern[o]);

            noiseValue += noise * amplitude;

            amplitude *= this.noise.Persistance;
            frequency *= this.noise.Lacunarity;
        }

        return noiseValue / maxValue * this.multiplier + this.addend;
    }

    public override void Prepare(WorldGeneratorArgs args, int x, int y)
    {
        this.scale = this.noise.NoiseScale;
        base.Prepare(args, x, y);

        this.octaveOffsets = new Vector2[this.noise.Octaves];
        System.Random random = new System.Random(args.Seed);
        this.maxValue = 0;
        float amplitude = 1;

        //Generate set-seed offsets
        for (int i = 0; i < this.octaveOffsets.Length; i++)
        {
            this.octaveOffsets[i] = new Vector2(
                random.Next(-10000, 10000) + x - this.noise.Offset.x,
                random.Next(-10000, 10000) + y - this.noise.Offset.y
            );

            this.maxValue += amplitude;
            amplitude *= this.noise.Persistance;
        }
    }

    protected static float ModifyNoise(float noise, float halfMaxValue, NoisePattern p)
    {
        switch (p)
        {
            case NoisePattern.Turbulent:
                noise = Mathf.Abs(2 * noise - 1);
                break;
            case NoisePattern.Ridge:
                noise = Mathf.Abs(2 * noise - 1);
                noise = halfMaxValue - noise;
                noise *= noise;
                break;
            case NoisePattern.Split:
                noise = (2 * noise) % 1f;
                break;
            default:
                break;
        }

        return noise;
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
