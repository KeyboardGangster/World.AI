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

        for (int o = 0; o < data.octaves; o++)
        {
            float noise = Mathf.PerlinNoise(
                (x + octaveOffsets[o].x) / data.noiseScale * frequency,
                (y + octaveOffsets[o].y) / data.noiseScale * frequency
            );

            noise = ModifyNoise(noise, halfMaxValue, data.pattern[o]);

            noiseValue += noise * amplitude;

            amplitude *= data.persistance;
            frequency *= data.lacunarity;
        }

        return noiseValue / maxValue * data.heightMultiplier + data.heightAddend;
    }

    public static float CalculateCompoundNoiseValue(int x, int y, Vector2[][] octaveOffsets, BiomeData[] biomes, float[] maxValues, float[] weights)
    {
        float totalWeight = 0;
        float totalNoiseValue = 0;
        float[] halfMaxValues = new float[biomes.Length];

        for (int biomeIndex = 0; biomeIndex < weights.Length; biomeIndex++)
        {
            if (weights[biomeIndex] == 0)
                continue;

            float amplitude = 1;
            float frequency = 1;
            float noiseValue = 0;
            float currentWeight = weights[biomeIndex];

            totalWeight += currentWeight;

            for (int o = 0; o < biomes[biomeIndex].noiseData.octaves; o++)
            {
                float noise = Mathf.PerlinNoise(
                    (x + octaveOffsets[biomeIndex][o].x) / biomes[biomeIndex].noiseData.noiseScale * frequency,
                    (y + octaveOffsets[biomeIndex][o].y) / biomes[biomeIndex].noiseData.noiseScale * frequency
                );

                //precache half-maxValues?
                noise = ModifyNoise(noise, maxValues[biomeIndex] / 2f, biomes[biomeIndex].noiseData.pattern[o]);

                noiseValue += noise * amplitude;

                amplitude *= biomes[biomeIndex].noiseData.persistance;
                frequency *= biomes[biomeIndex].noiseData.lacunarity;
            }

            noiseValue = noiseValue / maxValues[biomeIndex] * biomes[biomeIndex].noiseData.heightMultiplier + biomes[biomeIndex].noiseData.heightAddend;
            totalNoiseValue += noiseValue * currentWeight;
        }

        return totalNoiseValue / totalWeight;
    }

    public static Vector2[] CalculateOctaveOffsets(int x, int y, NoiseData data, out float maxValue)
    {
        Vector2[] octaveOffsets = new Vector2[data.octaves];
        System.Random random = new System.Random(data.seed);
        maxValue = 0;
        float amplitude = 1;

        //Generate set-seed offsets
        for (int i = 0; i < octaveOffsets.Length; i++)
        {
            octaveOffsets[i] = new Vector2(
                random.Next(-10000, 10000) + x - data.offset.x,
                random.Next(-10000, 10000) + y - data.offset.y
            );

            maxValue += amplitude;
            amplitude *= data.persistance;
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
