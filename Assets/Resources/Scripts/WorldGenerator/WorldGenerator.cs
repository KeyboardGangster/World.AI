using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using static UnityEngine.Mesh;
using UnityEngine.Rendering;

[RequireComponent(typeof(Terrain), typeof(AthmosphereControl))]
public class WorldGenerator : MonoBehaviour
{
    private Terrain terrain;

    public void Generate(WorldGeneratorArgs args, bool preview = false)
    {
        if (this.terrain == null)
            this.terrain = this.GetComponent<Terrain>();

        CreateBiomes(args);
        CreateHeightMap(args);
        CreateAlphaMap(args);
        CreateEntities(args);
        this.terrain.Flush();

        if (!preview)
        {
            TerrainCollider collider = this.GetComponent<TerrainCollider>();

            if (collider != null)
            {
                //Don't mind this garbage, collider didn't update for placed entities.
                collider.enabled = false;
                collider.enabled = true;
            }

            AthmosphereControl volBlender = this.GetComponent<AthmosphereControl>();
            volBlender.Init(args);
        }
    }

    private static void CreateBiomes(WorldGeneratorArgs args)
    {
        BiomeGenerator.GenerateBiomemapData(args, 1, 1);
    }

    private static void CreateHeightMap(WorldGeneratorArgs args)
    {
        int size = args.Terrain.terrainData.heightmapResolution;

        float[,] heightMap = new float[size, size];
        float maxTerrainHeight = args.Terrain.terrainData.heightmapScale.y;

        //int chunkOffsetX = chunkPos.x * (WIDTH - 1);
        //int chunkOffsetY = chunkPos.y * (HEIGHT - 1);

        for (int i = 0; i < args.BiomeCount; i++)
            args.GetBiome(i).HeightData.Prepare(args, 0, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float terrainHeight = 0;

                for(int i = 0; i < args.BiomeCount; i++)
                    terrainHeight += args.GetBiome(i).HeightData.GetHeight(args, x, y) * args.GetWeight(x, y, i);

                heightMap[x, y] = terrainHeight / maxTerrainHeight;
            }
        }

        args.Terrain.terrainData.SetHeights(0, 0, heightMap);
        args.Terrain.transform.position = new Vector3(0, -args.WaterLevel, 0);
    }

    private static void CreateAlphaMap(WorldGeneratorArgs args)
    {
        TerrainLayer slope = args.Slope;
        TerrainLayer inWater = args.InWater;
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
}
