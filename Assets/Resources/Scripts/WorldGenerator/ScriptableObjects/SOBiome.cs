using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "WorldAI/Biome")]
public class SOBiome : ScriptableObject
{
    [SerializeField]
    [TextArea(3, 10)]
    [Tooltip("150 words max!")]
    private string description;
    /*
    [SerializeField]
    private Volume volume;*/
    [SerializeField]
    private SOBiomeLighting biomeLighting;

    [Header("Biome Type")]
    [SerializeField]
    private SOHeight height;
    [SerializeField]
    private float falloffRate = 0.03f;

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
    private float grassNoiseScale = 1;

    [Header("Rock-Settings")]
    [SerializeField]
    private PropagationData[] propagationData;

    public string Description => this.description;
    public SOBiomeLighting Lighting => this.biomeLighting;
    public SOHeight Height => this.height;
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
        if (this.description != null && this.description.Length > 150)
        {
            this.description = this.description.Remove(149);
            Debug.LogWarning("Your description is too long, please shorten it to max 150 words.");
        }

        if (this.falloffRate <= 0.005f)
            this.falloffRate = 0.005f;
    }
}
