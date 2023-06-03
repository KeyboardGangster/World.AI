using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(WorldGenerator))]
public class WorldGeneratorInterface_Default : WorldGeneratorInterface
{
    private Terrain terrain;
    private WorldGenerator worldGenerator;

    [SerializeField]
    private SOHeight Bias;
    [SerializeField]
    private SOHeight Randomness;
    [SerializeField]
    private BiomeData[] biomeData;
    [SerializeField]
    private WorldSize size;
    [SerializeField]
    private int seed = 1;

    //private SOHeight[] biomeDistributionData; //Currently replaced by Bias and Randomness.

    public void SortBiomeData() => System.Array.Sort(this.biomeData);

    public override void GenerateWorld()
    {
        if (this.worldGenerator == null)
            this.worldGenerator = this.GetComponent<WorldGenerator>();

        this.Prepare();
        this.worldGenerator.Generate();
    }

    private void Prepare()
    {
        if (this.terrain == null)
            this.terrain = this.GetComponent<Terrain>();
        
        TerrainLayer slopeTerrainLayer = Resources.Load<TerrainLayer>("WorldAI_DefaultAssets/TerrainLayers/_DEFAULT_SLOPE");
        TerrainLayer inWaterTerrainLayer = Resources.Load<TerrainLayer>("WorldAI_DefaultAssets/TerrainLayers/_DEFAULT_IN_WATER");

        int heightmapRes;
        int alphamapRes;
        int textureRes;
        int detailRes;
        int detailResPerPatch;
        int terrainSize;
        int terrainHeight = 600;
        //Bigger Worlds, less detail. (1:x scale)
        float worldScaleRatio;
        //Bigger Worlds by downsizing everything. (1:x scale)
        float toyScaleRatio;
        //Decides how big biomes are. Not the biomes' features though.
        float biomeScale;
        //At which height to draw with in-water-texture. Terrain-height adjusts automatically to this.
        float waterLevel = 100;

        switch (this.size)
        {
            case WorldSize.Small:
                toyScaleRatio = 2f;
                biomeScale = 4f;

                heightmapRes = 129;
                alphamapRes = 64;
                textureRes = 512;
                detailRes = 256;
                detailResPerPatch = 16;
                terrainSize = 100;
                break;
            case WorldSize.Medium:
                toyScaleRatio = 1f;
                biomeScale = 10f;

                heightmapRes = 513;
                alphamapRes = 256;
                textureRes = 512;
                detailRes = 1024;
                detailResPerPatch = 16;
                terrainSize = 500;
                break;
            case WorldSize.Large:
                toyScaleRatio = 1f;
                biomeScale = 20f;

                heightmapRes = 1025;
                alphamapRes = 512;
                textureRes = 512;
                detailRes = 2024;
                detailResPerPatch = 32;
                terrainSize = 1000;
                break;
            default:
                throw new System.NotImplementedException();
        }

        worldScaleRatio = (float)terrainSize / heightmapRes;

        //this.biomeDistributionData = new SOHeight[2];
        //this.biomeDistributionData[0] = Resources.Load<SOHeight>("WorldAI_DefaultAssets/Prefabs/Height/_DEFAULT_BIAS");
        //this.biomeDistributionData[1] = Resources.Load<SOHeight>("WorldAI_DefaultAssets/Prefabs/Height/_DEFAULT_RANDOMNESS");

        this.terrain.terrainData.heightmapResolution = heightmapRes;
        this.terrain.terrainData.alphamapResolution = alphamapRes;
        this.terrain.terrainData.baseMapResolution = textureRes;
        this.terrain.terrainData.size = new Vector3(terrainSize * worldScaleRatio, terrainHeight, terrainSize * worldScaleRatio);
        this.terrain.terrainData.SetDetailResolution(detailRes, detailResPerPatch);
        this.terrain.drawInstanced = true;

        //1.Prepare Args
        this.SortBiomeData();
        this.GetComponent<WorldGenerator>().Args.CreateNew(
            this.terrain.terrainData,
            this.seed,
            biomeScale,
            new SOHeight[] { this.Bias, this.Randomness },
            this.biomeData,
            slopeTerrainLayer,
            inWaterTerrainLayer,
            worldScaleRatio,
            toyScaleRatio,
            waterLevel
        );
    }

    private void Reset()
    {
        this.Bias = Resources.Load<SOHeight>("WorldAI_DefaultAssets/Prefabs/Height/_DEFAULT_BIAS");
        this.Randomness = Resources.Load<SOHeight>("WorldAI_DefaultAssets/Prefabs/Height/_DEFAULT_RANDOMNESS");

        this.biomeData = new BiomeData[]
        {
            new BiomeData() { bias = new Vector2(0.0f, 0.5f), random = new Vector2(0.0f, 0.5f) },
            new BiomeData() { bias = new Vector2(0.0f, 0.5f), random = new Vector2(0.5f, 1.0f) },
            new BiomeData() { bias = new Vector2(0.5f, 1.0f), random = new Vector2(0.0f, 1.0f) }
        };

        this.size = WorldSize.Medium;
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

    [MenuItem("GameObject/3D Object/WorldAI/World (Default)", false, 0)]
    public static void CreateNew()
    {
        GameObject go = new GameObject("World");
        go.AddComponent<WorldGeneratorInterface_Default>();
        go.AddComponent<AthmosphereControl>();
        WorldGenerator wg = go.GetComponent<WorldGenerator>();
        WorldGenerator.CreateNew(wg);
    }
}
