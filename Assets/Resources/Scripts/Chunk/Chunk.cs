using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public static readonly int WIDTH = 100;
    public static readonly int HEIGHT = 100;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    public Vector2Int chunkPos;
    private MeshData meshData;
    private Texture2D texture;

    public void Generate(Vector2Int chunkPos, float[,][] weights, BiomeData[] biomes, Dictionary<Vector2, Chunk> chunks)
    {
        this.chunkPos = chunkPos;

        this.meshRenderer = this.transform.GetComponent<MeshRenderer>();
        this.meshFilter = this.transform.GetComponent<MeshFilter>();

        this.PrepareMeshData(weights, biomes, chunks);
        Mesh mesh = this.meshData.CreateMesh();
        meshFilter.sharedMesh = mesh;

        this.PrepareTexture(weights, biomes);
        this.meshRenderer.sharedMaterial = Instantiate(this.meshRenderer.sharedMaterial);
        this.meshRenderer.sharedMaterial.mainTexture = texture;

        this.transform.position = new Vector3(this.chunkPos.x * (WIDTH - 1), 0, this.chunkPos.y * (HEIGHT - 1));
        this.name = $"({this.chunkPos.x}, {this.chunkPos.y})";
    }

    private void PrepareMeshData(float[,][] weights, BiomeData[] biomes, Dictionary<Vector2, Chunk> chunks)
    {
        this.meshData = new MeshData(WIDTH, HEIGHT);
        int vertexIndex = 0;

        Vector2[][] octaveOffsets = new Vector2[biomes.Length][];
        float[] maxValues = new float[biomes.Length];

        int chunkOffsetX = chunkPos.x * (WIDTH - 1);
        int chunkOffsetY = chunkPos.y * (HEIGHT - 1);

        for (int i = 0; i < biomes.Length; i++)
            octaveOffsets[i] = Synthesizer.CalculateOctaveOffsets(chunkOffsetX, chunkOffsetY, biomes[i].noiseData, out maxValues[i]);

        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                float terrainHeight = Synthesizer.CalculateCompoundNoiseValue(x, y, octaveOffsets, biomes, maxValues, weights[chunkOffsetX + x, chunkOffsetY + y]);

                this.SetMeshDataAtPosition(x, y, vertexIndex, terrainHeight, chunks);
                vertexIndex++;
            }
        }
    }

    private void SetMeshDataAtPosition(int x, int y, int vertexIndex, float terrainHeight, Dictionary<Vector2, Chunk> chunks)
    {
        meshData.vertices[vertexIndex] = new Vector3(x, terrainHeight, y);
        meshData.uvs[vertexIndex] = new Vector2(x / (float)WIDTH, y / (float)WIDTH);

        //Add triangles
        if (x < WIDTH - 1 && y < HEIGHT - 1)
        {
            //Counter-clockwise
            meshData.AddTriangle(vertexIndex, vertexIndex + WIDTH, vertexIndex + WIDTH + 1);
            meshData.AddTriangle(vertexIndex, vertexIndex + WIDTH + 1, vertexIndex + 1);
        }

        //Set normals. If at edge and can't calculate normals, take the normals of neighbour chunk.
        if (x > 0 && y > 0)
        {
            meshData.normals[vertexIndex] = -Vector3.Cross(
                meshData.vertices[vertexIndex - 1] - meshData.vertices[vertexIndex],
                meshData.vertices[vertexIndex - WIDTH] - meshData.vertices[vertexIndex]);

            //Smoothing out the terrain here is done because bad blending code causes ugly lines to be seen all over.
            //This here is meant to reduce those. If blending gets fixed, this isn't needed.
            if (x < WIDTH - 1 && y < HEIGHT - 1)
                meshData.vertices[vertexIndex] =
                    (meshData.vertices[vertexIndex] +
                    meshData.vertices[vertexIndex - 1] +
                    meshData.vertices[vertexIndex - WIDTH]) / 3f;
        }
        else
        {
            if (x == 0 && chunks.TryGetValue(new Vector2(chunkPos.x - 1, chunkPos.y), out Chunk left))
            {
                meshData.normals[vertexIndex] = left.meshData.normals[y * WIDTH + (WIDTH - 1)];
            }
            else if (y == 0 && chunks.TryGetValue(new Vector2(chunkPos.x, chunkPos.y - 1), out Chunk down))
            {
                meshData.normals[vertexIndex] = down.meshData.normals[(HEIGHT - 1) * WIDTH + x];
            }
            else
            {
                meshData.normals[vertexIndex] = Vector3.up;
            }
        }
    }

    private void PrepareTexture(float[,][] weights, BiomeData[] biomes)
    {
        this.texture = new Texture2D(WIDTH, HEIGHT);
        this.texture.filterMode = FilterMode.Point;

        int chunkOffsetX = this.chunkPos.x * WIDTH;
        int chunkOffsetY = this.chunkPos.y * HEIGHT;

        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                int index = 0;
                float prevWeight = 0;

                for (int i = 0; i < biomes.Length; i++)
                {
                    if (weights[x + chunkOffsetX, y + chunkOffsetY][i] > prevWeight)
                    {
                        prevWeight = weights[x + chunkOffsetX, y + chunkOffsetY][i];
                        index = i;
                    }
                }

                this.texture.SetPixel(x, y, biomes[index].feature * Random.Range(0.75f, 1.25f));
            }
        }

        this.texture.Apply();
    }
}
