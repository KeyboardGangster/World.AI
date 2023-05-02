using System.Collections.Generic;
using UnityEngine;

public class Synthesizer
{
    public static float CalculateNoiseValue(int x, int y, Vector2[] octaveOffsets, NoiseData data, float maxValue)
    {
        float amplitude = 1;
        float frequency = 1;
        float noiseValue = 0;
        float halfMaxValue = (maxValue / 2f);

        for (int o = 0; o < data.noise.Octaves; o++)
        {
            float noise = Mathf.PerlinNoise(
                (x + octaveOffsets[o].x) / data.finalNoiseScale * frequency,
                (y + octaveOffsets[o].y) / data.finalNoiseScale * frequency
            );

            noise = ModifyNoise(noise, halfMaxValue, data.noise.Pattern[o]);

            noiseValue += noise * amplitude;

            amplitude *= data.noise.Persistance;
            frequency *= data.noise.Lacunarity;
        }

        return noiseValue / maxValue * data.finalHeightMultiplier + data.finalHeightAddend;
    }

    public static float CalculateCompoundNoiseValue(WorldGeneratorArgs args, int x, int y, Vector2[][] octaveOffsets, float[] maxValues)
    {
        float totalWeight = 0;
        float totalNoiseValue = 0;

        for (int biomeIndex = 0; biomeIndex < args.BiomeCount; biomeIndex++)
        {
            if (args.GetWeight(x, y, biomeIndex) == 0)
                continue;

            float amplitude = 1;
            float frequency = 1;
            float noiseValue = 0;
            float currentWeight = args.GetWeight(x, y, biomeIndex);

            totalWeight += currentWeight;

            for (int o = 0; o < args.GetBiome(biomeIndex).biome.NoiseData.noise.Octaves; o++)
            {
                float noise = Mathf.PerlinNoise(
                    (x + octaveOffsets[biomeIndex][o].x) / args.GetBiome(biomeIndex).biome.NoiseData.finalNoiseScale * frequency,
                    (y + octaveOffsets[biomeIndex][o].y) / args.GetBiome(biomeIndex).biome.NoiseData.finalNoiseScale * frequency
                );

                //precache half-maxValues?
                noise = ModifyNoise(noise, maxValues[biomeIndex] / 2f, args.GetBiome(biomeIndex).biome.NoiseData.noise.Pattern[o]);

                noiseValue += noise * amplitude;

                amplitude *= args.GetBiome(biomeIndex).biome.NoiseData.noise.Persistance;
                frequency *= args.GetBiome(biomeIndex).biome.NoiseData.noise.Lacunarity;
            }

            noiseValue = noiseValue / maxValues[biomeIndex] * args.GetBiome(biomeIndex).biome.NoiseData.finalHeightMultiplier + args.GetBiome(biomeIndex).biome.NoiseData.finalHeightAddend;
            totalNoiseValue += noiseValue * currentWeight;
        }

        return totalNoiseValue / totalWeight;
    }

    public static Vector2[] CalculateOctaveOffsets(int x, int y, int seed, NoiseData data, out float maxValue)
    {
        Vector2[] octaveOffsets = new Vector2[data.noise.Octaves];
        System.Random random = new System.Random(seed);
        maxValue = 0;
        float amplitude = 1;

        //Generate set-seed offsets
        for (int i = 0; i < octaveOffsets.Length; i++)
        {
            octaveOffsets[i] = new Vector2(
                random.Next(-10000, 10000) + x - data.noise.Offset.x,
                random.Next(-10000, 10000) + y - data.noise.Offset.y
            );

            maxValue += amplitude;
            amplitude *= data.noise.Persistance;
        }

        return octaveOffsets;
    }

    private static float ModifyNoise(float noise, float halfMaxValue, NoisePattern p)
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
}
