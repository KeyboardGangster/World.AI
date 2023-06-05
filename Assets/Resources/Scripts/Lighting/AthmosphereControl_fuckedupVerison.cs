using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class AthmosphereControl_fuckedupVersion : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform target;
    [SerializeField] private Camera targetCamera;
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
    //[SerializeField] private LightingValues weatherClear, weatherRain;
    private Fog fog;
    private VolumetricClouds clouds;

    [Header("Lightning Strikes")]
    [SerializeField] private bool isStriking;
    [SerializeField] private float lightingPeriod;
    [SerializeField] private float strikeDistanceMin;
    [SerializeField] private float strikeDistanceMax;
    [SerializeField] private ParticleSystem lightningEffect;
    private float strikeTime;

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

        if (this.isStriking)
        {
            this.UpdateLightning();
        }

        //this.UpdateWeather();
    }

    private void OnValidate()
    {
        if (this.dayDurationSeconds < 1)
            this.dayDurationSeconds = 1;

        this.orbitSpeed = 24 / (this.dayDurationSeconds > 0 ? this.dayDurationSeconds : 1);

        this.SetDayCycle();

        this.fogAndSkyVolume.profile.TryGet(out this.fog);
        this.fogAndSkyVolume.profile.TryGet(out this.clouds);

        /*if (this.isRaining)
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
        }*/
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
    }

    /// <summary>
    /// Sets the time of day to the given parameter and transitions according to the time.
    /// </summary>
    /// <param name="timeOfDay"></param>
    public void SetTimeOfDay(float timeOfDay)
    {
        if (timeOfDay < 0 || timeOfDay > 24)
        {
            Debug.LogError("The time of day must be in range of 0 to 24");
            return;
        }

        this.timeOfDay = timeOfDay;
        this.SetDayCycle();
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

    /// <summary>
    /// Initiates a lightning strike in front of the player after each striking period.
    /// </summary>
    private void UpdateLightning()
    {
        this.strikeTime -= Time.deltaTime;

        if (this.strikeTime > 0)
        {
            return;
        }

        Vector3 strikePosition = new Vector3(
            this.target.position.x + Random.Range(-this.strikeDistanceMax, this.strikeDistanceMax),
            100,
            this.target.position.z + Random.Range(-this.strikeDistanceMax, this.strikeDistanceMax)
        );

        Vector3 optimizedStrikePosition = new Vector3(this.target.position.x, 100, this.target.position.z);
        optimizedStrikePosition += this.targetCamera.transform.forward * Random.Range(this.strikeDistanceMin, this.strikeDistanceMax);

        this.lightningEffect.transform.position = optimizedStrikePosition;
        this.lightningEffect.Play(true);

        this.strikeTime = this.lightingPeriod;
    }
}
