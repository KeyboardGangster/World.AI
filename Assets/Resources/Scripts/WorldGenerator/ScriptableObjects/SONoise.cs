using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "WorldAI/Noise")]
public class SONoise : ScriptableObject
{
    [Range(1, 6)]
    [SerializeField]
    private int octaves;
    [Range(0.5f, 3f)]
    [SerializeField]
    private float lacunarity;
    [Range(0.01f, 1)]
    [SerializeField]
    private float persistance;

    [SerializeField]
    private float heightAddend;
    [SerializeField]
    private float heightMultiplier;
    [Range(10f, 1000f)]
    [SerializeField]
    private float noiseScale;

    [SerializeField]
    private Vector2 offset;

    [SerializeField]
    private NoisePattern[] pattern;

    public SONoise()
    {
        octaves = 1;
        lacunarity = 2f;
        persistance = 0.5f;

        heightAddend = 0f;
        heightMultiplier = 5f;
        noiseScale = 40f;

        pattern = new NoisePattern[]
        {
            NoisePattern.Default
        };
    }

    public int Octaves => this.octaves;
    public float Lacunarity => this.lacunarity;
    public float Persistance => this.persistance;

    public float HeightAddend => this.heightAddend;
    public float HeightMultiplier => this.heightMultiplier;
    public float NoiseScale => this.noiseScale;

    public Vector2 Offset => this.offset;

    public NoisePattern[] Pattern => this.pattern;

    public void OnValidate()
    {
        if (octaves < 1)
            octaves = 1;

        if (octaves != this.pattern.Length)
        {
            NoisePattern[] pattern = new NoisePattern[octaves];

            for(int i = 0; i < pattern.Length; i++)
            {
                if (i < this.pattern.Length)
                    pattern[i] = this.pattern[i];
                else
                    pattern[i] = NoisePattern.Default;
            }

            this.pattern = pattern;
        }
    }
}
