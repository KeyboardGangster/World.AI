using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using static UnityEngine.Mesh;
using UnityEngine.Rendering;

[RequireComponent(typeof(Terrain), typeof(BiomeVolumesBlender))]
public class WorldGenerator : MonoBehaviour
{
    private static int BASE_HEIGHTMAP_RESOLUTION = 1024;
    private static int BASE_CONTROLMAP_RESOLUTION = 512;
    private static int BASE_TEXTURE_RESOLUTION = 1024;
    private static int BASE_DETAIL_RESOLUTION = 2048;
    private static int BASE_DETAIL_RESOLUTION_PER_PATCH = 32;
    private static int BASE_TERRAIN_WIDTH = 1000;
    private static int BASE_TERRAIN_LENGTH = 1000;
    private static int BASE_TERRAIN_HEIGHT = 600;
    private static bool DRAW_INSTANCED = true;

    private WorldGeneratorArgs args;

    [SerializeField]
    private BiomeData[] biomeData = new BiomeData[]
    {
        new BiomeData() { bias = new Vector2(0.0f, 0.5f), random = new Vector2(0.0f, 0.5f) },
        new BiomeData() { bias = new Vector2(0.0f, 0.5f), random = new Vector2(0.5f, 1.0f) },
        new BiomeData() { bias = new Vector2(0.5f, 1.0f), random = new Vector2(0.0f, 1.0f) }
    };
    [SerializeField]
    private WorldSize size = WorldSize.Large;
    [SerializeField]
    private int seed = 1;

    private NoiseData[] biomeNoise;
    private Terrain terrain;
    private TerrainLayer slopeTerrainLayer;
    private TerrainLayer inWaterTerrainLayer;

    //Bigger Worlds, less detail. (1:x scale)
    private float worldScaleRatio;
    //Bigger Worlds by downsizing everything. (1:x scale)
    private float toyScaleRatio;
    //Biome-scale
    private float noiseScale;
    //At which height to draw with in-water-texture. Terrain-height adjusts automatically to this.
    private float waterLevel;

    public void SortBiomeData() => System.Array.Sort(this.biomeData);

    public void Generate()
    {
        this.Prepare();
        CreateBiomes(this.args);
        CreateHeightMap(this.args);
        CreateAlphaMap(this.args, this.slopeTerrainLayer, this.inWaterTerrainLayer);
        CreateEntities(this.args);
    }

    private void Prepare()
    {
        if (this.terrain == null)
            this.terrain = this.GetComponent<Terrain>();

        this.slopeTerrainLayer = Resources.Load<TerrainLayer>("Materials/TerrainLayers/_DEFAULT_SLOPE");
        this.inWaterTerrainLayer = Resources.Load<TerrainLayer>("Materials/TerrainLayers/_DEFAULT_IN_WATER");

        switch(this.size)
        {
            case WorldSize.Small:
                this.worldScaleRatio = 0.1f;
                this.toyScaleRatio = 2f;
                this.noiseScale = 4f;
                break;
            case WorldSize.Medium:
                this.worldScaleRatio = 0.5f;
                this.toyScaleRatio = 1f;
                this.noiseScale = 10f;
                break;
            case WorldSize.Large:
                this.worldScaleRatio = 1f;
                this.toyScaleRatio = 1f;
                this.noiseScale = 20f;
                break;
            default:
                throw new System.NotImplementedException();
        }

        /*this.worldScaleRatio = 1f;
        this.toyScaleRatio = 1f;
        this.noiseScale = 20f;*/

        this.waterLevel = 100f;

        this.biomeNoise = new NoiseData[2];
        this.biomeNoise[0] = new NoiseData() { noise = Resources.Load<SONoise>("Prefabs/Noise/_DEFAULT_BIAS") };
        this.biomeNoise[1] = new NoiseData() { noise = Resources.Load<SONoise>("Prefabs/Noise/_DEFAULT_RANDOMNESS") };

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
        this.terrain.terrainData.SetDetailResolution(BASE_DETAIL_RESOLUTION, BASE_DETAIL_RESOLUTION_PER_PATCH);
        this.terrain.drawInstanced = DRAW_INSTANCED;

        //1.Prepare Args
        this.SortBiomeData();
        this.args = WorldGeneratorArgs.CreateNew(
            this.terrain,
            this.seed,
            this.noiseScale,
            this.biomeNoise,
            this.biomeData,
            this.slopeTerrainLayer,
            this.inWaterTerrainLayer,
            this.worldScaleRatio,
            this.toyScaleRatio,
            this.waterLevel
        );
    }

    private static void CreateBiomes(WorldGeneratorArgs args)
    {
        BiomeGenerator.GenerateBiomemapData(args, 1, 1);
    }

    private static void CreateHeightMap(WorldGeneratorArgs args)
    {
        int size = args.Terrain.terrainData.heightmapResolution;

        float[,] heightMap = new float[size, size];

        Vector2[][] octaveOffsets = new Vector2[args.BiomeCount][];
        float[] maxValues = new float[args.BiomeCount];

        float maxTerrainHeight = args.Terrain.terrainData.heightmapScale.y;

        //int chunkOffsetX = chunkPos.x * (WIDTH - 1);
        //int chunkOffsetY = chunkPos.y * (HEIGHT - 1);

        for (int i = 0; i < args.BiomeCount; i++)
            //octaveOffsets[i] = Synthesizer.CalculateOctaveOffsets(chunkOffsetX, chunkOffsetY, biomes[i].noiseData, out maxValues[i]);
            octaveOffsets[i] = Synthesizer.CalculateOctaveOffsets(0, 0, args.Seed, args.GetBiome(i).biome.NoiseData, out maxValues[i]);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                //float terrainHeight = Synthesizer.CalculateCompoundNoiseValue(x, y, octaveOffsets, biomes, maxValues, weights[chunkOffsetX + x, chunkOffsetY + y]);
                float terrainHeight = Synthesizer.CalculateCompoundNoiseValue(args, x, y, octaveOffsets, maxValues);
                heightMap[x, y] = terrainHeight / maxTerrainHeight;
            }
        }

        args.Terrain.terrainData.SetHeights(0, 0, heightMap);
        args.Terrain.transform.position = new Vector3(0, -args.WaterLevel, 0);
    }

    private static void CreateAlphaMap(WorldGeneratorArgs args, TerrainLayer slope, TerrainLayer inWater)
    {
        args.Terrain.terrainData.terrainLayers = args.TerrainLayers;

        int heightmapRes = args.Terrain.terrainData.heightmapResolution;
        int alphamapRes = args.Terrain.terrainData.alphamapResolution;
        float heightmapAlphamapRatio = heightmapRes / (float)alphamapRes;

        float[,] heightMap = args.Terrain.terrainData.GetHeights(0, 0, heightmapRes, heightmapRes);
        float[,,] alphaMap = new float[alphamapRes, alphamapRes, args.TerrainLayerCount];
        float waterLevel = args.WaterLevel / args.Terrain.terrainData.heightmapScale.y;

        for (int y = 0; y < alphamapRes; y++)
        {
            for(int x = 0; x < alphamapRes; x++)
            {
                int xInHeightMap = Mathf.FloorToInt(x * heightmapAlphamapRatio);
                int yInHeightMap = Mathf.FloorToInt(y * heightmapAlphamapRatio);

                BiomeData dominantBiome = args.GetDominantBiome(xInHeightMap, yInHeightMap);

                bool isSteep = false;
                float steepnessValue = 0;
                bool isUnderWater = false;

                //Set slopeTerrainLayer if biome doesn't supress it.
                if (!dominantBiome.biome.OverrideSteepTerrainLayer)
                {
                    //Don't ask me why it's inverted in there, I have no idea haha.
                    //I think the normalized positions are inverted to the actual heightmap-positions but idk...
                    var angle = args.Terrain.terrainData.GetSteepness(
                    (float)y / (alphamapRes - 1),
                    (float)x / (alphamapRes - 1));

                    if (angle > 30f)
                    {
                        isSteep = true;
                        steepnessValue = (float)(angle / 90.0);
                        alphaMap[x, y, args.GetTerrainLayerIndex(slope)] = steepnessValue;
                    }
                }
                //Set inWaterTerrainLayer if biome doesn't supress it.
                if (!isSteep && !dominantBiome.biome.OverrideInWaterTerrainLayer && heightMap[xInHeightMap, yInHeightMap] < waterLevel)
                {
                    alphaMap[x, y, args.GetTerrainLayerIndex(inWater)] = 1f;
                }
                //Set biome-specific terrainLayer.
                if (!isUnderWater)
                {
                    for (int i = 0; i < args.BiomeCount; i++)
                    {
                        if (args.GetTerrainLayerIndex(args.GetBiome(i).biome.BaseTerrainLayer) != args.GetTerrainLayerIndex(slope))
                            alphaMap[x, y, args.GetTerrainLayerIndex(args.GetBiome(i).biome.BaseTerrainLayer)] += args.GetWeight(xInHeightMap, yInHeightMap, i) - steepnessValue;
                        else
                            alphaMap[x, y,args.GetTerrainLayerIndex(args.GetBiome(i).biome.BaseTerrainLayer)] += args.GetWeight(xInHeightMap, yInHeightMap, i);
                    }
                }
            }
        }

        args.Terrain.terrainData.SetAlphamaps(0, 0, alphaMap);
    }

    private static void CreateEntities(WorldGeneratorArgs args)
    {
        EntityPlacer.Place(args);
    }

    private void Start()
    {
        Generate();
        BiomeVolumesBlender volBlender = this.GetComponent<BiomeVolumesBlender>();
        volBlender.Init(this.args);
    }


    private void OnValidate()
    {
        /*
         * Terrain can go up to 1:100 worldScaleRatio, though generating that in one go is most likely going to crash unity.
         * Issue's the entity-placer doing BASE_ITERATIONS*100*100 iterations. Basically too many trees for one terrain I guess.
         */

        /*
        this.worldScaleRatio = Mathf.Clamp(this.worldScaleRatio, 1, 20);

        this.toyScaleRatio = Mathf.Clamp(this.toyScaleRatio, 1, 20);

        this.noiseScale = Mathf.Max(0.01f, this.noiseScale);

        if (this.biomeNoise.Length != 2)
        {
            Debug.LogError("There must be exactly 2 noise-data references for biomenoise: (0=bias, 1=randomness). Different values are not yet supported.");
            NoiseData[] biomeNoise = new NoiseData[2];

            for(int i = 0; i < biomeNoise.Length; i++)
            {
                if (i < this.biomeNoise.Length)
                    biomeNoise[i] = this.biomeNoise[i];
            }
        }*/
    }
}
