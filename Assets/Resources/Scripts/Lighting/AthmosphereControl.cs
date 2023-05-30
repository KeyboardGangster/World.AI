using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class AthmosphereControl : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform volumes;
    [SerializeField] private Volume fogAndSkyVolume;
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    private WorldGeneratorArgs args;
    private Dictionary<VolumeProfile, Volume> volumesDictionary;
    private BiomeData blendTowards;

    [Header("Day Cycle")]
    [Range(0, 24)] [SerializeField] private float timeOfDay = 13f;
    [SerializeField] private bool fixedTimeOfDay = false;
    [SerializeField] private float dayDurationSeconds = 60f;
    private float orbitSpeed;
    private bool isDay;

    [Header("Rain Toggle")]
    [SerializeField] private bool isRaining;
    [SerializeField] private float lerpTime;
    [SerializeField] private ParticleSystem rainEffect;
    [SerializeField] private LightingValues weatherClear, weatherRain;
    private Fog fog;
    private VolumetricClouds clouds;

    private void Awake()
    {
        this.orbitSpeed = 24 / (this.dayDurationSeconds > 0 ? this.dayDurationSeconds : 1);

        this.fogAndSkyVolume.profile.TryGet(out this.fog);
        this.fogAndSkyVolume.profile.TryGet(out this.clouds);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            this.isRaining = !this.isRaining;
        }

        if (!this.fixedTimeOfDay)
        {
            this.UpdateDayCycle();
        }

        this.UpdateWeather();
    }

    private void OnValidate()
    {
        if (this.dayDurationSeconds < 1)
            this.dayDurationSeconds = 1;

        this.orbitSpeed = 24 / (this.dayDurationSeconds > 0 ? this.dayDurationSeconds : 1);

        this.SetDayCycle();

        this.fogAndSkyVolume.profile.TryGet(out this.fog);
        this.fogAndSkyVolume.profile.TryGet(out this.clouds);

        if (this.isRaining)
        {
            this.SetWeather(
                this.weatherRain.sunTemperature,
                this.weatherRain.filterColor,
                this.weatherRain.attenuation,
                this.weatherRain.tintColor,
                this.weatherRain.cloudPreset
            );
        }
        else
        {
            this.SetWeather(
                this.weatherClear.sunTemperature,
                this.weatherClear.filterColor,
                this.weatherClear.attenuation,
                this.weatherClear.tintColor,
                this.weatherClear.cloudPreset
            );
        }
    }

    private void Reset()
    {
        if (this.moon == null)
        {
            var moon = Resources.Load<Light>("WorldAI_DefaultAssets/Prefabs/_DEFAULT_MOON");
            this.moon = Instantiate<Light>(moon, this.transform);
        }

        if (this.sun == null)
        {
            var sun = Resources.Load<Light>("WorldAI_DefaultAssets/Prefabs/_DEFAULT_SUN");
            this.sun = Instantiate<Light>(sun, this.transform);
        }

        if (this.volumes == null)
        {
            Transform volumes = Resources.Load<Transform>("WorldAI_DefaultAssets/Prefabs/_DEFAULT_VOLUME");
            this.volumes = Instantiate<Transform>(volumes, this.transform);
        }

        if (this.fogAndSkyVolume == null)
        {
            Volume fogAndSkyVolume = Resources.Load<Volume>("WorldAI_DefaultAssets/Prefabs/_DEFAULT_SKY_FOG_VOLUME");
            this.fogAndSkyVolume = Instantiate(fogAndSkyVolume, this.transform);
        }
    }

    public void Init(WorldGeneratorArgs args)
    {
        this.args = args;
        this.volumesDictionary = new Dictionary<VolumeProfile, Volume>();

        Volume defaultVolume = this.volumes.GetComponent<Volume>();

        if (defaultVolume != null)
            defaultVolume.enabled = false;

        for (int i = 0; i < args.BiomeCount; i++)
        {
            BiomeData biomeData = args.GetBiome(i);

            if (!this.volumesDictionary.ContainsKey(biomeData.biome.Lighting.VolumeProfile))
            {
                Volume v = this.volumes.gameObject.AddComponent<Volume>();
                v.profile = biomeData.biome.Lighting.VolumeProfile;
                this.volumesDictionary.Add(biomeData.biome.Lighting.VolumeProfile, v);
                v.weight = 0;
            }
        }

        StartCoroutine(BlendControl());
        StartCoroutine(BlendingCoroutine());
    }

    private IEnumerator BlendControl()
    {
        WaitForSeconds wait = new WaitForSeconds(1f);
        float ratio = args.Terrain.terrainData.heightmapResolution / args.Terrain.terrainData.size.x;

        Vector3Int posInHeightmap = Vector3Int.FloorToInt(this.target.transform.position * ratio);
        this.blendTowards = args.GetDominantBiome(posInHeightmap.z, posInHeightmap.x);
        Volume v = this.volumesDictionary[this.blendTowards.biome.Lighting.VolumeProfile];
        v.weight = 1;

        while (true)
        {
            posInHeightmap = Vector3Int.FloorToInt(this.target.transform.position * ratio);
            float dominantWeight = this.args.GetDominantWeight(posInHeightmap.z, posInHeightmap.x);

            if (dominantWeight > 0.7f)
                this.blendTowards = this.args.GetDominantBiome(posInHeightmap.z, posInHeightmap.x);

            yield return wait;
        }
    }

    private IEnumerator BlendingCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.01f);
        float ratio = args.Terrain.terrainData.heightmapResolution / args.Terrain.terrainData.size.x;

        while (true)
        {
            foreach (KeyValuePair<VolumeProfile, Volume> kvp in this.volumesDictionary)
            {
                if (this.blendTowards.biome.Lighting.VolumeProfile == kvp.Key)
                    kvp.Value.weight = Mathf.Min(kvp.Value.weight + 0.002f, 1);
                else
                    kvp.Value.weight = Mathf.Max(kvp.Value.weight - 0.002f, 0);
            }

            yield return wait;
        }
    }

    /// <summary>
    /// Increases the daytime by the orbitspeed, resulting in an animation the daycicle
    /// </summary>
    private void UpdateDayCycle()
    {
        this.timeOfDay += this.orbitSpeed * Time.deltaTime;

        if (this.timeOfDay > 24)
        {
            this.timeOfDay = 0 + this.timeOfDay % 24;
        }

        this.SetDayCycle();
    }

    /// <summary>
    /// Sets the rotation of the sun to the corresponding daytime.
    /// </summary>
    private void SetDayCycle()
    {
        float normal = this.timeOfDay / 24;
        float sunRotation = Mathf.Lerp(-90, 270, normal);
        float moonRotation = sunRotation - 180;

        this.sun.transform.rotation = Quaternion.Euler(sunRotation, -150, 0);
        this.moon.transform.rotation = Quaternion.Euler(moonRotation, -150, 0);

        this.DayTransition();
    }

    /// <summary>
    /// Manages the transition of moon and sun parameters
    /// </summary>
    private void DayTransition()
    {
        if (this.isDay)
        {
            if (this.sun.transform.rotation.eulerAngles.x > 180)
            {
                this.isDay = false;
                this.sun.shadows = LightShadows.None;
                this.moon.shadows = LightShadows.Soft;
            }
            return;
        }

        if (this.moon.transform.rotation.eulerAngles.x > 180)
        {
            this.isDay = true;
            this.sun.shadows = LightShadows.Soft;
            this.moon.shadows = LightShadows.None;
        }
    }

    /// <summary>
    /// Transitions between the two weather statea (raining and clear)
    /// </summary>
    private void UpdateWeather()
    {
        if (this.isRaining)
        {
            this.SetWeather(
                Mathf.Lerp(this.sun.colorTemperature, this.weatherRain.sunTemperature, this.lerpTime * Time.deltaTime),
                Color.Lerp(this.sun.color, this.weatherRain.filterColor, this.lerpTime * Time.deltaTime),
                Mathf.Lerp(this.fog.meanFreePath.value, this.weatherRain.attenuation, this.lerpTime * Time.deltaTime),
                Color.Lerp(this.sun.color, this.weatherRain.tintColor, this.lerpTime * Time.deltaTime),
                this.weatherRain.cloudPreset
            );

            // Rain
            if (!this.rainEffect.isPlaying)
            {
                this.rainEffect.Play(true);
            }
            return;
        }

        this.SetWeather(
            Mathf.Lerp(this.sun.colorTemperature, this.weatherClear.sunTemperature, this.lerpTime * Time.deltaTime),
            Color.Lerp(this.sun.color, this.weatherClear.filterColor, this.lerpTime * Time.deltaTime),
            Mathf.Lerp(this.fog.meanFreePath.value, this.weatherClear.attenuation, this.lerpTime * Time.deltaTime),
            Color.Lerp(this.sun.color, this.weatherClear.tintColor, this.lerpTime * Time.deltaTime),
            this.weatherClear.cloudPreset
        );

        // Rain
        if (this.rainEffect.isPlaying)
        {
            this.rainEffect.Stop(true);
        }
    }

    /// <summary>
    /// Sets the values of the weather (sun, fog and clouds) to the given parameters
    /// </summary>
    /// <param name="colorTemperature"></param>
    /// <param name="filterColor"></param>
    /// <param name="attenuation"></param>
    /// <param name="tintColor"></param>
    /// <param name="cloudPreset"></param>
    private void SetWeather(float colorTemperature, Color filterColor, float attenuation, Color tintColor, VolumetricClouds.CloudPresets cloudPreset)
    {
        // Sun
        this.sun.colorTemperature = colorTemperature;
        this.sun.color = filterColor;

        // Fog
        this.fog.meanFreePath.value = attenuation;
        this.fog.tint.value = tintColor;

        // Clouds
        this.clouds.cloudPreset.value = cloudPreset;
    }
}
