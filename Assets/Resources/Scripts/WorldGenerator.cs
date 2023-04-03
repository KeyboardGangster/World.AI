using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using static UnityEngine.Mesh;
using UnityEngine.Rendering;

[RequireComponent(typeof(Terrain))]
public class WorldGenerator : MonoBehaviour
{
    private Terrain terrain;

    [SerializeField]
    [Tooltip("1:_ scale.")]
    private float worldScaleRatio = 1f;

    [SerializeField]
    private int seed = 1;
    [SerializeField]
    private float noiseScale = 20;
    [SerializeField]
    private BiomeData[] biomeData;

    [SerializeField]
    private float waterLevel = 100f;


    public void Generate()
    {
        if (this.terrain == null)
            this.terrain = this.GetComponent<Terrain>();

        System.Array.Sort(this.biomeData);
        BiomeData[] biomes = new BiomeData[this.biomeData.Length];

        for (int i = 0; i < this.biomeData.Length; i++)
        {
            biomes[i] = this.biomeData[i];

            biomes[i].noiseData = BiomeGenerator.GetBiomeNoiseData(this.biomeData[i].biome);
            biomes[i].noiseData.noiseScale /= this.worldScaleRatio;
            biomes[i].noiseData.heightMultiplier /= this.worldScaleRatio;
            biomes[i].noiseData.heightAddend = (biomes[i].noiseData.heightAddend + this.waterLevel) / this.worldScaleRatio;
            biomes[i].falloffRate *= this.worldScaleRatio;
        }

        NoiseData biasData = new NoiseData(Vector2.zero, 1, 0, this.noiseScale * 30, 2, 2, 0.5f, this.seed);
        NoiseData randomnessData = new NoiseData(Vector2.one * 64000, 1, 0, this.noiseScale * 15, 2, 2, 0.5f, this.seed);
        biasData.noiseScale /= this.worldScaleRatio;
        randomnessData.noiseScale /= this.worldScaleRatio;

        int size = this.terrain.terrainData.heightmapResolution;

        float[,][] weights = BiomeGenerator.GetBiomeMap(1, 1, size, size, biomes, biasData, randomnessData);
        float[,] heightMap = this.CreateHeightMap(size, weights, biomes);
        this.terrain.terrainData.SetHeights(0, 0, heightMap);

        this.transform.position = new Vector3(0, -this.waterLevel, 0);
    }

    private float[,] CreateHeightMap(int size, float[,][] weights, BiomeData[] biomes)
    {
        float[,] heightMap = new float[size, size];

        Vector2[][] octaveOffsets = new Vector2[biomes.Length][];
        float[] maxValues = new float[biomes.Length];

        float maxTerrainHeight = this.terrain.terrainData.heightmapScale.y;

        //int chunkOffsetX = chunkPos.x * (WIDTH - 1);
        //int chunkOffsetY = chunkPos.y * (HEIGHT - 1);

        for (int i = 0; i < biomes.Length; i++)
            //octaveOffsets[i] = Synthesizer.CalculateOctaveOffsets(chunkOffsetX, chunkOffsetY, biomes[i].noiseData, out maxValues[i]);
            octaveOffsets[i] = Synthesizer.CalculateOctaveOffsets(0, 0, biomes[i].noiseData, out maxValues[i]);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                //float terrainHeight = Synthesizer.CalculateCompoundNoiseValue(x, y, octaveOffsets, biomes, maxValues, weights[chunkOffsetX + x, chunkOffsetY + y]);
                float terrainHeight = Synthesizer.CalculateCompoundNoiseValue(x, y, octaveOffsets, biomes, maxValues, weights[x, y]);
                heightMap[x, y] = terrainHeight / maxTerrainHeight;
            }
        }

        return heightMap;
    }

    private void Awake()
    {
        this.terrain = this.GetComponent<Terrain>();
    }

    private void Start()
    {
        Generate();
    }

    private void OnValidate()
    {
        this.worldScaleRatio = Mathf.Clamp(this.worldScaleRatio, 1, 10);
        this.noiseScale = Mathf.Max(0.01f, this.noiseScale);
    }
}
