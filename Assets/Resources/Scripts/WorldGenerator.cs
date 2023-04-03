using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [SerializeField]
    private Chunk chunkPrefab;

    [SerializeField]
    [Tooltip("1:_ scale.")]
    private float worldScaleRatio = 1f;

    [SerializeField]
    private int seed = 1;
    [SerializeField]
    private float noiseScale = 20;
    [SerializeField]
    private Vector2Int chunksAmount;
    [SerializeField]
    private BiomeData[] biomeData;

    private Dictionary<Vector2, Chunk> chunks = new Dictionary<Vector2, Chunk>();

    public void GenerateChunks()
    {
        DestroyChunks();

        System.Array.Sort(this.biomeData);
        BiomeData[] biomes = new BiomeData[this.biomeData.Length];

        for(int i = 0; i < this.biomeData.Length; i++)
        {
            biomes[i] = this.biomeData[i];

            biomes[i].noiseData = BiomeGenerator.GetBiomeNoiseData(this.biomeData[i].biome);
            biomes[i].noiseData.noiseScale /= this.worldScaleRatio;
            biomes[i].noiseData.heightMultiplier /= this.worldScaleRatio;
            biomes[i].noiseData.heightAddend /= this.worldScaleRatio;
            biomes[i].falloffRate *= this.worldScaleRatio;
        }

        NoiseData biasData = new NoiseData(Vector2.zero, 1, 0, this.noiseScale * 30, 2, 2, 0.5f, this.seed);
        NoiseData randomnessData = new NoiseData(Vector2.one * 64000, 1, 0, this.noiseScale * 15, 2, 2, 0.5f, this.seed);
        biasData.noiseScale /= this.worldScaleRatio;
        randomnessData.noiseScale /= this.worldScaleRatio;


        float[,][] weights = BiomeGenerator.GetBiomeMap(this.chunksAmount.x, this.chunksAmount.y, Chunk.WIDTH, Chunk.HEIGHT, biomes, biasData, randomnessData);

        for (int chunkY = 0; chunkY < this.chunksAmount.y; chunkY++)
        {
            for (int chunkX = 0; chunkX < this.chunksAmount.x; chunkX++)
            {
                Chunk c = Instantiate(this.chunkPrefab, this.transform);
                c.Generate(new Vector2Int(chunkX, chunkY), weights, biomes, chunks);
                this.chunks.Add(new Vector2(chunkX, chunkY), c);
                c.gameObject.AddComponent<MeshCollider>();
            }
        }
    }

    public void DestroyChunks(bool bufferedOnly = true)
    {
        if (bufferedOnly)
        {
            foreach (Vector2 chunkPos in this.chunks.Keys)
            {
                if (this.chunks[chunkPos] != null)
                    DestroyImmediate(this.chunks[chunkPos].gameObject);
            }
        }
        else
        {
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }

        this.chunks.Clear();
    }

    private void Start()
    {
        //GenerateChunks();
    }

    private void OnValidate()
    {
        this.chunksAmount.x = Mathf.Clamp(this.chunksAmount.x, 1, 50);
        this.chunksAmount.y = Mathf.Clamp(this.chunksAmount.y, 1, 50);
        this.worldScaleRatio = Mathf.Clamp(this.worldScaleRatio, 1, 10);
        this.noiseScale = Mathf.Max(0.01f, this.noiseScale);
    }
}
