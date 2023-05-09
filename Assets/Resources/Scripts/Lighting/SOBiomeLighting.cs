using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "WorldAI/Biome-Lighting")]
public class SOBiomeLighting : ScriptableObject
{
    [SerializeField]
    private VolumeProfile volumeProfile;
    [SerializeField]
    [Range(1500, 20000)]
    [Tooltip("15000 Bluesky | 8000 Shade | 6500 Cloudy | 5500 Direct Sunlight | 4000 Fluorescent Light | 3000 Incandescent Llight | 1900 Candlelight")]
    private float lightTemperature = 6500;
    [SerializeField]
    private Color lightFilter = new Color(1, 0.9568627f, 0.8392157f);

    public VolumeProfile VolumeProfile => this.volumeProfile;
    public float LightTemperature => this.lightTemperature;
    public Color LightFilter => this.lightFilter;
}
