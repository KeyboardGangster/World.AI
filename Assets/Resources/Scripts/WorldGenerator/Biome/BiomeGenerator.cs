using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.InputSystem.Controls.AxisControl;

public class BiomeGenerator
{
    /*public static readonly NoiseData SMALL_SAND_DUNES = new NoiseData(new Vector2(0f, 0f), 3.6f, 0f, 48.41f, 3, 2f, 0.8f, 0, new NoisePattern[] { NoisePattern.Ridge, NoisePattern.Ridge, NoisePattern.Default });
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
    */

    #region BiomeStuff

    /// <summary>
    /// Generates a full-resolution biome-map as weights for each vertex-position. (Not sepparated into chunks.)
    /// </summary>
    /// <param name="args">The WorldGeneratorArgs to use.</param>
    /// <param name="horChunks">Amount of horizontal chunks.</param>
    /// <param name="verChunks">Amount of vertical chunks.</param>
    /// <returns>Weights for each vertex-position. (Not sepparated into chunks.)</returns>
    public static void GenerateBiomemapData(WorldGeneratorArgs args, int horChunks, int verChunks)
    {
        int size = args.TerrainData.heightmapResolution;

        //float[,][] weights = new float[horChunks * size, verChunks * size][]; //weights per biome in worldspace (no chunking)
        float[,,] weights = new float[horChunks * size, verChunks * size, args.BiomeCount];
        PointsToBlend toBlend = new PointsToBlend(); //points in worldspace to still blend (no chunking)

        //Generate entire biome-map (without blending)
        for(int chunkY = 0; chunkY < verChunks; chunkY++)
        {
            for(int chunkX = 0; chunkX < horChunks; chunkX++)
            {
                int chunkOffsetX = chunkX * size;
                int chunkOffsetY = chunkY * size;

                args.Bias.Prepare(args, chunkOffsetX, chunkOffsetY);
                args.Randomness.Prepare(args, chunkOffsetX, chunkOffsetY);

                //Generate weights.
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int xInWorldSpace = chunkOffsetX + x;
                        int yInWorldSpace = chunkOffsetY + y;

                        //Select dominant biome from given noiseDatas
                        float biasValue = args.Bias.GetHeight(args, x, y);
                        float randomnessValue = args.Randomness.GetHeight(args, x, y);
                        int biomeIndex = WorldGeneratorArgs.GetDominantBiomeIndex(args, biasValue, randomnessValue);

                        //weights[xInWorldSpace, yInWorldSpace] = new float[args.BiomeCount];
                        //weights[xInWorldSpace, yInWorldSpace][biomeIndex] = 1f;
                        weights[xInWorldSpace, yInWorldSpace, biomeIndex] = 1f;

                        //Prepare for blending
                        AddBlendPoints(weights, toBlend, xInWorldSpace, yInWorldSpace, biomeIndex);
                    }
                }
            }
        }

        BlendBiomeBorders(args, weights, toBlend);

        Finalize(args, weights);
        //return weights;
    }

    
    /// <summary>
    /// Makes sure that the sum of all weights for a single position is 1. Also marks most dominant biome per position.
    /// </summary>
    /// <param name="args">The WorldGeneratorArgs to use.</param>
    /// <param name="weights">The unnormalized weights.</param>
    /// <returns>Normalized weights.</returns>
    private static void Finalize(WorldGeneratorArgs args, float[,,] weights)
    {
        //int[,] dominantBiomesIndices = new int[weights.GetLength(0), weights.GetLength(1)];
        int[,] dominantBiomesIndices = new int[weights.GetLength(0), weights.GetLength(1)];

        for(int i = 0; i < weights.GetLength(0); i++)
        {
            for(int j = 0; j < weights.GetLength(1); j++)
            {
                float sum = 0;

                for (int biomeIndex = 0; biomeIndex < args.BiomeCount; biomeIndex++)
                {
                    float biomeWeight = weights[i, j, biomeIndex];
                    //Get sum of weights
                    sum += biomeWeight;

                    //Write index of biome with highest weight at dominantBiomesIndices[i, j]
                    if (biomeWeight >= weights[i, j, dominantBiomesIndices[i, j]])
                    {
                        dominantBiomesIndices[i, j] = biomeIndex;
                    }
                }

                for (int biomeIndex = 0; biomeIndex < args.BiomeCount; biomeIndex++)
                {
                    //Normalize weights
                    weights[i, j, biomeIndex] /= sum;
                }
            }
        }

        WorldGeneratorArgs.ReceiveBiomemapData(args, weights, dominantBiomesIndices);
        //return weights;
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
    private static void AddBlendPoints(float[,,] weightMapNotBlended, PointsToBlend toBlend, int x, int y, int biomeIndex)
    {
        if (x == 0 && y == 0)
            return;

        bool needsBlending = false;

        if (y > 0 &&
            weightMapNotBlended[x, y, biomeIndex] != weightMapNotBlended[x, y - 1, biomeIndex])
        {
            needsBlending = true;
            toBlend.Enqueue(new Vector2Int(x, y - 1));
        }
        if (x > 0 &&
            weightMapNotBlended[x, y, biomeIndex] != weightMapNotBlended[x - 1, y, biomeIndex])
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
    /// <param name="args">The WorldGeneratorArgs to use.</param>
    /// <param name="weights">Weight-map to blend.</param>
    /// <param name="toBlend">Reference to starting points for blending.</param>
    private static void BlendBiomeBorders(WorldGeneratorArgs args, float[,,] weights, PointsToBlend toBlend)
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
            BlendOneStep(args, weights, toBlend, pos.x, pos.y);
        }
    }

    /// <summary>
    /// Blends one point on the map.
    /// </summary>
    /// <param name="args">The WorldGeneratorArgs to use.</param>
    /// <param name="weights">Weight-map to blend.</param>
    /// <param name="toBlend">Reference to starting points for blending.</param>
    /// <param name="x">X-coordinate in world-space.</param>
    /// <param name="y">Y-coordinate in world-space.</param>
    private static void BlendOneStep(WorldGeneratorArgs args, float[,,] weights, PointsToBlend toBlend, int x, int y)
    {
        int totalWidth = weights.GetLength(0);
        int totalHeight = weights.GetLength(1);

        for(int biomeIndex = 0; biomeIndex < args.BiomeCount; biomeIndex++)
        {
            Vector2Int up = new Vector2Int(x, y + 1);
            Vector2Int down = new Vector2Int(x, y - 1);
            Vector2Int right = new Vector2Int(x + 1, y);
            Vector2Int left = new Vector2Int(x - 1, y);
            float nextWeight = Mathf.Max(0, weights[x, y, biomeIndex] - args.GetBiome(biomeIndex).finalFalloffRate);

            if (y < totalHeight - 1 && weights[up.x, up.y, biomeIndex] < nextWeight)
            {
                if (!toBlend.Contains(up))
                    toBlend.Enqueue(up);

                weights[up.x, up.y, biomeIndex] = nextWeight;
            }
            if (y > 0 && weights[down.x, down.y, biomeIndex] < nextWeight)
            {
                if (!toBlend.Contains(down))
                    toBlend.Enqueue(down);

                weights[down.x, down.y, biomeIndex] = nextWeight;
            }
            if (x < totalWidth - 1 && weights[right.x, right.y, biomeIndex] < nextWeight)
            {
                if (!toBlend.Contains(right))
                    toBlend.Enqueue(right);

                weights[right.x, right.y, biomeIndex] = nextWeight;
            }
            if (x > 0 && weights[left.x, left.y, biomeIndex] < nextWeight)
            {
                if (!toBlend.Contains(left))
                    toBlend.Enqueue(left);

                weights[left.x, left.y, biomeIndex] = nextWeight;
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
