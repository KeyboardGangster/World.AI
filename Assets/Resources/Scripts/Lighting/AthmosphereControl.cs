using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.Rendering.HighDefinition.VolumetricClouds;

[RequireComponent(typeof(WorldGenerator))]
public class AthmosphereControl : MonoBehaviour
{
    private WorldGenerator worldGenerator;

    [Header("Components")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform volumes;
    //CloudOverride is only here to force the blend-effect between clouds.
    //Once Unity fixes blending between cloudPresets when using multiple
    //VolumeProfile-weights, this cloudOverride can be deleted.
    [SerializeField] private Volume cloudOverride;
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    private Dictionary<VolumeProfile, Volume> volumesDictionary;
    private BiomeData blendTowards;

    [Header("Day Cycle")]
    [Range(0, 24)] [SerializeField] private float timeOfDay = 13f;
    [SerializeField] private bool fixedTimeOfDay = true;
    [SerializeField] private float dayDurationSeconds = 1200f;
    private float orbitSpeed;

    [Header("Rain Toggle")]
    [SerializeField] private bool isRaining;
    [SerializeField] private ParticleSystem rainEffect;
    private Vector3 rainEffectOffset;

    [Header("Lightning Strikes")]
    [SerializeField] private bool isStriking;
    [SerializeField] private float lightningPeriod;
    //[SerializeField] private float strikeDistanceMin;
    //[SerializeField] private float strikeDistanceMax;
    [SerializeField] private ParticleSystem lightningEffect;
    //private Camera mainCam;

    [SerializeField]
    private float windSpeed = 0.25f;
    [SerializeField]
    private float windStrength = 0.3f;
    [SerializeField]
    private float windRotation = 20;

    private void Awake()
    {
        //this.mainCam = Camera.main;
        this.worldGenerator = this.GetComponent<WorldGenerator>();
        
        this.orbitSpeed = 24 / (this.dayDurationSeconds > 0 ? this.dayDurationSeconds : 1);
        this.rainEffectOffset = this.rainEffect.transform.localPosition;
    }

    private void Start()
    {
        if (this.worldGenerator.Args == null)
        {
            Debug.LogError("No WorldGeneratorArgs found in WorldGenerator. Either your world is not generated or you're missing a reference.");
        }

        this.volumesDictionary = new Dictionary<VolumeProfile, Volume>();

        Volume defaultVolume = this.volumes.GetComponent<Volume>();

        if (defaultVolume != null)
            defaultVolume.enabled = false;

        for (int i = 0; i < this.worldGenerator.Args.BiomeCount; i++)
        {
            BiomeData biomeData = this.worldGenerator.Args.GetBiome(i);

            if (!this.volumesDictionary.ContainsKey(biomeData.biome.Lighting.VolumeProfile))
            {
                Volume v = this.volumes.gameObject.AddComponent<Volume>();
                v.profile = biomeData.biome.Lighting.VolumeProfile;
                v.priority = 0;
                v.weight = 0;
                this.volumesDictionary.Add(biomeData.biome.Lighting.VolumeProfile, v);

                if (biomeData.biome.Lighting.VolumeProfileRain != null && !this.volumesDictionary.ContainsKey(biomeData.biome.Lighting.VolumeProfileRain))
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
        StartCoroutine(UpdateLightning());

        Shader.SetGlobalFloat("_WindSpeed", this.windSpeed);
        Shader.SetGlobalFloat("_WindStrength", this.windStrength);
        Shader.SetGlobalVector("_WindDirection", Quaternion.Euler(0, this.windRotation, 0) * Vector3.forward);
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
        /*if (this.mainCam == null)
            this.mainCam = Camera.main;*/


        this.orbitSpeed = 24 / (this.dayDurationSeconds > 0 ? this.dayDurationSeconds : 1);

        Shader.SetGlobalFloat("_WindSpeed", this.windSpeed);
        Shader.SetGlobalFloat("_WindStrength", this.windStrength);
        Shader.SetGlobalVector("_WindDirection", Quaternion.Euler(0, this.windRotation, 0) * Vector3.forward);

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

        if (this.lightningEffect == null)
        {
            ParticleSystem lightningEffect = Resources.Load<ParticleSystem>("WorldAI_DefaultAssets/Prefabs/_DEFAULT_LIGHTNING_EFFECT");
            this.lightningEffect = Instantiate(lightningEffect, this.transform);
        }
    }

    private IEnumerator BlendControl()
    {
        WaitForSeconds wait = new WaitForSeconds(1f);
        float ratio = this.worldGenerator.Args.TerrainData.heightmapResolution / this.worldGenerator.Args.TerrainData.size.x;

        Vector3Int posInHeightmap = Vector3Int.FloorToInt(this.target.transform.position * ratio);
        this.blendTowards = this.worldGenerator.Args.GetDominantBiome(posInHeightmap.z, posInHeightmap.x);
        Volume v = this.volumesDictionary[this.blendTowards.biome.Lighting.VolumeProfile];
        v.weight = 1;

        while (true)
        {
            posInHeightmap = Vector3Int.FloorToInt(this.target.transform.position * ratio);
            //float dominantWeight = this.worldGenerator.Args.GetDominantWeight(posInHeightmap.z, posInHeightmap.x);
            //if (dominantWeight > 0.7f)
            this.blendTowards = this.worldGenerator.Args.GetDominantBiome(posInHeightmap.z, posInHeightmap.x);

            yield return wait;
        }
    }

    private IEnumerator BlendingCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.01f);
        float ratio = this.worldGenerator.Args.TerrainData.heightmapResolution / this.worldGenerator.Args.TerrainData.size.x;

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
                    kvp.Value.weight = Mathf.Max(kvp.Value.weight - 0.002f, 0.002f);
            }

            Volume biomeVolumeRain = this.GetVolumeRain(this.blendTowards);
            Volume biomeVolume = this.GetVolume(this.blendTowards);

            //It should rain
            if (this.isRaining && biomeVolumeRain != null)
            {
                this.sun.colorTemperature = Mathf.Lerp(this.sun.colorTemperature, this.blendTowards.biome.Lighting.LightTemperatureRain, biomeVolumeRain.weight);
                this.sun.color = Color.Lerp(this.sun.color, this.blendTowards.biome.Lighting.LightFilterRain, biomeVolumeRain.weight);

                if (biomeVolumeRain.weight > 0.6f && !this.rainEffect.isPlaying)
                {
                    this.rainEffect.Play(true);
                }

                if (biomeVolumeRain.profile.TryGet(out VolumetricClouds biomeCloudsRain) && currentClouds.cloudPreset.value != biomeCloudsRain.cloudPreset.value)
                    currentClouds.cloudPreset.value = biomeCloudsRain.cloudPreset.value;
            }
            //It should not rain
            else
            {
                this.sun.colorTemperature = Mathf.Lerp(this.sun.colorTemperature, this.blendTowards.biome.Lighting.LightTemperature, biomeVolume.weight);
                this.sun.color = Color.Lerp(this.sun.color, this.blendTowards.biome.Lighting.LightFilter, biomeVolume.weight);

                if (this.rainEffect.isPlaying)
                {
                    this.rainEffect.Stop(true);
                }

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
        this.timeOfDay = ((this.timeOfDay % 24) + 24) % 24;
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
    /// Manages the transition of shadow-casting from moon and sun.
    /// </summary>
    private void DayTransition()
    {
        if (this.timeOfDay > 6 && this.timeOfDay <= 18)
        {
            if (this.moon.shadows != LightShadows.None)
            {
                this.moon.shadows = LightShadows.None;
                this.sun.shadows = LightShadows.Soft;
            }
        }
        else
        {
            if (this.sun.shadows != LightShadows.None)
            {
                this.sun.shadows = LightShadows.None;
                this.moon.shadows = LightShadows.Soft;

                //Quickfix to avoid lighting-bug
                if ((Application.isPlaying && this.fixedTimeOfDay) || !Application.isPlaying)
                    this.Invoke("LightingBugQuickfix", 0.2f); //Forgive me for I have sinned.
            }
        }
    }

    /// <summary>
    /// Moving Lightsources forces lighting-recalculation which conveniently fixes a bug when switching shadowcaster from sun to moon. It's not pretty, but it's better then a buggy lit scene.
    /// </summary>
    private void LightingBugQuickfix()
    {
        float timeOfDay;

        if (this.timeOfDay + 0.1f <= 6)
            timeOfDay = this.timeOfDay + 0.05f;
        else
            timeOfDay = this.timeOfDay - 0.05f;

        float normal = timeOfDay / 24;
        float sunRotation = Mathf.Lerp(-90, 270, normal);
        float moonRotation = sunRotation - 180;

        this.sun.transform.rotation = Quaternion.Euler(sunRotation, -150, 0);
        this.moon.transform.rotation = Quaternion.Euler(moonRotation, -150, 0);
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

    public void SetTimeOfDay(float timeInHours)
    {
        if (timeInHours == 6) //Since there's a bug when setting it from this time to something darker, time cannot be set to these values for now. I blame Unity.
            timeInHours = 6.1f;

        this.orbitSpeed = 24 / (this.dayDurationSeconds > 0 ? this.dayDurationSeconds : 1);
        this.timeOfDay = ((timeInHours % 24) + 24) % timeInHours;
        this.SetDayCycle();
    }

    private IEnumerator UpdateLightning()
    {
        WorldGeneratorArgs args = this.worldGenerator.Args;
        Vector2 min = new Vector2(-args.TerrainData.size.x + 1, -args.TerrainData.size.z + 1);
        Vector2 max = new Vector2(args.TerrainData.size.x - 1, args.TerrainData.size.z - 1) * 2;

        while (true)
        {
            if (!this.isStriking)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            /*Vector3 strikePosition = new Vector3(
                this.target.position.x + Random.Range(-this.strikeDistanceMax, this.strikeDistanceMax),
                100,
                this.target.position.z + Random.Range(-this.strikeDistanceMax, this.strikeDistanceMax)
            );

            Vector3 optimizedStrikePosition = new Vector3(this.target.position.x, 100, this.target.position.z);
            optimizedStrikePosition += this.mainCam.transform.forward * Random.Range(this.strikeDistanceMin, this.strikeDistanceMax);

            this.lightningEffect.transform.position = optimizedStrikePosition;
            this.lightningEffect.Play(true);*/

            Vector3 strikePos = new Vector3(Random.Range(min.x, max.x), 100, Random.Range(min.y, max.y));

            //If is within terrain-range, get terrain-height at pos.
            if (strikePos.x >= 0 && strikePos.x < args.TerrainData.size.x && strikePos.z >= 0 && strikePos.z < args.TerrainData.size.z)
                strikePos.y += Mathf.Max(args.TerrainData.GetHeight(Mathf.FloorToInt(strikePos.x), Mathf.FloorToInt(strikePos.z)), 0); //Mathf.Max to keep it above water-lvl.
            //otherwise assign random height
            else
                strikePos.y += Random.Range(0, 50f);


            this.lightningEffect.transform.position = strikePos;
            this.lightningEffect.Play(true);

            yield return new WaitForSeconds(this.lightningPeriod);
        }
    }
}
