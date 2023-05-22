using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Holds information about WorldGenerator arguments.
/// <para />
/// <b>Note:</b> Static declarations are used during initialization of this class, do not use them otherwise.
/// </summary>
public class WorldGeneratorArgs
{
    /// <summary>
    /// Holds weights of each biome for each position in biomemap-resolution. Also see GetWeight(...).
    /// </summary>
    private float[,][] weightsPerBiome;
    /// <summary>
    /// Holds the dominant biome for each position. Also see GetDominantBiome(...).
    /// </summary>
    private int[,] dominantBiomeIndices;
    /// <summary>
    /// Holds the indices of each defined tree and rock in the terrainData.treePrototypes. Also see GetTreePrototypeIndex(...)
    /// </summary>
    private Dictionary<ScriptableObject, int> treePrototypeIndices;
    /// <summary>
    /// Holds the indices of each defined detail (e.g. grass) in the terrainData.detailPrototypes. Also see GetDetailPrototypeIndex(...)
    /// </summary>
    private Dictionary<ScriptableObject, int> detailPrototypeIndices;

    /// <summary>
    /// All available biomes that were given during CreateNew(...).
    /// </summary>
    private BiomeData[] availableBiomes;

    private Dictionary<TerrainLayer, int> terrainLayerIndices = new Dictionary<TerrainLayer, int>();

    private WorldGeneratorArgs() { }

    /// <summary>
    /// The terrain that hold the generated world.
    /// </summary>
    public Terrain Terrain
    {
        get;
        private set;
    }

    /// <summary>
    /// The seed used for world-generation.
    /// </summary>
    public int Seed
    {
        get;
        private set;
    }

    /// <summary>
    /// Bias used during biomemap-generation.
    /// </summary>
    public NoiseData Bias
    {
        get;
        private set;
    }

    /// <summary>
    /// Randomness used during biomemap-generation.
    /// </summary>
    public NoiseData Randomness
    {
        get;
        private set;
    }

    /// <summary>
    /// Scales world by stretching terrain.
    /// </summary>
    public float WorldScaleRatio
    {
        get;
        private set;
    }

    /// <summary>
    /// Scales world by shrinking everything down.
    /// </summary>
    public float ToyScaleRatio
    {
        get;
        private set;
    }

    /// <summary>
    /// Water-level of terrain. Everything below water-level counts as underwater. Global terrain-height will be adjusted accordingly.
    /// </summary>
    public float WaterLevel
    {
        get;
        private set;
    }

    /// <summary>
    /// The TerrainLayers that were added.
    /// </summary>
    public TerrainLayer[] TerrainLayers
    {
        get;
        private set;
    }

    /// <summary>
    /// Amount of Biomes that are used.
    /// </summary>
    public int BiomeCount => this.availableBiomes.Length;

    /// <summary>
    /// Amount of TerrainLayers that were added.
    /// </summary>
    public int TerrainLayerCount => this.terrainLayerIndices.Count;

    /// <summary>
    /// Gets the biome at specified biome-index.
    /// </summary>
    /// <param name="index">The biome-index.</param>
    /// <returns>The biome at specified biome-index.</returns>
    public BiomeData GetBiome(int index)
    {
        return this.availableBiomes[index];
    }

    /// <summary>
    /// Returns the weight the biome at specified in biomemap-resolution (currently biomemapRes = heightmapRes, still WIP) location holds. Usefull for blending.
    /// </summary>
    /// <param name="x">The x-coordinate in biomemap.</param>
    /// <param name="z">The z-coordinate in biomemap.</param>
    /// <param name="biomeIndex">The index of the biome in the biomes-array given during args-creation.</param>
    /// <returns></returns>
    public float GetWeight(int x, int z, int biomeIndex) => this.weightsPerBiome[x, z][biomeIndex];

    /// <summary>
    /// Returns the most dominant biome at given position in biomemap-resolution (currently biomemapRes = heightmapRes, still WIP)
    /// </summary>
    /// <param name="x">The x-coordinate in biomemap.</param>
    /// <param name="z">The y-coordinate in biomemap.</param>
    /// <returns>The most dominant biome at given position in biomemap-resolution.</returns>
    public BiomeData GetDominantBiome(int x, int z) => this.availableBiomes[this.dominantBiomeIndices[x, z]];

    /// <summary>
    /// Returns the weight of the most dominant biome at given position in biomemap-resolution (currently biomemapRes = heightmapRes, still WIP)
    /// </summary>
    /// <param name="x">The x-coordinate in biomemap.</param>
    /// <param name="z">The y-coordinate in biomemap.</param>
    /// <returns>The weight of the most dominant biome at given position in biomemap-resolution.</returns>
    public float GetDominantWeight(int x, int z) => this.weightsPerBiome[x, z][this.dominantBiomeIndices[x, z]];

    /// <summary>
    /// Returns true if given biome-index corresponds to the most dominant biome at given position in biomemap-resolution.
    /// </summary>
    /// <param name="x">The x-coordinate in biomemap.</param>
    /// <param name="z">The z-coordinate in biomemap.</param>
    /// <param name="index">The index of the biome in the biomes-array given during args-creation.</param>
    /// <returns>True if given biome-index corresponds to the most dominant biome at given position in biomemap-resolution.</returns>
    public bool IsDominantBiome(int x, int z, int index) => this.dominantBiomeIndices[x, z] == index;

    /// <summary>
    /// Gets the index of the specified TerrainLayer.
    /// </summary>
    /// <param name="tl">The TerrainLayer.</param>
    /// <returns>The index of the specified TerrainLayer.</returns>
    public int GetTerrainLayerIndex(TerrainLayer tl) => this.terrainLayerIndices[tl];

    /// <summary>
    /// Gets the index of the specified ScriptableObject defined in BiomeData which is used for the terrainData.treePrototypes.
    /// </summary>
    /// <param name="so">The ScriptableObject to look for.</param>
    /// <returns>The index of the specified ScriptableObject defined in BiomeData which is used for the terrainData.treePrototypes.</returns>
    public int GetTreePrototypeIndex(ScriptableObject so) => this.treePrototypeIndices[so];

    /// <summary>
    /// Gets the index of the specified ScriptableObject defined in BiomeData which is used for the terrainData.detailPrototypes.
    /// </summary>
    /// <param name="so">The ScriptableObject to look for.</param>
    /// <returns>The index of the specified ScriptableObject defined in BiomeData which is used for the terrainData.detailPrototypes.</returns>
    public int GetDetailPrototypeIndex(ScriptableObject so) => this.detailPrototypeIndices[so];

    /// <summary>
    /// Creates a new instance of the WorldGeneratorArgs-class.
    /// </summary>
    /// <param name="terrain">The Terrain that holds the generated information.</param>
    /// <param name="seed">The seed used for generation.</param>
    /// <param name="biomeScale">The scale of biomes used during biomemap-generation.</param>
    /// <param name="biomes">The biomes to use for generation.</param>
    /// <param name="slope">The TerrainLayer used for slopes.</param>
    /// <param name="inWater">The TerrainLayer used for underwater.</param>
    /// <param name="worldScaleRatio">Worldscare-ratio used during generation. Scales terrain by stretching terrain.</param>
    /// <param name="toyScaleRatio">Toyscale-ratio used during generation. Scales world by shrinking everything.</param>
    /// <param name="worldToHeightmapRatio">World- to heightmap-resolution-ratio. Used to adjust noise-scale to world-/ heightmap-resolution differences.</param>
    /// <param name="waterLevel">The waterlevel at which to consider terrain underwater.</param>
    /// <returns>A new instance of the WorldGeneratorArgs-class.</returns>
    public static WorldGeneratorArgs CreateNew(Terrain terrain, int seed, float biomeScale, NoiseData[] biomeNoise, BiomeData[] biomes, TerrainLayer slope, TerrainLayer inWater, float worldScaleRatio, float toyScaleRatio, float waterLevel)
    {
        WorldGeneratorArgs args = new WorldGeneratorArgs();
        args.availableBiomes = biomes;
        //System.Array.Sort(args.availableBiomes); //Needs to be done!! Uncommented since WorldGenerator does it first.

        args.Terrain = terrain;
        args.Seed = seed;
        args.WorldScaleRatio = worldScaleRatio;
        args.ToyScaleRatio = toyScaleRatio;
        args.WaterLevel = waterLevel;

        //Prepare Biomes and TerrainLayers
        List<TerrainLayer> terrainLayers = new List<TerrainLayer>();
        AddTerrainLayer(args, terrainLayers, slope);
        AddTerrainLayer(args, terrainLayers, inWater);

        for (int i = 0; i < args.availableBiomes.Length; i++)
        {
            //args.availableBiomes[i].noiseData = BiomeGenerator.GetBiomeNoiseData(biomes[i].so.biome);
            args.availableBiomes[i].Prepare(worldScaleRatio, toyScaleRatio);
            args.availableBiomes[i].biome.NoiseData.Prepare(worldScaleRatio, toyScaleRatio, waterLevel);
            AddTerrainLayer(args, terrainLayers, args.availableBiomes[i].biome.BaseTerrainLayer);
        }

        args.TerrainLayers = terrainLayers.ToArray();

        //Prepare Biomemap-generation
        NoiseData biasData = biomeNoise[0];
        NoiseData randomnessData = biomeNoise[1];
        //biasData.Prepare(worldScaleRatio / biomeScale, toyScaleRatio, 0);
        //randomnessData.Prepare(worldScaleRatio / biomeScale, toyScaleRatio, 0);
        biasData.Prepare(1, 1, 0);
        biasData.finalNoiseScale *= biomeScale;
        biasData.finalNoiseScale /= worldScaleRatio * toyScaleRatio;
        randomnessData.Prepare(1, 1, 0);
        randomnessData.finalNoiseScale *= biomeScale;
        randomnessData.finalNoiseScale /= worldScaleRatio * toyScaleRatio;

        args.Bias = biasData;
        args.Randomness = randomnessData;

        return args;
    }

    /// <summary>
    /// Gets index of the first most dominant biome for specified noise-values.
    /// </summary>
    /// <param name="w1">Bias noise-value.</param>
    /// <param name="w2">Randomness noise-value.</param>
    /// <param name="biomeData">The biomes to choose from.</param>
    /// <returns>The index of the first most dominant biome.</returns>
    public static int GetDominantBiomeIndex(WorldGeneratorArgs args, float w1, float w2)
    {
        int index = args.availableBiomes.Length - 1;

        foreach (BiomeData biome in args.availableBiomes.Reverse())
        {
            if (biome.bias.x <= w1 && biome.random.x <= w2)
                return index;

            index--;
        }

        return 0;
    }

    /// <summary>
    /// Update Args with biomemap-data. Usefull for blending.
    /// </summary>
    /// <param name="args">The WorldGeneratorArgs to update.</param>
    /// <param name="weights">The weights of each biome at each position in biomemap-resolution.</param>
    /// <param name="dominantBiomeIndices">The 2 most dominant biomes for each position in biomemap-resolution.</param>
    public static void ReceiveBiomemapData(WorldGeneratorArgs args, float[,][] weights, int[,] dominantBiomeIndices)
    {
        args.weightsPerBiome = weights;
        args.dominantBiomeIndices = dominantBiomeIndices;
    }

    /// <summary>
    /// Update Args with entity-data. Usefull for entity-placement.
    /// </summary>
    /// <param name="args">The WorldGeneratorArgs to update.</param>
    /// <param name="treePrototypeIndices">The indices of each defined tree and rock in the terrainData.treePrototypes.</param>
    /// <param name="detailPrototypeIndices">The indices of each detail (e.g. grass) in the terrainData.detailPrototypes.</param>
    public static void ReceiveEntityData(WorldGeneratorArgs args, Dictionary<ScriptableObject, int> treePrototypeIndices, Dictionary<ScriptableObject, int> detailPrototypeIndices)
    {
        args.treePrototypeIndices = treePrototypeIndices;
        args.detailPrototypeIndices = detailPrototypeIndices;
    }

    /// <summary>
    /// Adds a TerrainLayer to list. Terrain needs to know this for splatmap-rendering.
    /// </summary>
    /// <param name="args">The WorldGeneratorArgs to update.</param>
    /// <param name="terrainLayers">The TerrainLayer-list. Will be converted to args.TerrainLayers later.</param>
    /// <param name="tl">The TerrainLayer to add to the list.</param>
    private static void AddTerrainLayer(WorldGeneratorArgs args, List<TerrainLayer> terrainLayers, TerrainLayer tl)
    {
        if (!args.terrainLayerIndices.ContainsKey(tl))
        {
            TerrainLayer newTl = Object.Instantiate(tl);
            newTl.tileSize /= args.ToyScaleRatio;
            args.terrainLayerIndices.Add(tl, terrainLayers.Count);
            terrainLayers.Add(newTl);
        }
    }

}
