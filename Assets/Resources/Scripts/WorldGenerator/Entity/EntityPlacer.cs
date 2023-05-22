using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class EntityPlacer : MonoBehaviour
{
    public static void Place(WorldGeneratorArgs args)
    {
        Dictionary<ScriptableObject, int> treePrototypeIndices = PrepareTreePrototypes(args);
        Dictionary<ScriptableObject, int> detaillPrototypeIndices = PrepareDetailPrototypes(args);
        WorldGeneratorArgs.ReceiveEntityData(args, treePrototypeIndices, detaillPrototypeIndices);

        List<TreeInstance> instances = new List<TreeInstance>();
        PlaceTrees(args, instances);
        PlaceRocks(args, instances);

        PlaceGrass(args);

        args.Terrain.terrainData.SetTreeInstances(instances.ToArray(), true);
    }

    private static void PlaceTrees(WorldGeneratorArgs args, List<TreeInstance> instances)
    {
        System.Random random = new System.Random(args.Seed);

        int heightmapRes = args.Terrain.terrainData.heightmapResolution - 1;

        int iterations = Mathf.FloorToInt(args.ToyScaleRatio * (args.Terrain.terrainData.size.x * args.Terrain.terrainData.size.x) / 50f);
        //int iterations = Mathf.FloorToInt(BASE_ITERATIONS_TREES * args.WorldScaleRatio * args.WorldScaleRatio * args.ToyScaleRatio);

        for (int i = 0; i < iterations; i++)
        {
            Vector3 normalizedPos = new Vector3((float)random.NextDouble(), 0, (float)random.NextDouble());
            var angle = args.Terrain.terrainData.GetSteepness(normalizedPos.x, normalizedPos.z);

            if (angle > 30)
                continue;

            //Don't ask me why normalizedPos is inverted in there, I have no idea haha.
            //I think the normalized positions are inverted to the actual heightmap-positions but idk...
            BiomeData dominantBiome = args.GetDominantBiome(
                Mathf.FloorToInt(normalizedPos.z * heightmapRes), 
                Mathf.FloorToInt(normalizedPos.x * heightmapRes)
            );

            if (dominantBiome.biome.Trees.Length == 0)
                continue;

            if (dominantBiome.biome.ForceRemoveTreesUnderwater &&
                args.Terrain.terrainData.GetHeight(
                    Mathf.FloorToInt(normalizedPos.x * args.Terrain.terrainData.heightmapResolution),
                    Mathf.FloorToInt(normalizedPos.z * args.Terrain.terrainData.heightmapResolution)
                ) <= args.WaterLevel)
                continue;

            int treeIndex = random.Next(0, dominantBiome.biome.Trees.Length);

            for(int j = 0; j < dominantBiome.biome.Trees.Length; j++)
            {
                treeIndex = (treeIndex + 1) % dominantBiome.biome.Trees.Length;
                TreeData treeData = dominantBiome.biome.Trees[treeIndex];

                if (random.NextDouble() > treeData.spawnRate)
                    continue;

                TreeInstance instance = new TreeInstance();
                //instance.heightScale = ((float)random.NextDouble() * (1.25f - 0.75f) + 0.75f) / args.ToyScaleRatio;
                //instance.widthScale = ((float)random.NextDouble() * (1.15f - 0.85f) + 0.85f) / args.ToyScaleRatio;
                instance.heightScale = ((float)random.NextDouble() * (treeData.tree.Height.y - treeData.tree.Height.x) + treeData.tree.Height.x) / args.ToyScaleRatio;
                instance.widthScale = ((float)random.NextDouble() * (treeData.tree.Width.y - treeData.tree.Width.x) + treeData.tree.Width.x) / args.ToyScaleRatio;
                instance.position = normalizedPos;
                instance.prototypeIndex = args.GetTreePrototypeIndex(dominantBiome.biome.Trees[treeIndex].tree);
                instance.rotation = Mathf.Deg2Rad * random.Next(0, 360);

                instances.Add(instance);
                break;
            }
        }
    }

    private static void PlaceRocks(WorldGeneratorArgs args, List<TreeInstance> instances)
    {
        System.Random random = new System.Random(args.Seed + 1);

        int heightmapRes = args.Terrain.terrainData.heightmapResolution - 1;
        int iterations = Mathf.FloorToInt(args.ToyScaleRatio * (args.Terrain.terrainData.size.x * args.Terrain.terrainData.size.x) / 1000f);
        //int iterations = Mathf.FloorToInt(BASE_ITERATIONS_ROCKS * args.WorldScaleRatio * args.WorldScaleRatio * args.ToyScaleRatio);

        for (int i = 0; i < iterations; i++)
        {
            Vector3 normalizedPos = new Vector3((float)random.NextDouble(), 0, (float)random.NextDouble());
            var angle = args.Terrain.terrainData.GetSteepness(normalizedPos.x, normalizedPos.z);

            if (angle > 30)
                continue;

            //Don't ask me why normalizedPos is inverted in there, I have no idea haha.
            //I think the normalized positions are inverted to the actual heightmap-positions but idk...
            BiomeData dominantBiome = args.GetDominantBiome(
                Mathf.FloorToInt(normalizedPos.z * heightmapRes),
                Mathf.FloorToInt(normalizedPos.x * heightmapRes)
            );

            if (dominantBiome.biome.PropagationData.Length == 0)
                continue;

            /*if (dominantBiome.biome.ForceRemoveTreesUnderwater &&
                args.Terrain.terrainData.GetHeight(
                    Mathf.FloorToInt(normalizedPos.x * args.Terrain.terrainData.heightmapResolution),
                    Mathf.FloorToInt(normalizedPos.z * args.Terrain.terrainData.heightmapResolution)
                ) <= args.WaterLevel)
                continue;*/

            int rockIndex = random.Next(0, dominantBiome.biome.PropagationData.Length);

            for (int j = 0; j < dominantBiome.biome.PropagationData.Length; j++)
            {
                rockIndex = (rockIndex + 1) % dominantBiome.biome.PropagationData.Length;
                PropagationData propagationData = dominantBiome.biome.PropagationData[rockIndex];

                if (random.NextDouble() > propagationData.spawnRate)
                    continue;

                PropagateRocks(args, instances, normalizedPos, propagationData);
                //Do propagation stuff here...
                /*
                TreeInstance instance = new TreeInstance();
                //instance.heightScale = ((float)random.NextDouble() * (1.25f - 0.75f) + 0.75f) / args.ToyScaleRatio;
                //instance.widthScale = ((float)random.NextDouble() * (1.15f - 0.85f) + 0.85f) / args.ToyScaleRatio;
                instance.heightScale = ((float)random.NextDouble() * (rockData.rocks[0].Height.y - rockData.rocks[0].Height.x) + rockData.rocks[0].Height.x) / args.ToyScaleRatio;
                instance.widthScale = ((float)random.NextDouble() * (rockData.rocks[0].Width.y - rockData.rocks[0].Width.x) + rockData.rocks[0].Width.x) / args.ToyScaleRatio;
                instance.position = normalizedPos;
                instance.prototypeIndex = args.GetTreePrototypeIndex(dominantBiome.biome.Trees[rockIndex].tree);
                instance.rotation = Mathf.Deg2Rad * random.Next(0, 360);

                instances.Add(instance);*/
                break;
            }
        }
    }

    private static void PropagateRocks(WorldGeneratorArgs args, List<TreeInstance> instances, Vector3 normalizedPos, PropagationData propagationData)
    {
        System.Random random = new System.Random(args.Seed);

        //Key = normalized-position, Value.x = angle, Value.y = size
        List<KeyValuePair<Vector3, Vector2>> nodes = new List<KeyValuePair<Vector3, Vector2>>();

        PlaceRock(args, instances, random, propagationData.rocks[random.Next(0, propagationData.rocks.Length)], normalizedPos, propagationData.startingSize);
        nodes.Add(new KeyValuePair<Vector3, Vector2>(normalizedPos, new Vector2(0, propagationData.startingSize)));

        //float lifeTime = propagationData.spawnRate * 2;
        //float lifeTimeDecrease = 1f / propagationData.reach;
        int maxChildren = 8;
        int minChildren = 4;
        int iterations = random.Next(
            Mathf.Min(propagationData.minIterations, propagationData.maxIterations),
            Mathf.Max(propagationData.minIterations, propagationData.maxIterations) + 1
        );

        for(int iteration = 0; iteration < iterations; iteration++)
        {
            List<KeyValuePair<Vector3, Vector2>> newNodes = new List<KeyValuePair<Vector3, Vector2>>();

            foreach (KeyValuePair<Vector3, Vector2> node in nodes)
            {
                int children = random.Next(minChildren, maxChildren);
                Vector3 normalizedParentPos = node.Key;
                float parentAngle = node.Value.x;
                float parentSize = node.Value.y;

                for (int i = 0; i < children; i++)
                {
                    float angleDeviation;

                    if (normalizedParentPos == normalizedPos) //if root node
                        angleDeviation = random.Next(0, 360);
                    else
                        angleDeviation = random.Next(-90, 91);

                    float newAngle = parentAngle + angleDeviation;
                    float radians = Mathf.Deg2Rad * newAngle;
                    float newSize = parentSize * propagationData.sizeDecrease * ((float)random.NextDouble() * (1.05f - 0.95f) + 0.95f);

                    SORock nextRock = propagationData.rocks[random.Next(0, propagationData.rocks.Length)];
                    Vector3 dir = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians)); //normalised
                    //float sizeRatio = 1f / nextRock.CollisionRadius; //size in relation to radius=1

                    float distance = parentSize + 1 * newSize;
                    float distanceInNormalizedSpace = distance / args.Terrain.terrainData.heightmapResolution;

                    Vector3 newNormalizedPos = normalizedParentPos + dir * distanceInNormalizedSpace;

                    bool placed = PlaceRock(args, instances, random, nextRock, newNormalizedPos, newSize);

                    if (placed)
                        newNodes.Add(new KeyValuePair<Vector3, Vector2>(newNormalizedPos, new Vector2(newAngle, newSize)));
                }
            }

            nodes = newNodes;

            if (nodes.Count == 0)
                break;
        }
    }

    private static bool PlaceRock(WorldGeneratorArgs args, List<TreeInstance> instances, System.Random random, SORock rock, Vector3 normalizedPos, float size)
    {
        //work with normalized sizes.
        float sizeRatio = 1f / rock.CollisionRadius; //size in relation to radius=1
        TreeInstance instance = new TreeInstance();
        instance.heightScale = sizeRatio * size / args.ToyScaleRatio;
        instance.widthScale = sizeRatio * size / args.ToyScaleRatio;
        instance.position = normalizedPos;
        instance.prototypeIndex = args.GetTreePrototypeIndex(rock);
        instance.rotation = Mathf.Deg2Rad * random.Next(0, 360);

        instances.Add(instance);

        //ToDo: return false if colliding
        return true;
    }

    private static void PlaceGrass(WorldGeneratorArgs args)
    {
        System.Random random = new System.Random(args.Seed);

        int heightmapRes = args.Terrain.terrainData.heightmapResolution - 1;
        int detailRes = args.Terrain.terrainData.detailResolution;
        float ratio = (float)heightmapRes / detailRes;
        int[][,] detailMap = new int[args.Terrain.terrainData.detailPrototypes.Length][,];
        float scaleMultiplier = args.WorldScaleRatio * args.ToyScaleRatio;
        Vector2 seedOffset = new Vector2(random.Next(-10000, 10000), random.Next(-10000, 10000));

        for (int i = 0; i < detailMap.Length; i++)
        {
            detailMap[i] = new int[detailRes, detailRes];
        }

        for (int i = 0; i < detailRes; i++)
        {
            for (int j = 0; j < detailRes; j++)
            {
                Vector3Int posInHeightmap = new Vector3Int(Mathf.FloorToInt(i * ratio), 0, Mathf.FloorToInt(j * ratio));
                Vector3 normalizedPos = (Vector3)posInHeightmap / heightmapRes;
                var angle = args.Terrain.terrainData.GetSteepness(normalizedPos.z, normalizedPos.x);
                float height = args.Terrain.terrainData.GetHeight(posInHeightmap.z, posInHeightmap.x);

                for (int biomeIndex = 0; biomeIndex < args.BiomeCount; biomeIndex++)
                {
                    BiomeData biomeData = args.GetBiome(biomeIndex); 
                    float weight = args.GetWeight(posInHeightmap.x, posInHeightmap.z, biomeIndex);

                    if (weight < 0.1f)
                        continue;

                    if (biomeData.biome.Grass.Length == 0)
                        continue;

                    //attempt at giving grass/ foliage some pattern.
                    float noiseScale = Mathf.Max(200f * biomeData.finalGrassNoiseScale, 0.1f);
                    float bias = Mathf.Max(Mathf.PerlinNoise((i + seedOffset.x) / noiseScale, (j + seedOffset.y) / noiseScale) * 2f, 0);
                    int grassIndex = Mathf.FloorToInt(biomeData.biome.Grass.Length * bias) % biomeData.biome.Grass.Length;

                    for (int layer = 0; layer < biomeData.biome.Grass.Length; layer++)
                    {
                        grassIndex = (grassIndex + 1) % biomeData.biome.Grass.Length;
                        GrassData grass = biomeData.biome.Grass[grassIndex];

                        double chance = random.NextDouble();
                        if (
                            //Normal spawning
                            (!grass.biased && chance > grass.spawnRate) ||

                            //Biased spawning/ pattern-creation
                            (grass.biased && 
                            ((0.2f <= bias && bias < 0.35f) ||
                            (0.5f <= bias && bias < 0.75f) ||
                            (1f <= bias && bias < 1.2f) ||
                            (1.7f <= bias && bias <= 2f) ||
                            chance > grass.spawnRate / (3 * layer + 1)))
                        )
                            continue;

                        //Biome-blending
                        if (!args.IsDominantBiome(posInHeightmap.x, posInHeightmap.z, biomeIndex) && random.NextDouble() > weight)
                            continue;

                        //Underwater
                        if (!grass.grass.PlaceUnderwater && height <= args.WaterLevel)
                            continue;

                        //Steep slopes
                        if (!grass.grass.PlaceOnSlopes && angle > 30)
                            continue;

                        detailMap[args.GetDetailPrototypeIndex(grass.grass)][i, j] = Mathf.Max(Mathf.FloorToInt(weight * (scaleMultiplier * grass.density + 1)), 1);
                    }
                }
            }
        }

        for (int i = 0; i < detailMap.Length; i++)
        {
            args.Terrain.terrainData.SetDetailLayer(0, 0, i, detailMap[i]);
        }
    }


    private static Dictionary<ScriptableObject, int> PrepareTreePrototypes(WorldGeneratorArgs args)
    {
        Dictionary<ScriptableObject, int> indices = new Dictionary<ScriptableObject, int>();
        int index = 0;

        for(int i = 0; i < args.BiomeCount; i++) 
        {
            SOBiome biome = args.GetBiome(i).biome;

            for(int j = 0; j < biome.Trees.Length; j++)
            {
                if (!indices.ContainsKey(biome.Trees[j].tree))
                    indices.Add(biome.Trees[j].tree, index++);
            }

            for(int j = 0; j < biome.PropagationData.Length; j++)
            {
                foreach(SORock rock in biome.PropagationData[j].rocks)
                    if (!indices.ContainsKey(rock))
                        indices.Add(rock, index++);
            }
        }

        TreePrototype[] prototypes = new TreePrototype[indices.Count];

        foreach (KeyValuePair<ScriptableObject, int> prototype in indices)
        {
            if (prototype.Key is SOTree)
                prototypes[prototype.Value] = new TreePrototype() { prefab = ((SOTree)prototype.Key).Prefab };
            else if (prototype.Key is SORock)
                prototypes[prototype.Value] = new TreePrototype() { prefab = ((SORock)prototype.Key).Prefab };
        }

        args.Terrain.terrainData.treePrototypes = prototypes;
        return indices;
    }

    private static Dictionary<ScriptableObject, int> PrepareDetailPrototypes(WorldGeneratorArgs args)
    {
        Dictionary<ScriptableObject, int>  indices = new Dictionary<ScriptableObject, int>();
        int index = 0;

        for (int i = 0; i < args.BiomeCount; i++)
        {
            for (int j = 0; j < args.GetBiome(i).biome.Grass.Length; j++)
            {
                SOGrass grass = args.GetBiome(i).biome.Grass[j].grass;

                if (!indices.ContainsKey(grass))
                    indices.Add(grass, index++);
            }
        }

        DetailPrototype[] prototypes = new DetailPrototype[indices.Count];

        foreach (KeyValuePair<ScriptableObject, int> prototype in indices)
        {
            SOGrass grass = (SOGrass)prototype.Key;

            prototypes[prototype.Value] = new DetailPrototype()
            {
                prototype = grass.Prefab,
                minHeight = grass.Height.x / args.ToyScaleRatio,
                maxHeight = grass.Height.y / args.ToyScaleRatio,
                minWidth = grass.Width.x / args.ToyScaleRatio,
                maxWidth = grass.Width.y / args.ToyScaleRatio,
                noiseSeed = grass.Seed,
                noiseSpread = (grass.NoiseSpread + 0.01f) * args.ToyScaleRatio,
                prototypeTexture = null,
                renderMode = DetailRenderMode.VertexLit,
                usePrototypeMesh = true,
                useInstancing = true
            };
        }

        args.Terrain.terrainData.detailPrototypes = prototypes;
        return indices;
    }
}