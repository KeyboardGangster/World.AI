using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SOHeight : ScriptableObject
{
    [SerializeField]
    [TextArea(3, 10)]
    [Tooltip("Describe the distribution of biomes by grouping them with letters with A being height 0 and ascending. 150 words max!")]
    private string description;

    [SerializeField]
    protected float heightAddend = 0;
    [SerializeField]
    protected float heightMultiplier = 1;
    [SerializeField]
    protected float scale = 1;

    public float HeightAddend => this.heightAddend;
    public float HeightMultiplier => this.heightMultiplier;
    public float Scale => this.scale;

    public abstract HeightData GetHeightData();

    private void OnValidate()
    {
        if (this.description.Length > 150)
        {
            this.description = this.description.Remove(149);
            Debug.LogWarning("Your description is too long, please shorten it to max 150 words.");
        }
    }
}
