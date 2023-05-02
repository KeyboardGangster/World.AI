using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "WorldAI/Grass")]
public class SOGrass : ScriptableObject
{
    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private Vector2 width;
    [SerializeField]
    private Vector2 height;
    [SerializeField]
    private int seed;
    [Range(0, 100)]
    [SerializeField]
    private float noiseSpread;

    [SerializeField]
    private bool placeUnderwater;
    [SerializeField]
    private bool placeOnSlopes;

    public GameObject Prefab => this.prefab;
    public Vector2 Width => this.width;
    public Vector2 Height => this.height;
    public int Seed => this.seed;
    public float NoiseSpread => this.noiseSpread;

    public bool PlaceUnderwater => this.placeUnderwater;
    public bool PlaceOnSlopes => this.placeOnSlopes;
}