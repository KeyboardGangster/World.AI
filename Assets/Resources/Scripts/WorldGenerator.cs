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
    private Dictionary<TerrainLayer, int> terrainLayersIndices;
    [SerializeField]
    private TerrainLayer slopeTerrainLayer;
    [SerializeField]
    private TerrainLayer inWaterTerrainLayer;

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
        List<TerrainLayer> terrainLayers = new List<TerrainLayer>();
        this.terrainLayersIndices = new Dictionary<TerrainLayer, int>();

        int terrainLayerIndex = 0;
        terrainLayerIndex = this.AddTerrainLayer(terrainLayers, this.slopeTerrainLayer, terrainLayerIndex);
        terrainLayerIndex = this.AddTerrainLayer(terrainLayers, this.inWaterTerrainLayer, terrainLayerIndex);

        for (int i = 0; i < this.biomeData.Length; i++)
        {
            biomes[i] = this.biomeData[i];

            biomes[i].noiseData = BiomeGenerator.GetBiomeNoiseData(this.biomeData[i].biome);
            biomes[i].noiseData.noiseScale /= this.worldScaleRatio;
            biomes[i].noiseData.heightMultiplier /= this.worldScaleRatio;
            biomes[i].noiseData.heightAddend = (biomes[i].noiseData.heightAddend / this.worldScaleRatio) + this.waterLevel;
            biomes[i].falloffRate *= this.worldScaleRatio;
            terrainLayerIndex = this.AddTerrainLayer(terrainLayers, biomes[i].baseTerrainLayer, terrainLayerIndex);
        }


        NoiseData biasData = new NoiseData(Vector2.zero, 1, 0, this.noiseScale * 30, 2, 2, 0.5f, this.seed);
        NoiseData randomnessData = new NoiseData(Vector2.one * 64000, 1, 0, this.noiseScale * 15, 2, 2, 0.5f, this.seed);
        biasData.noiseScale /= this.worldScaleRatio;
        randomnessData.noiseScale /= this.worldScaleRatio;

        int size = this.terrain.terrainData.heightmapResolution;

        float[,][] weights = BiomeGenerator.GetBiomeMap(1, 1, size, size, biomes, biasData, randomnessData, out int[,] dominantBiomes);

        float[,] heightMap = this.CreateHeightMap(size, weights, biomes);
        this.terrain.terrainData.SetHeights(0, 0, heightMap);

        this.terrain.terrainData.terrainLayers = terrainLayers.ToArray();
        float[,,] alphaMap = this.CreateAlphaMap(heightMap, weights, biomes, dominantBiomes);
        this.terrain.terrainData.SetAlphamaps(0, 0, alphaMap);

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

    private float[,,] CreateAlphaMap(float[,] heightMap, float[,][] weights, BiomeData[] biomes, int[,] dominantBiomes)
    {
        int heightMapRes = this.terrain.terrainData.heightmapResolution;
        int alphaMapRes = this.terrain.terrainData.alphamapResolution;
        float heightMapAlphaMapRatio = heightMapRes / (float)alphaMapRes;

        float[,,] alphaMap = new float[alphaMapRes, alphaMapRes, this.terrainLayersIndices.Count];
        float waterLevel = this.waterLevel / this.terrain.terrainData.heightmapScale.y;

        for (int y = 0; y < alphaMapRes; y++)
        {
            for(int x = 0; x < alphaMapRes; x++)
            {
                int xInHeightMap = Mathf.FloorToInt(x * heightMapAlphaMapRatio);
                int yInHeightMap = Mathf.FloorToInt(y * heightMapAlphaMapRatio);

                BiomeData dominantBiome = biomes[dominantBiomes[xInHeightMap, yInHeightMap]];

                bool isSteep = false;
                float steepnessValue = 0;
                bool isUnderWater = false;

                if (!dominantBiome.overrideSteepTerrainLayer)
                {
                    //Fuck this undocumented shit, inverting coords works somehow.
                    var angle = this.terrain.terrainData.GetSteepness(
                    (float)y / (this.terrain.terrainData.alphamapHeight - 1),
                    (float)x / (this.terrain.terrainData.alphamapWidth - 1));

                    if (angle > 30f)
                    {
                        isSteep = true;
                        steepnessValue = (float)(angle / 90.0);
                        alphaMap[x, y, this.terrainLayersIndices[this.slopeTerrainLayer]] = steepnessValue;
                    }
                }
                if (!isSteep && !dominantBiome.overrideInWaterTerrainLayer && heightMap[xInHeightMap, yInHeightMap] < waterLevel)
                {
                    alphaMap[x, y, this.terrainLayersIndices[this.inWaterTerrainLayer]] = 1f;
                }
                if (!isUnderWater)
                {
                    for (int i = 0; i < biomes.Length; i++)
                    {
                        alphaMap[x, y, this.terrainLayersIndices[biomes[i].baseTerrainLayer]] += weights[xInHeightMap, yInHeightMap][i] - steepnessValue;
                    }
                }
            }
        }

        return alphaMap;
    }

    private int AddTerrainLayer(List<TerrainLayer> terrainLayers, TerrainLayer tl, int terrainLayerIndex)
    {
        if (!this.terrainLayersIndices.ContainsKey(tl))
        {
            TerrainLayer newTl = Instantiate(tl);
            newTl.tileSize /= this.worldScaleRatio;
            terrainLayers.Add(newTl);
            this.terrainLayersIndices.Add(tl, terrainLayerIndex++);
        }

        return terrainLayerIndex;
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
