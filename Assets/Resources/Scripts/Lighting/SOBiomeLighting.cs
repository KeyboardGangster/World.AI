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
    private VolumeProfile volumeProfileRain;

    [SerializeField]
    [Range(1500, 20000)]
    [Tooltip("15000 Bluesky | 8000 Shade | 6500 Cloudy | 5500 Direct Sunlight | 4000 Fluorescent Light | 3000 Incandescent Llight | 1900 Candlelight")]
    private float lightTemperature = 6500;
    [SerializeField]
    private Color lightFilter = new Color(1, 0.9568627f, 0.8392157f);

    [SerializeField]
    [Range(1500, 20000)]
    [Tooltip("15000 Bluesky | 8000 Shade | 6500 Cloudy | 5500 Direct Sunlight | 4000 Fluorescent Light | 3000 Incandescent Llight | 1900 Candlelight")]
    private float lightTempreratureRain = 10000;
    [SerializeField]
    private Color lightFilterRain = new Color(0.7686275f, 0.9529412f, 1f);

    public VolumeProfile VolumeProfile => this.volumeProfile;
    public VolumeProfile VolumeProfileRain => this.volumeProfileRain;
    public float LightTemperature => this.lightTemperature;
    public Color LightFilter => this.lightFilter;
    public float LightTemperatureRain => this.lightTempreratureRain;
    public Color LightFilterRain => this.lightFilterRain;
}
