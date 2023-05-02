using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "WorldAI/Biome")]
public class SOBiome : ScriptableObject
{
    [SerializeField]
    [TextArea(3, 10)]
    [Tooltip("150 words max!")]
    private string description;

    [Header("Biome Type")]
    [SerializeField]
    private NoiseData noiseData;
    [SerializeField]
    private float falloffRate;

    [Header("Texturing")]
    [SerializeField]
    private TerrainLayer baseTerrainLayer;
    [SerializeField]
    private bool overrideSteepTerrainLayer;
    [SerializeField]
    private bool overrideInWaterTerrainLayer;

    [Header("Tree-Settings")]
    [SerializeField]
    private TreeData[] trees;
    [SerializeField]
    private bool forceRemoveTreesUnderwater;

    [Header("Grass-Settings")]
    [SerializeField]
    private GrassData[] grass;
    [Range(0f, 2f)]
    [SerializeField]
    private float grassNoiseScale;

    [Header("Rock-Settings")]
    [SerializeField]
    private PropagationData[] propagationData;

    public string Description => this.description;
    public NoiseData NoiseData => this.noiseData;
    public float FalloffRate => this.falloffRate;
    public TerrainLayer BaseTerrainLayer => this.baseTerrainLayer;
    public bool OverrideSteepTerrainLayer => this.overrideSteepTerrainLayer;
    public bool OverrideInWaterTerrainLayer => this.overrideInWaterTerrainLayer;
    public TreeData[] Trees => this.trees;
    public bool ForceRemoveTreesUnderwater => this.forceRemoveTreesUnderwater;
    public GrassData[] Grass => this.grass;
    public float GrassNoiseScale => this.grassNoiseScale;

    public PropagationData[] PropagationData => this.propagationData;

    private void OnValidate()
    {
        if (this.description.Length > 150)
        {
            this.description = this.description.Remove(149);
            Debug.LogWarning("Your description is too long, please shorten it to max 150 words.");
        }
    }
}
