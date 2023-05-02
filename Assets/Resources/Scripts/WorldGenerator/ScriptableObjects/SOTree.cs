using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "WorldAI/Tree")]
public class SOTree : ScriptableObject
{
    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private Vector2 width;
    [SerializeField]
    private Vector2 height;

    public GameObject Prefab => this.prefab;
    public Vector2 Width => width;
    public Vector2 Height => height;
}
