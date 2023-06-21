using OpenAI_API.Chat;
using OpenAI_API.Models;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(WorldGenerator))]
public class WorldGeneratorInterface_MAD : WorldGeneratorInterface
{
    public bool isGenerated = false;
    public bool isFailed = false;

    public string extendedSeed;

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
    [Tooltip("Always ask ChatGPT, even if the prompt didn't change.")]
    private bool alwaysAskChatGPT = false;
    [SerializeField]
    [Tooltip("Prints progress to console.")]
    private bool showProgressLogs = false;

    private string prevPrompt;
    private BiomeData[] prevBiomeData;

    //private SOHeight[] biomeDistributionData; //Currently replaced by Bias and Randomness.

    public override void GenerateWorld()
    {
        this.isFailed = false;
        this.isGenerated = false;

        /*if (this.useDebugExtendedSeed && !string.IsNullOrEmpty(this.debugExtendedSeed))
        {
            Debug.Log("!!");
            GenerateWorldWithExtendedSeed(this.debugExtendedSeed);
            return;
        }*/

        this.GenerateWorldWithChatGPT(this.prompt, this.key);
    }

    public void GenerateWorldWithChatGPT(string prompt, string key)
    {
        this.prompt = prompt;
        this.key = key;
        this.GenerateWorldAsync();
    }

    private async void GenerateWorldAsync()
    {
        if (string.IsNullOrEmpty(this.prompt))
        {
            Debug.LogError("Your prompt is empty, please write a prompt so ChatGPT can help you out.");
            this.isFailed = true;
            return;
        }

        if (string.IsNullOrEmpty(this.key))
        {
            Debug.LogError("Your key is empty, please provide an OpenAI-API key so we can talk to ChatGPT for you.");
            this.isFailed = true;
            return;
        }

        try
        {
            if (this.worldGenerator == null)
                this.worldGenerator = this.GetComponent<WorldGenerator>();

            //User prompt changed (or alwaysAskChatGPT set to true), OpenAI communication necessary.
            if (this.alwaysAskChatGPT || this.prevPrompt != this.prompt)
            {
                BiomeData[] biomeData = await FetchBiomeDataFromOpenAI();

                ShowProgressMsg("Generating world from processed answer...");
                if (!this.fixedSeed)
                    this.seed = Random.Range(0, 999999);

                this.prevBiomeData = biomeData;
                this.Prepare(biomeData);
                this.worldGenerator.Generate();

                AthmosphereControl athmosphereControl = this.GetComponent<AthmosphereControl>();
                if (athmosphereControl != null)
                {
                    ShowProgressMsg("Found AthmosphereControl, asking ChatGPT for time of day...");
                    int hourOfDay = await FetchTimeFromOpenAI();
                    ShowProgressMsg($"Done! It's {hourOfDay} o'clock.");
                    athmosphereControl.SetTimeOfDay(hourOfDay);
                }
            }
            //User prompt did not change, just regenerate.
            else
            {
                ShowProgressMsg("Prompt didn't change, using previous ChatGPT-answer...");
                ShowProgressMsg("Generating world...");

                if (!this.fixedSeed)
                    this.seed = Random.Range(0, 999999);

                this.Prepare(this.prevBiomeData);
                this.worldGenerator.Generate();
            }

            this.prevPrompt = prompt;
            this.isGenerated = true;

            this.extendedSeed = this.GetExtendedSeed();
        }
        catch
        {
            this.isFailed = true;
        }
    }

    public void GenerateWorldWithExtendedSeed(string extendedSeed)
    {
        //Convert to usable data
        if (extendedSeed.Length > 11)
            extendedSeed.Remove(0, extendedSeed.Length - 11);

        string base10 = extendedSeed.ToBase10().ToString();

        if (base10.Length > 18)
            base10 = base10.Substring(0, 18);
        else if (base10.Length < 18)
            base10 = base10.PadLeft(18, '0');

        if (!int.TryParse(base10.Substring(0, 2), out int biome5Index) ||
            !int.TryParse(base10.Substring(2, 2), out int biome4Index) ||
            !int.TryParse(base10.Substring(4, 2), out int biome3Index) ||
            !int.TryParse(base10.Substring(6, 2), out int biome2Index) ||
            !int.TryParse(base10.Substring(8, 2), out int biome1Index) ||
            !int.TryParse(base10.Substring(10, 2), out int timeOfDay) ||
            !int.TryParse(base10.Substring(12), out int seed
        ))
            throw new System.ArgumentException();

        int[] biomeIndices = new int[]
        {
            biome1Index,
            biome2Index,
            biome3Index,
            biome4Index,
            biome5Index
        };

        SOBiome[] allBiomes = Resources.LoadAll<SOBiome>("WorldAI_DefaultAssets/Prefabs/Biomes/");
        List<SOBiome> selectedBiomes = new List<SOBiome>();
        int biomeCount = 0;
        System.Random random = new System.Random(seed);

        //Determine amount of biomes
        for (int i = biomeIndices.Length - 1; i >= 0; i--)
        {
            if (biomeIndices[i] != 0)
            {
                biomeCount = i + 1;
                break;
            }
        }

        //Fallback
        if (biomeCount == 0)
        {
            biomeCount = random.Next(1, 6);

            for(int i = 0; i < biomeCount; i++)
                biomeIndices[i] = random.Next(0, allBiomes.Length) + 1;
        }

        //Put biomes in list
        for(int i = 0; i < biomeCount; i++)
        {
            int biomeIndex = biomeIndices[i] - 1;

            if (biomeIndex >= allBiomes.Length)
                biomeIndex = random.Next(0, allBiomes.Length);

            selectedBiomes.Add(allBiomes[biomeIndex]);
        }

        //Set seed
        this.seed = seed;

        //Generate world
        this.Prepare(ConvertToBiomeData(allBiomes, selectedBiomes));
        this.worldGenerator.Generate();

        //Set time of day
        AthmosphereControl athmosphereControl = this.GetComponent<AthmosphereControl>();
        if (athmosphereControl != null)
        {
            Debug.Log(timeOfDay);
            athmosphereControl.SetTimeOfDay(timeOfDay);
        }
    }

    public string GetExtendedSeed()
    {
        SOBiome[] allBiomes = Resources.LoadAll<SOBiome>("WorldAI_DefaultAssets/Prefabs/Biomes/");
        int[] biomeIndices = new int[5];
        StringBuilder sb = new StringBuilder();

        //Determine biomes
        for (int i = 0; i < this.worldGenerator.Args.BiomeCount; i++)
        {
            biomeIndices[i] = System.Array.IndexOf(allBiomes, this.worldGenerator.Args.GetBiome(i).biome) + 1;
        }

        for (int i = biomeIndices.Length - 1; i >= 0; i--)
        {
            string index = biomeIndices[i].ToString();

            if (index.Length < 2)
                index = index.PadLeft(2, '0');
            if (index.Length > 2)
                throw new System.NotImplementedException();

            sb.Append(index);
        }

        //Determine time of day
        AthmosphereControl athmosphereControl = this.GetComponent<AthmosphereControl>();
        if (athmosphereControl != null)
            sb.Append(athmosphereControl.GetTimeOfDay().ToString().PadLeft(2, '0'));
        else
            sb.Append("13"); //Fallback

        //Determine seed
        string seed = this.worldGenerator.Args.Seed.ToString();
        if (seed.Length < 6)
            seed = seed.PadLeft(6, '0');
        if (seed.Length > 6)
            seed = seed.Substring(0, 6);

        sb.Append(seed);
        Debug.Log(sb.ToString());
        //Convert to Base36
        ulong base10 = System.Convert.ToUInt64(sb.ToString());
        return base10.ToBase36();
    }

    private async Task<BiomeData[]> FetchBiomeDataFromOpenAI()
    {
        ShowProgressMsg("Preparing Prompt...");
        SOBiome[] allBiomes = Resources.LoadAll<SOBiome>("WorldAI_DefaultAssets/Prefabs/Biomes/");
        string fullPrompt = GetFullPromptForBiomes(allBiomes, this.prompt);

        ShowProgressMsg("Waiting for OpenAI answer...");
        ChatResult result = await GetAnswerFromOpenAIAsync(fullPrompt, this.key);

        ShowProgressMsg($"Answer received! Prompt needed {result.Usage.PromptTokens} tokens and result used up {result.Usage.CompletionTokens} tokens.");
        ShowProgressMsg($"Result: {result.ToString()}");

        ShowProgressMsg("Converting answer to BiomeData[]...");
        List<SOBiome> selectedBiomes = ConvertToBiomes(allBiomes, result.ToString());
        return ConvertToBiomeData(allBiomes, selectedBiomes);
    }

    private async Task<int> FetchTimeFromOpenAI()
    {
        string fullPrompt = $"Given a prompt, please pick one hour of the day (as an integer in the range 0 to 24) that fits best (or pick randomly if in doubt). PROMPT: {this.prompt}";
        ChatResult result = await GetAnswerFromOpenAIAsync(fullPrompt, this.key);

        ShowProgressMsg($"Answer received! Prompt needed {result.Usage.PromptTokens} tokens and result used up {result.Usage.CompletionTokens} tokens.");
        ShowProgressMsg($"Result: {result.ToString()}");

        if (!this.TryParseHour(result.ToString(), out int hourOfDay))
        {
            hourOfDay = Random.Range(1, 25); //Fallback
        }

        return hourOfDay;
    }

    private bool TryParseHour(string result, out int hour)
    {
        string[] tryParse = result.Split(' ');

        foreach(string str in tryParse)
        {
            if (int.TryParse(str, out hour))
                return true;
        }

        hour = 0;
        return false;
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

    private static List<SOBiome> ConvertToBiomes(SOBiome[] allBiomes, string answer)
    {
        List<SOBiome> biomes = new List<SOBiome>();

        for(int i = 0; i < allBiomes.Length; i++)
        {
            if (answer.Contains(allBiomes[i].name))
                biomes.Add(allBiomes[i]);
        }

        return biomes;
    }

    private static BiomeData[] ConvertToBiomeData(SOBiome[] allBiomes, List<SOBiome> biomes)
    {   
        //Fallback
        if (biomes.Count == 0)
        {
            Debug.LogError("It seems like OpenAI had a hard time working with your prompt. Instead choosing 3 biomes randomly...");
            biomes.Add(allBiomes[Random.Range(0, allBiomes.Length)]);
            biomes.Add(allBiomes[Random.Range(0, allBiomes.Length)]);
            biomes.Add(allBiomes[Random.Range(0, allBiomes.Length)]);
        }

        //Randomly remove excess biomes.
        while (biomes.Count > 5)
            biomes.RemoveAt(Random.Range(0, biomes.Count));

        BiomeData[] biomeData = new BiomeData[biomes.Count];

        //Hardcoded biome-distribution.
        switch (biomes.Count)
        {
            case 1:
                biomeData[0] = new BiomeData()
                {
                    bias = new Vector2(0, 1),
                    random = new Vector2(0, 1),
                    biome = biomes[0]
                };
                break;
            case 2:
                biomeData[0] = new BiomeData()
                {
                    bias = new Vector2(0f, 0.5f),
                    random = new Vector2(0f, 1f),
                    biome = biomes[0]
                };
                biomeData[1] = new BiomeData()
                {
                    bias = new Vector2(0.5f, 1f),
                    random = new Vector2(0f, 1f),
                    biome = biomes[1]
                };
                break;
            case 3:
                biomeData[0] = new BiomeData()
                {
                    bias = new Vector2(0, 0.5f),
                    random = new Vector2(0, 0.5f),
                    biome = biomes[0]
                };
                biomeData[1] = new BiomeData()
                {
                    bias = new Vector2(0, 0.5f),
                    random = new Vector2(0.5f, 1f),
                    biome = biomes[1]
                };
                biomeData[2] = new BiomeData()
                {
                    bias = new Vector2(0.5f, 1),
                    random = new Vector2(0, 1),
                    biome = biomes[2]
                };
                break;
            case 4:
                biomeData[0] = new BiomeData()
                {
                    bias = new Vector2(0, 0.5f),
                    random = new Vector2(0, 0.5f),
                    biome = biomes[0]
                };
                biomeData[1] = new BiomeData()
                {
                    bias = new Vector2(0, 0.5f),
                    random = new Vector2(0.5f, 1f),
                    biome = biomes[1]
                };
                biomeData[2] = new BiomeData()
                {
                    bias = new Vector2(0.5f, 1),
                    random = new Vector2(0, 0.5f),
                    biome = biomes[2]
                };
                biomeData[3] = new BiomeData()
                {
                    bias = new Vector2(0.5f, 1),
                    random = new Vector2(0.5f, 1f),
                    biome = biomes[3]
                };
                break;
            case 5:
                biomeData[0] = new BiomeData()
                {
                    bias = new Vector2(0, 0.33f),
                    random = new Vector2(0, 0.5f),
                    biome = biomes[0]
                };
                biomeData[1] = new BiomeData()
                {
                    bias = new Vector2(0, 0.33f),
                    random = new Vector2(0.5f, 1f),
                    biome = biomes[1]
                };
                biomeData[2] = new BiomeData()
                {
                    bias = new Vector2(0.33f, 0.5f),
                    random = new Vector2(0, 0.5f),
                    biome = biomes[2]
                };
                biomeData[3] = new BiomeData()
                {
                    bias = new Vector2(0.33f, 0.5f),
                    random = new Vector2(0.5f, 1f),
                    biome = biomes[3]
                };
                biomeData[4] = new BiomeData()
                {
                    bias = new Vector2(0.5f, 1f),
                    random = new Vector2(0, 1f),
                    biome = biomes[4]
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

    [MenuItem("GameObject/3D Object/WorldAI/World (MAD)", false, 1)]
    public static void CreateNew()
    {
        GameObject go = new GameObject("World");
        go.AddComponent<WorldGeneratorInterface_MAD>();
        go.AddComponent<AthmosphereControl>();
        WorldGenerator wg = go.GetComponent<WorldGenerator>();
        WorldGenerator.CreateNew(wg);
    }

}
