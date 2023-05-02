using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "WorldAI/Rock")]
public class SORock : ScriptableObject
{
    [SerializeField]
    private GameObject prefab;

    [SerializeField]
    private float colliderRadius;

    public GameObject Prefab => this.prefab;

    public float CollisionRadius => colliderRadius;
}
