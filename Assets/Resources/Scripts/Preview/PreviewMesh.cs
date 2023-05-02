using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mesh;

public class PreviewMesh : MonoBehaviour
{

    public static readonly int WIDTH = 100;
    public static readonly int HEIGHT = 100;

    [Header("Dependencies")]
    public GameObject prefab;

    [Header("World Data")]
    public int horizontalChunks = 1;
    public int verticalChunks = 1;

    [Header("Noise Data")]
    public string noiseDataName = "X";
    [SerializeField]
    NoiseData noiseData;

    [Header("Misc")]
    public bool autoUpdate = false;

    private Dictionary<Vector2, GameObject> chunks = new Dictionary<Vector2, GameObject>();


    public void Preview()
    {
        this.DestroyChunks();

        for (int chunkY = 0; chunkY < verticalChunks; chunkY++)
        {
            for (int chunkX = 0; chunkX < horizontalChunks; chunkX++)
            {
                GameObject obj = Instantiate(this.prefab, this.transform);
                MeshFilter filter = obj.GetComponent<MeshFilter>();

                Vector2Int chunkPos = new Vector2Int(chunkX, chunkY);

                //NoiseData noiseData = new NoiseData(this.offset, this.heightMultiplier, this.heightAddend, this.noiseScale, this.octaves, this.lacunarity, this.persistance, this.seed);

                PrepareMesh(chunkPos, noiseData, filter);

                obj.transform.position = new Vector3(chunkPos.x * (WIDTH - 1), 0, chunkPos.y * (HEIGHT - 1));
                obj.name = $"({chunkPos.x}, {chunkPos.y})";

                this.chunks.Add(chunkPos, obj);
            }
        }
    }

    private void PrepareMesh(Vector2Int chunkPos, NoiseData noiseData, MeshFilter meshFilter)
    {
        MeshData meshData = new MeshData(WIDTH, HEIGHT);
        int vertexIndex = 0;

        Vector2[] octaveOffsets = new Vector2[noiseData.noise.Octaves];
        int chunkOffsetX = chunkPos.x * (WIDTH - 1);
        int chunkOffsetY = chunkPos.y * (HEIGHT - 1);

        octaveOffsets = Synthesizer.CalculateOctaveOffsets(chunkOffsetX, chunkOffsetY, 0, noiseData, out float maxValue);

        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                float terrainHeight = Synthesizer.CalculateNoiseValue(x, y, octaveOffsets, noiseData, maxValue);

                meshData.vertices[vertexIndex] = new Vector3(x, terrainHeight, y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)WIDTH, y / (float)WIDTH);

                //Add triangles
                if (x < WIDTH - 1 && y < HEIGHT - 1)
                {
                    //Counter-clockwise
                    meshData.AddTriangle(vertexIndex, vertexIndex + WIDTH, vertexIndex + WIDTH + 1);
                    meshData.AddTriangle(vertexIndex, vertexIndex + WIDTH + 1, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        Mesh mesh = meshData.CreateMesh();
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;
    }

    #region Meta

    private void OnValidate()
    {
        if (this.horizontalChunks <= 0)
            this.horizontalChunks = 1;

        if (this.verticalChunks <= 0)
            this.verticalChunks = 1;

        /*if (this.heightMultiplier == 0)
            this.heightMultiplier = 0.01f;

        if (this.noiseScale == 0)
            this.noiseScale = 0.01f;

        if (octaves <= 0)
            octaves = 1;*/
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

    public void AddColliders()
    {
        foreach (Vector2 chunkPos in this.chunks.Keys)
        {
            if (this.chunks[chunkPos] != null && this.chunks[chunkPos].GetComponent<MeshCollider>() == null)
                this.chunks[chunkPos].AddComponent<MeshCollider>();
        }
    }

    public void PrintNoiseData()
    {
        String nd = $"public static readonly NoiseData {this.noiseDataName.ToUpper()} = new NoiseData(" +
            $"new Vector2({this.noiseData.noise.Offset.x}f, {this.noiseData.noise.Offset.y}f), " +
            $"{this.noiseData.noise.HeightMultiplier}f, " +
            $"{this.noiseData.noise.HeightAddend}f, " +
            $"{this.noiseData.noise.NoiseScale}f, " +
            $"{this.noiseData.noise.Octaves}, " +
            $"{this.noiseData.noise.Lacunarity}f, " +
            $"{this.noiseData.noise.Persistance}f, " +
            $"new NoisePattern[] {{";

        for(int i = 0; i < this.noiseData.noise.Octaves; i++)
        {
            nd += $"NoisePattern.{this.noiseData.noise.Pattern[i].ToString()}";

            if (i < this.noiseData.noise.Octaves - 1)
                nd += ", ";
        }

        nd += "});";

        Debug.Log(nd);
    }
    #endregion
}
