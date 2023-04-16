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
    private static int BASE_HEIGHTMAP_RESOLUTION = 1024;
    private static int BASE_CONTROLMAP_RESOLUTION = 512;
    private static int BASE_TEXTURE_RESOLUTION = 1024;
    private static int BASE_TERRAIN_WIDTH = 1000;
    private static int BASE_TERRAIN_LENGTH = 1000;
    private static int BASE_TERRAIN_HEIGHT = 600;

    private Terrain terrain;
    private Dictionary<TerrainLayer, int> terrainLayersIndices;
    [SerializeField]
    private TerrainLayer slopeTerrainLayer;
    [SerializeField]
    private TerrainLayer inWaterTerrainLayer;

    [SerializeField]
    [Tooltip("Bigger Worlds, less detail. (1:x scale).")]
    private float worldScaleRatio = 1f;
    [SerializeField]
    [Tooltip("Bigger Worlds by downsizing everything. (1:x scale)")]
    private float toyScaleRatio = 1f;

    [SerializeField]
    private int seed = 1;
    [SerializeField]
    private float noiseScale = 20;
    [SerializeField]
    private BiomeData[] biomeData;

    [SerializeField]
    private float waterLevel = 100f;

    public void SortBiomeData() => System.Array.Sort(this.biomeData);

    public void Generate()
    {
        if (this.terrain == null)
            this.terrain = this.GetComponent<Terrain>();

        /*
         * As of yet, scaling the heightmap to something different then BASE_HEIGHTMAP_RESOLUTION causes problems with the noise-scaling and such.
         * Gotta fix that first before continuing with this.
         * 
        //this.terrain.terrainData.heightmapResolution = BASE_HEIGHTMAP_RESOLUTION * Mathf.Max(1, Mathf.FloorToInt(this.worldScaleRatio / 5f));
        //this.terrain.terrainData.alphamapResolution = BASE_CONTROLMAP_RESOLUTION * Mathf.Max(1, Mathf.FloorToInt(this.worldScaleRatio / 5f));
        //this.terrain.terrainData.baseMapResolution = BASE_TEXTURE_RESOLUTION * Mathf.Max(1, Mathf.FloorToInt(this.worldScaleRatio / 5f));
        */
        this.terrain.terrainData.heightmapResolution = BASE_HEIGHTMAP_RESOLUTION;
        this.terrain.terrainData.alphamapResolution = BASE_CONTROLMAP_RESOLUTION;
        this.terrain.terrainData.baseMapResolution = BASE_TEXTURE_RESOLUTION;
        this.terrain.terrainData.size = new Vector3(BASE_TERRAIN_WIDTH * this.worldScaleRatio, BASE_TERRAIN_HEIGHT, BASE_TERRAIN_LENGTH * this.worldScaleRatio);

        this.SortBiomeData();

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
            biomes[i].noiseData.seed = this.seed;
            biomes[i].noiseData.noiseScale /= this.worldScaleRatio * this.toyScaleRatio;
            biomes[i].noiseData.heightMultiplier /= this.toyScaleRatio;
            biomes[i].noiseData.heightAddend = (biomes[i].noiseData.heightAddend / this.toyScaleRatio);
            biomes[i].noiseData.heightAddend += this.waterLevel;
            biomes[i].falloffRate *= this.worldScaleRatio * this.toyScaleRatio;
            terrainLayerIndex = this.AddTerrainLayer(terrainLayers, biomes[i].baseTerrainLayer, terrainLayerIndex);
        }

        NoiseData biasData = new NoiseData(Vector2.zero, 1, 0, this.noiseScale * 30, 2, 2, 0.5f, this.seed);
        NoiseData randomnessData = new NoiseData(Vector2.one * 64000, 1, 0, this.noiseScale * 15, 2, 2, 0.5f, this.seed + 1);
        biasData.noiseScale /= this.worldScaleRatio * this.toyScaleRatio;
        randomnessData.noiseScale /= this.worldScaleRatio * this.toyScaleRatio;

        int size = this.terrain.terrainData.heightmapResolution;

        float[,][] weights = BiomeGenerator.GetBiomeMap(1, 1, size, size, biomes, biasData, randomnessData, out int[,] dominantBiomes);

        float[,] heightMap = this.CreateHeightMap(size, weights, biomes);
        this.terrain.terrainData.SetHeights(0, 0, heightMap);

        this.terrain.terrainData.terrainLayers = terrainLayers.ToArray();
        float[,,] alphaMap = this.CreateAlphaMap(heightMap, weights, biomes, dominantBiomes);
        this.terrain.terrainData.SetAlphamaps(0, 0, alphaMap);

        EntityPlacer.PlaceTrees(this.terrain, this.worldScaleRatio, this.toyScaleRatio, biomes, dominantBiomes);

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
        int heightmapRes = this.terrain.terrainData.heightmapResolution;
        int alphamapRes = this.terrain.terrainData.alphamapResolution;
        float heightmapAlphamapRatio = heightmapRes / (float)alphamapRes;

        float[,,] alphaMap = new float[alphamapRes, alphamapRes, this.terrainLayersIndices.Count];
        float waterLevel = this.waterLevel / this.terrain.terrainData.heightmapScale.y;

        for (int y = 0; y < alphamapRes; y++)
        {
            for(int x = 0; x < alphamapRes; x++)
            {
                int xInHeightMap = Mathf.FloorToInt(x * heightmapAlphamapRatio);
                int yInHeightMap = Mathf.FloorToInt(y * heightmapAlphamapRatio);

                BiomeData dominantBiome = biomes[dominantBiomes[xInHeightMap, yInHeightMap]];

                bool isSteep = false;
                float steepnessValue = 0;
                bool isUnderWater = false;

                //Set slopeTerrainLayer if biome doesn't supress it.
                if (!dominantBiome.overrideSteepTerrainLayer)
                {
                    //Don't ask me why it's inverted in there, I have no idea haha.
                    //I think the normalized positions are inverted to the actual heightmap-positions but idk...
                    var angle = this.terrain.terrainData.GetSteepness(
                    (float)y / (alphamapRes - 1),
                    (float)x / (alphamapRes - 1));

                    if (angle > 30f)
                    {
                        isSteep = true;
                        steepnessValue = (float)(angle / 90.0);
                        alphaMap[x, y, this.terrainLayersIndices[this.slopeTerrainLayer]] = steepnessValue;
                    }
                }
                //Set inWaterTerrainLayer if biome doesn't supress it.
                if (!isSteep && !dominantBiome.overrideInWaterTerrainLayer && heightMap[xInHeightMap, yInHeightMap] < waterLevel)
                {
                    alphaMap[x, y, this.terrainLayersIndices[this.inWaterTerrainLayer]] = 1f;
                }
                //Set biome-specific terrainLayer.
                if (!isUnderWater)
                {
                    for (int i = 0; i < biomes.Length; i++)
                    {
                        if (this.terrainLayersIndices[biomes[i].baseTerrainLayer] != this.terrainLayersIndices[this.slopeTerrainLayer])
                            alphaMap[x, y, this.terrainLayersIndices[biomes[i].baseTerrainLayer]] += weights[xInHeightMap, yInHeightMap][i] - steepnessValue;
                        else
                            alphaMap[x, y, this.terrainLayersIndices[biomes[i].baseTerrainLayer]] += weights[xInHeightMap, yInHeightMap][i];
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
            newTl.tileSize /= this.toyScaleRatio;
            terrainLayers.Add(newTl);
            this.terrainLayersIndices.Add(tl, terrainLayerIndex++);
        }

        return terrainLayerIndex;
    }

    private void Start()
    {
        this.terrain = this.GetComponent<Terrain>();
        Generate();
    }

    private void OnValidate()
    {
        //this.worldScaleRatio = Mathf.Clamp(this.worldScaleRatio, 1, 100);

        /*
         * Terrain can go up to 1:100 worldScaleRatio, though generating that in one go is most likely going to crash unity.
         * Issue's the entity-placer doing BASE_ITERATIONS*100*100 iterations. Basically too many trees for one terrain I guess.
         */
        this.worldScaleRatio = Mathf.Clamp(this.worldScaleRatio, 1, 20);

        this.toyScaleRatio = Mathf.Clamp(this.toyScaleRatio, 1, 20);

        this.noiseScale = Mathf.Max(0.01f, this.noiseScale);
    }
}
