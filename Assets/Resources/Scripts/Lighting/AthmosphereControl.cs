using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class AthmosphereControl : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform volumes;
    //CloudOverride is only here to force the blend-effect between clouds.
    //Once Unity fixes blending between cloudPresets when using multiple
    //VolumeProfile-weights, this cloudOverride can be deleted.
    [SerializeField] private Volume cloudOverride;
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
    [SerializeField] private ParticleSystem rainEffect;
    private Vector3 rainEffectOffset;

    private void Awake()
    {
        this.orbitSpeed = 24 / (this.dayDurationSeconds > 0 ? this.dayDurationSeconds : 1);
        this.rainEffectOffset = this.rainEffect.transform.localPosition;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            this.isRaining = !this.isRaining;

        if (!this.fixedTimeOfDay)
            this.UpdateDayCycle();

        this.rainEffect.transform.position = this.target.transform.position + this.rainEffectOffset;
    }

    private void OnValidate()
    {
        if (this.dayDurationSeconds < 1)
            this.dayDurationSeconds = 1;

        this.orbitSpeed = 24 / (this.dayDurationSeconds > 0 ? this.dayDurationSeconds : 1);

        this.SetDayCycle();
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

        if (this.cloudOverride == null)
        {
            Volume cloudOverride = Resources.Load<Volume>("WorldAI_DefaultAssets/Prefabs/_DEFAULT_CLOUD_OVERRIDE");
            this.cloudOverride = Instantiate<Volume>(cloudOverride, this.transform);
        }

        if (this.rainEffect == null)
        {
            ParticleSystem rainEffect = Resources.Load<ParticleSystem>("WorldAI_DefaultAssets/Prefabs/_DEFAULT_RAIN_EFFECT");
            this.rainEffect = Instantiate(rainEffect, this.transform);
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
                v.priority = 0;
                v.weight = 0;
                this.volumesDictionary.Add(biomeData.biome.Lighting.VolumeProfile, v);

                if (biomeData.biome.Lighting.VolumeProfileRain != null)
                {
                    Volume vRain = this.volumes.gameObject.AddComponent<Volume>();
                    vRain.profile = biomeData.biome.Lighting.VolumeProfileRain;
                    vRain.priority = 1;
                    vRain.weight = 0;
                    this.volumesDictionary.Add(biomeData.biome.Lighting.VolumeProfileRain, vRain);
                }
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

        this.cloudOverride.profile.TryGet(out VolumetricClouds currentClouds);

        while (true)
        {
            foreach (KeyValuePair<VolumeProfile, Volume> kvp in this.volumesDictionary)
            {
                if (this.blendTowards.biome.Lighting.VolumeProfile == kvp.Key)
                    kvp.Value.weight = Mathf.Min(kvp.Value.weight + 0.002f, 1);
                else if (this.isRaining && this.blendTowards.biome.Lighting.VolumeProfileRain == kvp.Key)
                    kvp.Value.weight = Mathf.Min(kvp.Value.weight + 0.002f, 1);
                else
                    kvp.Value.weight = Mathf.Max(kvp.Value.weight - 0.002f, 0);
            }

            Volume biomeVolumeRain = this.GetVolumeRain(this.blendTowards);
            Volume biomeVolume = this.GetVolume(this.blendTowards);

            //It should rain
            if (this.isRaining && biomeVolumeRain != null)
            {
                this.sun.colorTemperature = Mathf.Lerp(this.sun.colorTemperature, this.blendTowards.biome.Lighting.LightTemperatureRain, biomeVolumeRain.weight);
                this.sun.color = Color.Lerp(this.sun.color, this.blendTowards.biome.Lighting.LightFilterRain, biomeVolumeRain.weight);

                if (biomeVolumeRain.weight > 0.6f && !this.rainEffect.isPlaying)
                    this.rainEffect.Play(true);

                if (biomeVolumeRain.profile.TryGet(out VolumetricClouds biomeCloudsRain) && currentClouds.cloudPreset.value != biomeCloudsRain.cloudPreset.value)
                    currentClouds.cloudPreset.value = biomeCloudsRain.cloudPreset.value;
            }
            //It should not rain
            else
            {
                this.sun.colorTemperature = Mathf.Lerp(this.sun.colorTemperature, this.blendTowards.biome.Lighting.LightTemperature, biomeVolume.weight);
                this.sun.color = Color.Lerp(this.sun.color, this.blendTowards.biome.Lighting.LightFilter, biomeVolume.weight);

                if (this.rainEffect.isPlaying)
                    this.rainEffect.Stop(true);

                if (biomeVolume.profile.TryGet(out VolumetricClouds biomeClouds) && currentClouds.cloudPreset.value != biomeClouds.cloudPreset.value)
                    currentClouds.cloudPreset.value = biomeClouds.cloudPreset.value;
            }

            this.moon.colorTemperature = Mathf.Min(sun.colorTemperature * 2f, 20000);
            this.moon.color = this.sun.color;

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
    /// Returns Volume for given biomeData.
    /// </summary>
    /// <param name="biomeData">The biomedata.</param>
    /// <returns>Volume of biomedata.</returns>
    private Volume GetVolume(BiomeData biomeData) => this.volumesDictionary[biomeData.biome.Lighting.VolumeProfile];

    /// <summary>
    /// Returns Rain-Volume for given biomeData or null if none was specified.
    /// </summary>
    /// <param name="biomeData">The biomedata.</param>
    /// <returns>Rain-Volume for biomedata or null if none was specified.</returns>
    private Volume GetVolumeRain(BiomeData biomeData) => biomeData.biome.Lighting.VolumeProfileRain != null? this.volumesDictionary[biomeData.biome.Lighting.VolumeProfileRain]: null;
}
