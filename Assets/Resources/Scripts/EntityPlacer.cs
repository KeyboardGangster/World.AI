using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntityPlacer : MonoBehaviour
{
    private static int BASE_ITERATIONS = 10000;

    public static void PlaceTrees(Terrain terrain, float worldScaleRatio, float toyScaleRatio, BiomeData[] biomes, int[,] dominantBiomes)
    {
        if (biomes == null || biomes.Length == 0)
            return;

        System.Random random = new System.Random(biomes[0].noiseData.seed);


        terrain.terrainData.treePrototypes = GenerateTreePrototypes(biomes, out Dictionary<GameObject, int> treePrototypeIndices);
        int heightmapRes = terrain.terrainData.heightmapResolution - 1;
        List<TreeInstance> treeInstances = new List<TreeInstance>();

        int iterations = Mathf.FloorToInt(BASE_ITERATIONS * worldScaleRatio * worldScaleRatio * toyScaleRatio * toyScaleRatio);

        for (int i = 0; i < iterations; i++)
        {
            Vector3 normalizedPos = new Vector3((float)random.NextDouble(), 0, (float)random.NextDouble());
            var angle = terrain.terrainData.GetSteepness(normalizedPos.x, normalizedPos.z);

            if (angle > 30)
                continue;

            //Don't ask me why normalizedPos is inverted in there, I have no idea haha.
            //I think the normalized positions are inverted to the actual heightmap-positions but idk...
            BiomeData dominantBiome = biomes[dominantBiomes[Mathf.FloorToInt(normalizedPos.z * heightmapRes), Mathf.FloorToInt(normalizedPos.x * heightmapRes)]];

            if (dominantBiome.trees.Length == 0)
                continue;

            if (random.NextDouble() > dominantBiome.treePropability)
                continue;

            TreeInstance instance = new TreeInstance();
            instance.heightScale = ((float)random.NextDouble() * (1.25f - 0.75f) + 0.75f) / toyScaleRatio;
            instance.widthScale = ((float)random.NextDouble() * (1.15f - 0.85f) + 0.85f) / toyScaleRatio;
            instance.position = normalizedPos;
            instance.prototypeIndex = treePrototypeIndices[dominantBiome.trees[random.Next(0, dominantBiome.trees.Length)]];
            instance.rotation = Mathf.Deg2Rad * random.Next(0, 360);

            treeInstances.Add(instance);
        }

        terrain.terrainData.SetTreeInstances(treeInstances.ToArray(), true);
    }

    private static TreePrototype[] GenerateTreePrototypes(BiomeData[] biomes, out Dictionary<GameObject, int> treePrototypeIndices)
    {
        treePrototypeIndices = new Dictionary<GameObject, int>();
        int prototypeIndex = 0;

        for (int i = 0; i < biomes.Length; i++)
        {
            for (int j = 0; j < biomes[i].trees.Length; j++)
            {
                if (!treePrototypeIndices.ContainsKey(biomes[i].trees[j]))
                    treePrototypeIndices.Add(biomes[i].trees[j], prototypeIndex++);
            }
        }

        TreePrototype[] treePrototypes = new TreePrototype[treePrototypeIndices.Count];

        foreach(KeyValuePair<GameObject, int> treePrototype in treePrototypeIndices)
        {
            treePrototypes[treePrototype.Value] = new TreePrototype() { prefab = treePrototype.Key }; 
        }

        return treePrototypes;
    }
}