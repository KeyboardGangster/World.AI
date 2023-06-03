using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using static UnityEngine.Mesh;
using UnityEngine.Rendering;
using UnityEditor;
using System.IO;


[RequireComponent(typeof(Terrain))]
public class WorldGenerator : MonoBehaviour
{
    [SerializeField]
    private WorldGeneratorArgs args;
    [SerializeField]
    private Transform waterPlane;
    [SerializeField]
    private TerrainLayer[] terrainLayers;

    /// <summary>
    /// Holds all the data necessary to generate a world. This needs to be initialized in a WorldGeneratorInterface.
    /// </summary>
    public WorldGeneratorArgs Args => this.args;

    public void Generate()
    {
        if (this.terrainLayers == null || this.terrainLayers.Length == 0 || this.terrainLayers.Length < Args._TerrainLayerCount)
        {
            Debug.LogError("Not enough referenced TerrainLayers. Aborting generation.");
            return;
        }

        CreateBiomes(Args);
        CreateHeightMap(Args);
        CreateAlphaMap(Args, this.terrainLayers);
        CreateEntities(Args);
        EditorUtility.SetDirty(Args);
        Terrain terrain = this.GetComponent<Terrain>();
        terrain.transform.position = new Vector3(0, -Args.WaterLevel, 0);
        terrain.Flush();

        TerrainCollider collider = this.GetComponent<TerrainCollider>();

        if (collider != null)
        {
            //Don't mind this garbage, collider didn't update for placed entities.
            collider.enabled = false;
            collider.enabled = true;
        }

        if (this.waterPlane == null)
        {
            this.waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane).transform;
            this.waterPlane.name = "Water";
            this.waterPlane.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("WorldAI_DefaultAssets/Water/_DEFAULT_WATER");
            Destroy(this.waterPlane.GetComponent<MeshCollider>());
        }

        float size = terrain.terrainData.size.x;
        this.waterPlane.transform.position = new Vector3(size / 2f, 0, size / 2f);
        this.waterPlane.transform.localScale = new Vector3(size / 10, 1, size / 10);
    }

    private static void CreateBiomes(WorldGeneratorArgs args)
    {
        BiomeGenerator.GenerateBiomemapData(args, 1, 1);
    }

    private static void CreateHeightMap(WorldGeneratorArgs args)
    {
        int size = args.TerrainData.heightmapResolution;

        float[,] heightMap = new float[size, size];
        float maxTerrainHeight = args.TerrainData.heightmapScale.y;

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
                    terrainHeight += args.GetBiome(i).HeightData.GetHeight(args, x, y) * args._GetWeight(x, y, i);

                heightMap[x, y] = terrainHeight / maxTerrainHeight;
            }
        }

        args.TerrainData.SetHeights(0, 0, heightMap);
    }

    private static void CreateAlphaMap(WorldGeneratorArgs args, TerrainLayer[] reference)
    {
        TerrainLayer slope = args._Slope;
        TerrainLayer inWater = args._InWater;

        TerrainLayer[] toSave = new TerrainLayer[args._TerrainLayerCount];

        for(int i = 0; i < toSave.Length; i++)
        {
            toSave[i] = reference[Mathf.Min(reference.Length - 1, i)]; //Mathf.Min to avoid indexoutofrange-exception.
            CopyToTerrainLayer(args._TerrainLayers[i], toSave[i]);
        }

        args.TerrainData.terrainLayers = toSave;

        int heightmapRes = args.TerrainData.heightmapResolution;
        int alphamapRes = args.TerrainData.alphamapResolution;
        float heightmapAlphamapRatio = heightmapRes / (float)alphamapRes;

        float[,] heightMap = args.TerrainData.GetHeights(0, 0, heightmapRes, heightmapRes);
        float[,,] alphaMap = new float[alphamapRes, alphamapRes, args._TerrainLayerCount];
        float waterLevel = args.WaterLevel / args.TerrainData.heightmapScale.y;

        for (int y = 0; y < alphamapRes; y++)
        {
            for(int x = 0; x < alphamapRes; x++)
            {
                int xInHeightMap = Mathf.FloorToInt(x * heightmapAlphamapRatio);
                int yInHeightMap = Mathf.FloorToInt(y * heightmapAlphamapRatio);

                BiomeData dominantBiome = args._GetDominantBiome(xInHeightMap, yInHeightMap);

                bool isSteep = false;
                float steepnessValue = 0;
                bool isUnderWater = false;

                //Set slopeTerrainLayer if biome doesn't supress it.
                if (!dominantBiome.biome.OverrideSteepTerrainLayer)
                {
                    //Don't ask me why it's inverted in there, I have no idea haha.
                    //I think the normalized positions are inverted to the actual heightmap-positions but idk...
                    var angle = args.TerrainData.GetSteepness(
                    (float)y / (alphamapRes - 1),
                    (float)x / (alphamapRes - 1));

                    if (angle > 30f)
                    {
                        isSteep = true;
                        steepnessValue = (float)(angle / 90.0);
                        alphaMap[x, y, args._GetTerrainLayerIndex(slope)] = steepnessValue;
                    }
                }
                //Set inWaterTerrainLayer if biome doesn't supress it.
                if (!isSteep && !dominantBiome.biome.OverrideInWaterTerrainLayer && heightMap[xInHeightMap, yInHeightMap] < waterLevel)
                {
                    alphaMap[x, y, args._GetTerrainLayerIndex(inWater)] = 1f;
                }
                //Set biome-specific terrainLayer.
                if (!isUnderWater)
                {
                    for (int i = 0; i < args.BiomeCount; i++)
                    {
                        if (args._GetTerrainLayerIndex(args.GetBiome(i).biome.BaseTerrainLayer) != args._GetTerrainLayerIndex(slope))
                            alphaMap[x, y, args._GetTerrainLayerIndex(args.GetBiome(i).biome.BaseTerrainLayer)] += args._GetWeight(xInHeightMap, yInHeightMap, i) - steepnessValue;
                        else
                            alphaMap[x, y,args._GetTerrainLayerIndex(args.GetBiome(i).biome.BaseTerrainLayer)] += args._GetWeight(xInHeightMap, yInHeightMap, i);
                    }
                }
            }
        }

        args.TerrainData.SetAlphamaps(0, 0, alphaMap);
    }

    private static void CreateEntities(WorldGeneratorArgs args)
    {
        EntityPlacer.Place(args);
    }

    private void Reset()
    {
        //This happens when WorldGenerator is first created and not yet fully initialized.
        //CreateNew(...) needs to be called first.
        if (this.terrainLayers == null)
            return;

        Terrain terrain = this.GetComponent<Terrain>();
        TerrainCollider collider = this.GetComponent<TerrainCollider>();

        //This can happen when Terrain is added as component beforehand (don't do that)
        if (terrain.terrainData == null)
        {
            TerrainData terrainData = new TerrainData();
            AssetDatabase.CreateAsset(terrainData, $"Assets/New TerrainData.asset");
            terrain.terrainData = terrainData;
            Material terrainLit = Resources.Load<Material>("WorldAI_DefaultAssets/_DEFAULT_TERRAIN_LIT");
            terrain.materialTemplate = terrainLit;
        }

        if (collider == null)
        {
            collider = this.gameObject.AddComponent<TerrainCollider>();
            collider.terrainData = terrain.terrainData;
        }
    }

    public static void CreateNew(WorldGenerator instance)
    {
        string guid = AssetDatabase.CreateFolder("Assets", "World");
        string path = AssetDatabase.GUIDToAssetPath(guid);

        TerrainData terrainData = new TerrainData();
        AssetDatabase.CreateAsset(terrainData, $"{path}/TerrainData.asset");
        
        Terrain terrain = instance.GetComponent<Terrain>();
        terrain.terrainData = terrainData;
        Material terrainLit = Resources.Load<Material>("WorldAI_DefaultAssets/_DEFAULT_TERRAIN_LIT");
        terrain.materialTemplate = terrainLit;

        TerrainLayer[] tl = new TerrainLayer[8];

        for (int i = 0; i < tl.Length; i++)
        {
            if (tl[i] == null)
            {
                tl[i] = new TerrainLayer();
                AssetDatabase.CreateAsset(tl[i], $"{path}/TerrainLayer{i}.asset");
            }
        }

        WorldGeneratorArgs args = ScriptableObject.CreateInstance<WorldGeneratorArgs>();
        instance.args = args;
        AssetDatabase.CreateAsset(args, $"{path}/WorldGeneratorArgs.asset");
        args.hideFlags = HideFlags.NotEditable;

        instance.terrainLayers = tl;
        TerrainCollider col = instance.gameObject.AddComponent<TerrainCollider>();
        col.terrainData = terrainData;

        UnityEditorInternal.ComponentUtility.MoveComponentUp(col);
        UnityEditorInternal.ComponentUtility.MoveComponentUp(col);
        UnityEditorInternal.ComponentUtility.MoveComponentUp(col);
    }

    private static void CopyToTerrainLayer(TerrainLayer from, TerrainLayer to)
    {
        to.maskMapTexture = from.maskMapTexture;
        to.maskMapRemapMin = from.maskMapRemapMin;
        to.maskMapRemapMax = from.maskMapRemapMax;

        to.normalMapTexture = from.normalMapTexture;
        to.normalScale = from.normalScale;

        to.diffuseTexture = from.diffuseTexture;
        to.diffuseRemapMin = from.diffuseRemapMin;
        to.diffuseRemapMax = from.diffuseRemapMax;

        to.tileSize = from.tileSize;
        to.tileOffset = from.tileOffset;

        to.specular = from.specular;
        to.metallic = from.metallic;
        to.smoothness = from.smoothness;
    }
}
