using OpenAI_API.Chat;
using OpenAI_API.Models;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(WorldGenerator))]
public class WorldGeneratorInterface_AI : WorldGeneratorInterface
{
    private Terrain terrain;
    private WorldGenerator worldGenerator;

    [SerializeField]
    [TextArea(3, 10)]
    private string prompt;
    [SerializeField]
    private string key;
    [SerializeField]
    private WorldSize size;
    [SerializeField]
    private int seed = 1;
    [SerializeField]
    private bool fixedSeed = false;
    [SerializeField]
    [Tooltip("Always ask ChatGPT, even if the prompt didn't change. (Not recommended)")]
    private bool alwaysAskChatGPT = false;
    [SerializeField]
    [Tooltip("Prints progress to console.")]
    private bool showProgressLogs = false;

    private string prevPrompt;
    private BiomeData[] prevBiomeData;

    //private SOHeight[] biomeDistributionData; //Currently replaced by Bias and Randomness.

    public override async void GenerateWorld()
    {
        if (string.IsNullOrEmpty(this.prompt))
        {
            Debug.LogError("Your prompt is empty, please write a prompt so ChatGPT can help you out.");
            return;
        }

        if (this.worldGenerator == null)
            this.worldGenerator = this.GetComponent<WorldGenerator>();

        //User prompt changed (or alwaysAskChatGPT set to true), OpenAI communication necessary.
        if (this.alwaysAskChatGPT || this.prevPrompt != this.prompt)
        {
            BiomeData[] biomeData = await FetchWorldsFromOpenAI();

            //Disabled, cos' it triggers some Unity-bug that causes ultrabright night when switching directly from day to night.
            //Didn't want the world to be generated in such a state so it's disabled for now, but it works.
            /*AthmosphereControl athmosphereControl = this.GetComponent<AthmosphereControl>();
            if (athmosphereControl != null)
            {
                ShowProgressMsg("Found AthmosphereControl, asking ChatGPT for time of day...");
                int hourOfDay = await FetchTimeFromOpenAI();
                ShowProgressMsg($"Done! It's {hourOfDay} o'clock.");
                athmosphereControl.SetTimeOfDay(hourOfDay);
            }*/

            ShowProgressMsg("Generating world from processed answer...");
            if (!this.fixedSeed)
                this.seed = Random.Range(0, 9999999);

            this.prevBiomeData = biomeData;
            this.Prepare(biomeData);
            this.worldGenerator.Generate();
        }
        //User prompt did not change, just regenerate.
        else
        {
            ShowProgressMsg("Prompt didn't change, using previous ChatGPT-answer...");
            ShowProgressMsg("Generating world...");

            if(!this.fixedSeed)
                this.seed = Random.Range(0, 9999999);

            this.Prepare(this.prevBiomeData);
            this.worldGenerator.Generate();
        }

        this.prevPrompt = prompt;
    }

    private async Task<BiomeData[]> FetchWorldsFromOpenAI()
    {
        ShowProgressMsg("Preparing Prompt...");
        SOBiome[] biomes = Resources.LoadAll<SOBiome>("WorldAI_DefaultAssets/Prefabs/Biomes/");
        string fullPrompt = GetFullPromptForBiomes(biomes, this.prompt);

        ShowProgressMsg("Waiting for OpenAI answer...");
        ChatResult result = await GetAnswerFromOpenAIAsync(fullPrompt, this.key);

        ShowProgressMsg($"Answer received! Prompt needed {result.Usage.PromptTokens} tokens and result used up {result.Usage.CompletionTokens} tokens.");
        ShowProgressMsg($"Result: {result.ToString()}");

        ShowProgressMsg("Converting answer to BiomeData[]...");
        return ConvertToBiomes(biomes, result.ToString());
    }

    private async Task<int> FetchTimeFromOpenAI()
    {
        string fullPrompt = $"Given a prompt, please pick one hour of the day (as an integer in the range 0 to 24) that fits best (or pick randomly if in doubt). PROMPT: {this.prompt}";
        ChatResult result = await GetAnswerFromOpenAIAsync(fullPrompt, this.key);

        ShowProgressMsg($"Answer received! Prompt needed {result.Usage.PromptTokens} tokens and result used up {result.Usage.CompletionTokens} tokens.");
        ShowProgressMsg($"Result: {result.ToString()}");

        if (!int.TryParse(result.ToString(), out int hourOfDay))
        {
            hourOfDay = Random.Range(1, 25); //Fallback
        }

        return hourOfDay;
    }

    private static string GetFullPromptForBiomes(SOBiome[] biomes, string userInput)
    {
        StringBuilder fullPrompt = new StringBuilder();
        fullPrompt.Append("Given is an array of possible biomes to choose from with their names and descriptions: ");

        foreach (SOBiome b in biomes)
        {
            fullPrompt.Append($"[name: {b.name}, description: {b.Description}], ");
        }

        fullPrompt.Append("Based on the array above please pick one to five of the provided biomes " +
            "which would be the best choice (please keep it to a single theme/ climate unless stated otherwise) to use for the following prompt and please use following " +
            "format for the answer: FORMAT: '[Biome1], [Biome2], [Biome3], etc...' ");
        fullPrompt.Append($"PROMPT: '{userInput}'");
        return fullPrompt.ToString();
    }

    private static async Task<ChatResult> GetAnswerFromOpenAIAsync(string prompt, string key)
    {
        var api = new OpenAI_API.OpenAIAPI(key);
        var result = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            Temperature = 0.2,
            MaxTokens = 100,
            Messages = new ChatMessage[] {
            new ChatMessage(ChatMessageRole.User, prompt)
        }
        });

        return result;
    }

    private static BiomeData[] ConvertToBiomes(SOBiome[] biomes, string answer)
    {
        List<SOBiome> result = new List<SOBiome>();

        for(int i = 0; i < biomes.Length; i++)
        {
            if (answer.Contains(biomes[i].name))
                result.Add(biomes[i]);
        }

        //Fallback
        if (result.Count == 0)
        {
            Debug.LogError("It seems like OpenAI had a hard time working with your prompt. Instead choosing 3 biomes randomly...");
            result.Add(biomes[Random.Range(0, biomes.Length)]);
            result.Add(biomes[Random.Range(0, biomes.Length)]);
            result.Add(biomes[Random.Range(0, biomes.Length)]);
        }

        //Randomly remove excess biomes.
        while (result.Count > 5)
            result.RemoveAt(Random.Range(0, result.Count));

        BiomeData[] biomeData = new BiomeData[result.Count];

        //Hardcoded biome-distribution.
        switch (result.Count)
        {
            case 1:
                biomeData[0] = new BiomeData()
                {
                    bias =      new Vector2(0, 1),
                    random =    new Vector2(0, 1),
                    biome = result[0]
                };
                break;
            case 2:
                biomeData[0] = new BiomeData()
                {
                    bias =      new Vector2(0f, 0.5f),
                    random =    new Vector2(0f, 1f),
                    biome = result[0]
                };
                biomeData[1] = new BiomeData()
                {
                    bias =      new Vector2(0.5f, 1f),
                    random =    new Vector2(0f, 1f),
                    biome = result[1]
                };
                break;
            case 3:
                biomeData[0] = new BiomeData()
                {
                    bias =      new Vector2(0, 0.5f),
                    random =    new Vector2(0, 0.5f),
                    biome = result[0]
                };
                biomeData[1] = new BiomeData()
                {
                    bias =      new Vector2(0, 0.5f),
                    random =    new Vector2(0.5f, 1f),
                    biome = result[1]
                };
                biomeData[2] = new BiomeData()
                {
                    bias =      new Vector2(0.5f, 1),
                    random =    new Vector2(0, 1),
                    biome = result[2]
                };
                break;
            case 4:
                biomeData[0] = new BiomeData()
                {
                    bias =      new Vector2(0, 0.5f),
                    random =    new Vector2(0, 0.5f),
                    biome = result[0]
                };
                biomeData[1] = new BiomeData()
                {
                    bias =      new Vector2(0, 0.5f),
                    random =    new Vector2(0.5f, 1f),
                    biome = result[1]
                };
                biomeData[2] = new BiomeData()
                {
                    bias =      new Vector2(0.5f, 1),
                    random =    new Vector2(0, 0.5f),
                    biome = result[2]
                }; 
                biomeData[3] = new BiomeData()
                {
                    bias =      new Vector2(0.5f, 1),
                    random =    new Vector2(0.5f, 1f),
                    biome = result[3]
                };
                break;
            case 5:
                biomeData[0] = new BiomeData()
                {
                    bias =      new Vector2(0, 0.33f),
                    random =    new Vector2(0, 0.5f),
                    biome = result[0]
                };
                biomeData[1] = new BiomeData()
                {
                    bias =      new Vector2(0, 0.33f),
                    random =    new Vector2(0.5f, 1f),
                    biome = result[1]
                };
                biomeData[2] = new BiomeData()
                {
                    bias =      new Vector2(0.33f, 0.5f),
                    random =    new Vector2(0, 0.5f),
                    biome = result[2]
                };
                biomeData[3] = new BiomeData()
                {
                    bias =      new Vector2(0.33f, 0.5f),
                    random =    new Vector2(0.5f, 1f),
                    biome = result[3]
                };
                biomeData[4] = new BiomeData()
                {
                    bias =      new Vector2(0.5f, 1f),
                    random =    new Vector2(0, 1f),
                    biome = result[4]
                };
                break;
            default:
                throw new System.NotImplementedException();
        }

        return biomeData;
    }

    private void ShowProgressMsg(string msg)
    {
        if (this.showProgressLogs)
            Debug.Log(msg);
    }

    private void Prepare(BiomeData[] biomeData)
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

        SOHeight bias = Resources.Load<SOHeight>("WorldAI_DefaultAssets/Prefabs/Height/_DEFAULT_BIAS");
        SOHeight randomness = Resources.Load<SOHeight>("WorldAI_DefaultAssets/Prefabs/Height/_DEFAULT_RANDOMNESS");

        //1.Prepare Args
        //System.Array.Sort(biomeData);
        this.GetComponent<WorldGenerator>().Args.CreateNew(
            this.terrain.terrainData,
            this.seed,
            biomeScale,
            new SOHeight[] { bias, randomness },
            biomeData,
            slopeTerrainLayer,
            inWaterTerrainLayer,
            worldScaleRatio,
            toyScaleRatio,
            waterLevel
        );
    }

    private void Awake()
    {
        this.terrain = this.GetComponent<Terrain>();
        this.worldGenerator = this.GetComponent<WorldGenerator>();
    }

    private void Reset()
    {
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

    [MenuItem("GameObject/3D Object/WorldAI/World (ChatGPT)", false, 1)]
    public static void CreateNew()
    {
        GameObject go = new GameObject("World");
        go.AddComponent<WorldGeneratorInterface_AI>();
        go.AddComponent<AthmosphereControl>();
        WorldGenerator wg = go.GetComponent<WorldGenerator>();
        WorldGenerator.CreateNew(wg);
    }
}
