using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Holds information about WorldGenerator arguments.
/// <para />
/// <b>Note:</b><br/> Properties and methods starting with _ are using non-serializable data and will cause reference-exceptions if used outside of world-generation. Do not use at runtime!<br/>
/// Static declarations are used during initialization of this class, use with caution.
/// </summary>
[CreateAssetMenu(fileName = "WorldGeneratorArgs", menuName = "GoFuckYourself", order = 0)]
public class WorldGeneratorArgs : ScriptableObject
{
    [SerializeField]
    private TerrainData terrainData;
    [SerializeField]
    private int seed;
    [SerializeField]
    [SerializeReference]
    private HeightData bias;
    [SerializeField]
    [SerializeReference]
    private HeightData randomness;
    [SerializeField]
    private float biomeScale;
    [SerializeField]
    private float worldScaleRatio;
    [SerializeField]
    private float toyScaleRatio;
    [SerializeField]
    private float waterLevel;

    /// <summary>
    /// Holds weights of each biome for each position in biomemap-resolution. Also see GetWeight(...).
    /// </summary>
    private float[,,] _weightsPerBiome;
    /// <summary>
    /// Holds the dominant biome for each position. Also see GetDominantBiome(...).
    /// </summary>
    private int[,] _dominantBiomeIndices;
    /// <summary>
    /// Holds the indices of each defined tree and rock in the terrainData.treePrototypes. Also see GetTreePrototypeIndex(...)
    /// </summary>
    private Dictionary<ScriptableObject, int> _treePrototypeIndices;
    /// <summary>
    /// Holds the indices of each defined detail (e.g. grass) in the terrainData.detailPrototypes. Also see GetDetailPrototypeIndex(...)
    /// </summary>
    private Dictionary<ScriptableObject, int> _detailPrototypeIndices;

    /// <summary>
    /// All available biomes that were given during CreateNew(...).
    /// </summary>
    [SerializeField]
    private BiomeData[] availableBiomes;

    private Dictionary<TerrainLayer, int> _terrainLayerIndices;

    /// <summary>
    /// The terrain that hold the generated world.
    /// </summary>
    public TerrainData TerrainData => this.terrainData;

    /// <summary>
    /// The seed used for world-generation.
    /// </summary>
    public int Seed => this.seed;

    /// <summary>
    /// Bias used during biomemap-generation.
    /// </summary>
    public HeightData Bias => this.bias;

    /// <summary>
    /// Randomness used during biomemap-generation.
    /// </summary>
    public HeightData Randomness => this.randomness;

    public float BiomeScale => this.biomeScale;

    /// <summary>
    /// Scales world by stretching terrain.
    /// </summary>
    public float WorldScaleRatio => this.worldScaleRatio;

    /// <summary>
    /// Scales world by shrinking everything down.
    /// </summary>
    public float ToyScaleRatio => this.toyScaleRatio;

    /// <summary>
    /// Water-level of terrain. Everything below water-level counts as underwater. Global terrain-height will be adjusted accordingly.
    /// </summary>
    public float WaterLevel => this.waterLevel;

    /// <summary>
    /// The TerrainLayers that were added.
    /// </summary>
    public TerrainLayer[] _TerrainLayers
    {
        get;
        private set;
    }

    public TerrainLayer _Slope
    {
        get;
        private set;
    }

    public TerrainLayer _InWater
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
    public int _TerrainLayerCount => this._terrainLayerIndices.Count;

    /// <summary>
    /// Gets the biome at specified biome-index.
    /// </summary>
    /// <param name="index">The biome-index.</param>
    /// <returns>The biome at specified biome-index.</returns>
    public BiomeData GetBiome(int index) => this.availableBiomes[index];

    /// <summary>
    /// Returns the weight the biome at specified in biomemap-resolution (currently biomemapRes = heightmapRes, still WIP) location holds. Usefull for blending.
    /// </summary>
    /// <param name="x">The x-coordinate in biomemap.</param>
    /// <param name="z">The z-coordinate in biomemap.</param>
    /// <param name="biomeIndex">The index of the biome in the biomes-array given during args-creation.</param>
    /// <returns></returns>
    public float _GetWeight(int x, int z, int biomeIndex) => this._weightsPerBiome[x, z, biomeIndex]; //(z * xMax * yMax) + (y * xMax) + x

    /// <summary>
    /// Returns the most dominant biome at given position in biomemap-resolution (currently biomemapRes = heightmapRes, still WIP)
    /// </summary>
    /// <param name="x">The x-coordinate in biomemap.</param>
    /// <param name="z">The y-coordinate in biomemap.</param>
    /// <returns>The most dominant biome at given position in biomemap-resolution.</returns>
    public BiomeData GetDominantBiome(int x, int z) => this.GetBiome(GetDominantBiomeIndex(this, this.bias.GetHeight(this, x, z), this.randomness.GetHeight(this, x, z)));


    /// <summary>
    /// Returns true if given biome-index corresponds to the most dominant biome at given position in biomemap-resolution.
    /// </summary>
    /// <param name="x">The x-coordinate in biomemap.</param>
    /// <param name="z">The z-coordinate in biomemap.</param>
    /// <param name="index">The index of the biome in the biomes-array given during args-creation.</param>
    /// <returns>True if given biome-index corresponds to the most dominant biome at given position in biomemap-resolution.</returns>
    public bool _IsDominantBiome(int x, int z, int index) => this._dominantBiomeIndices[x, z] == index;

    /// <summary>
    /// Gets the index of the specified TerrainLayer.
    /// </summary>
    /// <param name="tl">The TerrainLayer.</param>
    /// <returns>The index of the specified TerrainLayer.</returns>
    public int _GetTerrainLayerIndex(TerrainLayer tl) => this._terrainLayerIndices[tl];

    /// <summary>
    /// Gets the index of the specified ScriptableObject defined in BiomeData which is used for the terrainData.treePrototypes.
    /// </summary>
    /// <param name="so">The ScriptableObject to look for.</param>
    /// <returns>The index of the specified ScriptableObject defined in BiomeData which is used for the terrainData.treePrototypes.</returns>
    public int _GetTreePrototypeIndex(ScriptableObject so) => this._treePrototypeIndices[so];

    /// <summary>
    /// Gets the index of the specified ScriptableObject defined in BiomeData which is used for the terrainData.detailPrototypes.
    /// </summary>
    /// <param name="so">The ScriptableObject to look for.</param>
    /// <returns>The index of the specified ScriptableObject defined in BiomeData which is used for the terrainData.detailPrototypes.</returns>
    public int _GetDetailPrototypeIndex(ScriptableObject so) => this._detailPrototypeIndices[so];

    /// <summary>
    /// Creates a new instance of the WorldGeneratorArgs-class.
    /// </summary>
    /// <param name="terrain">The Terrain that holds the generated information.</param>
    /// <param name="seed">The seed used for generation.</param>
    /// <param name="biomeScale">The scale of biomes used during biomemap-generation.</param>
    /// <param name="biomeDistributionData">Heightmap-data used for biomemap-generation.</param>
    /// <param name="biomes">The biomes to use for generation.</param>
    /// <param name="slope">The TerrainLayer used for slopes.</param>
    /// <param name="inWater">The TerrainLayer used for underwater.</param>
    /// <param name="worldScaleRatio">Worldscare-ratio used during generation. Scales terrain by stretching terrain.</param>
    /// <param name="toyScaleRatio">Toyscale-ratio used during generation. Scales world by shrinking everything.</param>
    /// <param name="waterLevel">The waterlevel at which to consider terrain underwater.</param>
    /// <returns>A new instance of the WorldGeneratorArgs-class.</returns>
    public void CreateNew(TerrainData terrainData, int seed, float biomeScale, SOHeight[] biomeDistributionData, BiomeData[] biomes, TerrainLayer slope, TerrainLayer inWater, float worldScaleRatio, float toyScaleRatio, float waterLevel)
    {
        Undo.RecordObject(this, "WorldGeneratorArgs.CreateNew call");
        this.Clear();
        this.availableBiomes = biomes;
        //System.Array.Sort(args.availableBiomes); //Needs to be done!! Uncommented since WorldGenerator does it first.

        this.terrainData = terrainData;
        this.seed = seed;
        this.biomeScale = biomeScale;
        this.worldScaleRatio = worldScaleRatio;
        this.toyScaleRatio = toyScaleRatio;
        this.waterLevel = waterLevel;
        this._Slope = slope;
        this._InWater = inWater;

        //Prepare Biomes and TerrainLayers
        this._terrainLayerIndices = new Dictionary<TerrainLayer, int>();
        List<TerrainLayer> tl = new List<TerrainLayer>();
        _AddTerrainLayer(this, tl, slope);
        _AddTerrainLayer(this, tl, inWater);

        for (int i = 0; i < this.availableBiomes.Length; i++)
        {
            this.availableBiomes[i].Prepare(worldScaleRatio, toyScaleRatio);
            _AddTerrainLayer(this, tl, this.availableBiomes[i].biome.BaseTerrainLayer);
        }

        this._TerrainLayers = tl.ToArray();

        //Prepare Biomemap-generation
        this.bias = biomeDistributionData[0].GetHeightData();
        this.randomness = biomeDistributionData[1].GetHeightData();
        this.Bias.isBiomeDistribution = true;
        this.Randomness.isBiomeDistribution = true;
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
    public static void ReceiveBiomemapData(WorldGeneratorArgs args, float[,,] weights, int[,] dominantBiomeIndices)
    {
        args._weightsPerBiome = weights;
        args._dominantBiomeIndices = dominantBiomeIndices;
    }

    /// <summary>
    /// Update Args with entity-data. Usefull for entity-placement.
    /// </summary>
    /// <param name="args">The WorldGeneratorArgs to update.</param>
    /// <param name="treePrototypeIndices">The indices of each defined tree and rock in the terrainData.treePrototypes.</param>
    /// <param name="detailPrototypeIndices">The indices of each detail (e.g. grass) in the terrainData.detailPrototypes.</param>
    public static void ReceiveEntityData(WorldGeneratorArgs args, Dictionary<ScriptableObject, int> treePrototypeIndices, Dictionary<ScriptableObject, int> detailPrototypeIndices)
    {
        args._treePrototypeIndices = treePrototypeIndices;
        args._detailPrototypeIndices = detailPrototypeIndices;
    }

    /// <summary>
    /// Adds a TerrainLayer to list. Terrain needs to know this for splatmap-rendering.
    /// </summary>
    /// <param name="args">The WorldGeneratorArgs to update.</param>
    /// <param name="terrainLayers">The TerrainLayer-list. Will be converted to args.TerrainLayers later.</param>
    /// <param name="tl">The TerrainLayer to add to the list.</param>
    private static void _AddTerrainLayer(WorldGeneratorArgs args, List<TerrainLayer> terrainLayers, TerrainLayer tl)
    {
        if (!args._terrainLayerIndices.ContainsKey(tl))
        {
            TerrainLayer newTl = Object.Instantiate(tl);
            newTl.tileSize /= args.ToyScaleRatio;
            args._terrainLayerIndices.Add(tl, terrainLayers.Count);
            terrainLayers.Add(newTl);
        }
    }

    public void Clear()
    {
        this._weightsPerBiome = null;
        this._dominantBiomeIndices = null;
        this._treePrototypeIndices = null;
        this._detailPrototypeIndices = null;
        this.availableBiomes = null;
        this._terrainLayerIndices = null;
        this.terrainData = null;
        this.seed = 0;
        this.bias = null;
        this.randomness = null;
        this.biomeScale = 0;
        this.worldScaleRatio = 0;
        this.toyScaleRatio = 0;
        this.waterLevel = 0;
        this._TerrainLayers = null;
        this._Slope = null;
        this._InWater = null;

    }

}
