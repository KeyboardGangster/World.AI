using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.InputSystem.Controls.AxisControl;

public class BiomeGenerator
{
    public static readonly NoiseData SMALL_SAND_DUNES = new NoiseData(new Vector2(0f, 0f), 3.6f, 0f, 48.41f, 3, 2f, 0.8f, 0, new NoisePattern[] { NoisePattern.Ridge, NoisePattern.Ridge, NoisePattern.Default });
    public static readonly NoiseData MEDIUM_SAND_DUNES = new NoiseData(new Vector2(0f, 0f), 6.77f, 0f, 50f, 3, 2f, 0.8f, 0, new NoisePattern[] { NoisePattern.Ridge, NoisePattern.Ridge, NoisePattern.Default });
    public static readonly NoiseData LARGE_SAND_DUNES = new NoiseData(new Vector2(0f, 0f), 31.05f, 0f, 100f, 3, 2f, 0.5f, 0, new NoisePattern[] { NoisePattern.Ridge, NoisePattern.Ridge, NoisePattern.Default });

    public static readonly NoiseData FLAT_PASTURE = new NoiseData(new Vector2(0f, 0f), 4f, 0f, 50f, 1, 2f, 0.5f, 0, new NoisePattern[] { NoisePattern.Default });
    public static readonly NoiseData ROUGH_PASTURE = new NoiseData(new Vector2(0f, 0f), 6.09f, 0f, 30f, 2, 2f, 0.5f, 0, new NoisePattern[] { NoisePattern.Default, NoisePattern.Default });
    public static readonly NoiseData BUMPY_PASTURE = new NoiseData(new Vector2(0f, 0f), 20f, 0f, 50f, 2, 2f, 0.5f, 0, new NoisePattern[] { NoisePattern.Default, NoisePattern.Default });

    public static readonly NoiseData SMALL_HILLS = new NoiseData(new Vector2(0f, 0f), 15f, 15f, 30f, 2, 2f, 0.5f, 0, new NoisePattern[] { NoisePattern.Default, NoisePattern.Default });
    public static readonly NoiseData HILLS = new NoiseData(new Vector2(0f, 0f), 40f, 20f, 70f, 2, 2f, 0.5f, 0, new NoisePattern[] { NoisePattern.Default, NoisePattern.Default });

    public static readonly NoiseData MOUNTAINS = new NoiseData(new Vector2(0f, 0f), 110f, 75f, 150f, 3, 1.4f, 0.5f, 0, new NoisePattern[] { NoisePattern.Default, NoisePattern.Default, NoisePattern.Ridge });
    public static readonly NoiseData MOUNTAINS_RIDGE = new NoiseData(new Vector2(0f, 0f), 140.6f, 115f, 150f, 2, 2f, 0.4f, 0, new NoisePattern[] { NoisePattern.Ridge, NoisePattern.Ridge });
    public static readonly NoiseData LARGE_MOUNTAINS = new NoiseData(new Vector2(0f, 0f), 200f, 125f, 200f, 3, 1.3f, 0.4f, 0, new NoisePattern[] { NoisePattern.Default, NoisePattern.Default, NoisePattern.Ridge });
    public static readonly NoiseData LARGE_MOUNTAINS_RIDGE = new NoiseData(new Vector2(0f, 0f), 256.52f, 200f, 220f, 3, 1.5f, 0.4f, 0, new NoisePattern[] { NoisePattern.Ridge, NoisePattern.Ridge, NoisePattern.Turbulent });

    public static readonly NoiseData SWAMP = new NoiseData(new Vector2(0f, 0f), 6f, -2f, 30f, 2, 2f, 0.5f, 0, new NoisePattern[] { NoisePattern.Turbulent, NoisePattern.Default });
    public static readonly NoiseData WET_SWAMP = new NoiseData(new Vector2(0f, 0f), 6f, -3f, 50f, 2, 2f, 0.5f, 0, new NoisePattern[] { NoisePattern.Turbulent, NoisePattern.Turbulent });
    public static readonly NoiseData DRY_SWAMP = new NoiseData(new Vector2(0f, 0f), 4.11f, 0.5f, 50f, 2, 2f, 0.5f, 0, new NoisePattern[] { NoisePattern.Turbulent, NoisePattern.Default });
    
    public static readonly NoiseData WEIRD = new NoiseData(new Vector2(0f, 0f), 30f, 0f, 200f, 3, 2f, 0.5f, 0, new NoisePattern[] { NoisePattern.Split, NoisePattern.Split, NoisePattern.Ridge });
    public static readonly NoiseData WEIRD_PLATEAU = new NoiseData(new Vector2(0f, 0f), 70f, 100f, 100f, 2, 2f, 0.1f, 0, new NoisePattern[] { NoisePattern.Split, NoisePattern.Turbulent });
    public static readonly NoiseData WEIRD_RIDGE = new NoiseData(new Vector2(0f, 0f), 40f, 50f, 100f, 3, 2f, 0.6f, 0, new NoisePattern[] { NoisePattern.Split, NoisePattern.Ridge, NoisePattern.Ridge });




    #region BiomeStuff

    /// <summary>
    /// Generates a full-resolution biome-map as weights for each vertex-position. (Not sepparated into chunks.)
    /// </summary>
    /// <param name="horChunks">Amount of horizontal chunks.</param>
    /// <param name="verChunks">Amount of vertical chunks.</param>
    /// <param name="width">Width of a single chunk.</param>
    /// <param name="height">Height of a single chunk.</param>
    /// <param name="biomes">The biomes to consider.</param>
    /// <param name="bias">The bias noise-map for biome-generation.</param>
    /// <param name="randomness">The randomness noise-map for biome-generation.</param>
    /// <returns>Weights for each vertex-position. (Not sepparated into chunks.)</returns>
    public static float[,][] GetBiomeMap(int horChunks, int verChunks, int width, int height, BiomeData[] biomes, NoiseData bias, NoiseData randomness, out int[,] dominantBiomes)
    {
        float[,][] weights = new float[horChunks * width, verChunks * height][]; //weights per biome in worldspace (no chunking)
        dominantBiomes = new int[horChunks * width, verChunks * height]; //Caching dominant biomes per point.
        PointsToBlend toBlend = new PointsToBlend(); //points in worldspace to still blend (no chunking)

        //Generate entire biome-map (without blending)
        for(int chunkY = 0; chunkY < verChunks; chunkY++)
        {
            for(int chunkX = 0; chunkX < horChunks; chunkX++)
            {
                int chunkOffsetX = chunkX * width;
                int chunkOffsetY = chunkY * height;

                Vector2[] octaveOffsetsBias = Synthesizer.CalculateOctaveOffsets(chunkOffsetX, chunkOffsetY, bias, out float maxValueBias);
                Vector2[] octaveOffsetsRandomness = Synthesizer.CalculateOctaveOffsets(chunkOffsetX, chunkOffsetY, randomness, out float maxValueRandomness);

                //Generate weights.
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int xInWorldSpace = chunkOffsetX + x;
                        int yInWorldSpace = chunkOffsetY + y;

                        //Select dominant biome from given noiseDatas
                        float biasValue = Synthesizer.CalculateNoiseValue(x, y, octaveOffsetsBias, bias, maxValueBias);
                        float randomnessValue = Synthesizer.CalculateNoiseValue(x, y, octaveOffsetsRandomness, randomness, maxValueRandomness);
                        int biomeIndex = GetDominantBiomeIndex(biasValue, randomnessValue, biomes);
                        dominantBiomes[xInWorldSpace, yInWorldSpace] = biomeIndex;
                        weights[xInWorldSpace, yInWorldSpace] = new float[biomes.Length];
                        weights[xInWorldSpace, yInWorldSpace][biomeIndex] = 1f;

                        //Prepare for blending
                        AddBlendPoints(weights, toBlend, xInWorldSpace, yInWorldSpace, biomeIndex);
                    }
                }
            }
        }

        //Blend entire biome-map
        Blend(weights, toBlend, biomes);

        return weights;
    }

    /// <summary>
    /// Gets index of the first most dominant biome for specified noise-values.
    /// </summary>
    /// <param name="w1">Bias noise-value.</param>
    /// <param name="w2">Randomness noise-value.</param>
    /// <param name="biomeData">The biomes to choose from.</param>
    /// <returns>The index of the first most dominant biome.</returns>
    private static int GetDominantBiomeIndex(float w1, float w2, BiomeData[] biomeData)
    {
        //SortBiomes(biomeData);

        int index = biomeData.Length - 1;

        foreach (BiomeData biome in biomeData.Reverse())
        {
            if (biome.bias.x <= w1 && biome.random.x <= w2)
                return index;

            index--;
        }

        return 0;
    }

    /// <summary>
    /// Fetches the NoiaseData for given Biomes-enum.
    /// </summary>
    /// <param name="biome">Biome-type enum.</param>
    /// <returns>The corresponding NoiseData</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public static NoiseData GetBiomeNoiseData(Biomes biome)
    {
        switch(biome)
        {
            case Biomes.SMALL_SAND_DUNES:
                return SMALL_SAND_DUNES;
            case Biomes.MEDIUM_SAND_DUNES:
                return MEDIUM_SAND_DUNES;
            case Biomes.LARGE_SAND_DUNES:
                return LARGE_SAND_DUNES;
            case Biomes.FLAT_PASTURE:
                return FLAT_PASTURE;
            case Biomes.ROUGH_PASTURE:
                return ROUGH_PASTURE;
            case Biomes.BUMPY_PASTURE:
                return BUMPY_PASTURE;
            case Biomes.SMALL_HILLS:
                return SMALL_HILLS;
            case Biomes.HILLS:
                return HILLS;
            case Biomes.MOUNTAINS:
                return MOUNTAINS;
            case Biomes.MOUNTAINS_RIDGE:
                return MOUNTAINS_RIDGE;
            case Biomes.LARGE_MOUNTAINS:
                return LARGE_MOUNTAINS;
            case Biomes.LARGE_MOUNTAINS_RIDGE:
                return LARGE_MOUNTAINS_RIDGE;
            case Biomes.SWAMP:
                return SWAMP;
            case Biomes.WET_SWAMP:
                return WET_SWAMP;
            case Biomes.DRY_SWAMP:
                return DRY_SWAMP;
            case Biomes.WEIRD:
                return WEIRD;
            case Biomes.WEIRD_PLATEAU:
                return WEIRD_PLATEAU;
            case Biomes.WEIRD_RIDGE:
                return WEIRD_RIDGE;
            default:
                throw new System.NotImplementedException();
        }
    }

    #endregion

    #region BlendStuff

    /// <summary>
    /// Adds starting points for blending based on biome-borders.
    /// </summary>
    /// <param name="weightMapNotBlended">Weight-map that will be used for blending.</param>
    /// <param name="toBlend">Reference to starting points for blending.</param>
    /// <param name="x">X-coordinate in world-space.</param>
    /// <param name="y">Y-coordinate in world-space.</param>
    /// <param name="biomeIndex">Index of dominant biome for these coordinates.</param>
    private static void AddBlendPoints(float[,][] weightMapNotBlended, PointsToBlend toBlend, int x, int y, int biomeIndex)
    {
        if (x == 0 && y == 0)
            return;

        bool needsBlending = false;

        if (y > 0 &&
            weightMapNotBlended[x, y][biomeIndex] != weightMapNotBlended[x, y - 1][biomeIndex])
        {
            needsBlending = true;
            toBlend.Enqueue(new Vector2Int(x, y - 1));
        }
        if (x > 0 &&
            weightMapNotBlended[x, y][biomeIndex] != weightMapNotBlended[x - 1, y][biomeIndex])
        {
            needsBlending = true;
            toBlend.Enqueue(new Vector2Int(x - 1, y));
        }
        if (needsBlending)
            toBlend.Enqueue(new Vector2Int(x, y));
    }

    /// <summary>
    /// Blends given weights using the flood fill algorithm.
    /// </summary>
    /// <param name="weights">Weight-map to blend.</param>
    /// <param name="toBlend">Reference to starting points for blending.</param>
    /// <param name="biomes">All biomes to consider.</param>
    private static void Blend(float[,][] weights, PointsToBlend toBlend, BiomeData[] biomes)
    {
        /*  
         *  Blending using this approach looks really bad, leaving line-like blending-fragments everywhere.
         *  Either do blending differently, smooth the mesh during or after generation or try using voronoi-like meshes.
         *  
         *  I guess this approach blends in a square-like way which leaves these lines, especially on diagonal biome-borders.
         *  It's fast though.
         */

        while (toBlend.Count > 0)
        {
            Vector2Int pos = toBlend.Dequeue();
            BlendOneStep(weights, toBlend, pos.x, pos.y, biomes);
        }
    }

    /// <summary>
    /// Blends one point on the map.
    /// </summary>
    /// <param name="weights">Weight-map to blend.</param>
    /// <param name="toBlend">Reference to starting points for blending.</param>
    /// <param name="x">X-coordinate in world-space.</param>
    /// <param name="y">Y-coordinate in world-space.</param>
    /// <param name="biomes">All biomes to consider.</param>
    private static void BlendOneStep(float[,][] weights, PointsToBlend toBlend, int x, int y, BiomeData[] biomes)
    {
        int totalWidth = weights.GetLength(0);
        int totalHeight = weights.GetLength(1);

        for(int biomeIndex = 0; biomeIndex < biomes.Length; biomeIndex++)
        {
            Vector2Int up = new Vector2Int(x, y + 1);
            Vector2Int down = new Vector2Int(x, y - 1);
            Vector2Int right = new Vector2Int(x + 1, y);
            Vector2Int left = new Vector2Int(x - 1, y);
            float nextWeight = Mathf.Max(0, weights[x, y][biomeIndex] - biomes[biomeIndex].falloffRate);

            if (y < totalHeight - 1 && weights[up.x, up.y][biomeIndex] < nextWeight)
            {
                if (!toBlend.Contains(up))
                    toBlend.Enqueue(up);

                weights[up.x, up.y][biomeIndex] = nextWeight;
            }
            if (y > 0 && weights[down.x, down.y][biomeIndex] < nextWeight)
            {
                if (!toBlend.Contains(down))
                    toBlend.Enqueue(down);

                weights[down.x, down.y][biomeIndex] = nextWeight;
            }
            if (x < totalWidth - 1 && weights[right.x, right.y][biomeIndex] < nextWeight)
            {
                if (!toBlend.Contains(right))
                    toBlend.Enqueue(right);

                weights[right.x, right.y][biomeIndex] = nextWeight;
            }
            if (x > 0 && weights[left.x, left.y][biomeIndex] < nextWeight)
            {
                if (!toBlend.Contains(left))
                    toBlend.Enqueue(left);

                weights[left.x, left.y][biomeIndex] = nextWeight;
            }
        }
    }
    #endregion

    /// <summary>
    /// Simple Queue with indices for faster Contains() checks.
    /// </summary>
    private class PointsToBlend
    {
        /// <summary>
        /// Queue holding all the data.
        /// </summary>
        private Queue<Vector2Int> toBlend = new Queue<Vector2Int>();

        /// <summary>
        /// Indices, mainly for faster Contains() checks.
        /// </summary>
        private HashSet<Vector2Int> indices = new HashSet<Vector2Int>();

        public int Count => toBlend.Count;

        public bool Contains(Vector2Int pos) => this.indices.Contains(pos);

        public void Enqueue(Vector2Int pos)
        {
            toBlend.Enqueue(pos);
            this.indices.Add(pos);
        }

        public Vector2Int Dequeue()
        {
            Vector2Int pos = toBlend.Dequeue();
            this.indices.Remove(pos);
            return pos;
        }
    }
}
