using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SkyController : MonoBehaviour
{
    [TextArea(3, 4)]
    public string info;

    [Header("Volumes")]
    [SerializeField] private Volume defaultVolume;
    [SerializeField] private Volume otherVolume;
    private Volume currentVolume;

    [Space]
    [SerializeField] private float fadeDuration;

    [Header("Day / Night Cycle")]
    [SerializeField] private Light directionalLight;
    private float sunSpeed;

    [Space]
    [SerializeField] private float dayDuration;
    [SerializeField] private bool rotateAroundX;
    [SerializeField] private bool rotateAroundY;
    [SerializeField] private bool rotateAroundZ;

    private void Awake()
    {
        this.sunSpeed = 360 / this.dayDuration;
        this.currentVolume = this.defaultVolume;
    }

    private void Update()
    {
        this.DayNightCycle();

        if (Input.GetKeyDown(KeyCode.K))
        {
            this.SwitchVolume(SkyBiomes.Default);
            print("DEFAULT");
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            this.SwitchVolume(SkyBiomes.Other);
            print("OTHER");
        }
    }

    /// <summary>
    /// Rotates the directional light to simulate day and night cycle.
    /// </summary>
    private void DayNightCycle()
    {
        if (this.rotateAroundX)
        { 
            this.directionalLight.transform.Rotate(Vector3.right, this.sunSpeed * Time.deltaTime); 
        }

        if (this.rotateAroundY)
        {
            this.directionalLight.transform.Rotate(Vector3.up, this.sunSpeed * Time.deltaTime);
        }

        if (this.rotateAroundZ)
        {
            this.directionalLight.transform.Rotate(Vector3.forward, this.sunSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Switches the volume based on a given biome by fading between their weight.
    /// </summary>
    /// <param name="newBiome"></param>
    public void SwitchVolume(SkyBiomes newBiome)
    {
        switch (newBiome)
        {
            case SkyBiomes.Default:
                if (this.currentVolume == this.defaultVolume) { return; }
                this.StartCoroutine(this.FadeVolumes(this.currentVolume, this.defaultVolume, this.fadeDuration));
                this.currentVolume = this.defaultVolume;
                break;
            case SkyBiomes.Other:
                if (this.currentVolume == this.otherVolume) { return; }
                this.StartCoroutine(this.FadeVolumes(this.currentVolume, this.otherVolume, this.fadeDuration));
                this.currentVolume = this.otherVolume;
                break;
        }
    }

    /// <summary>
    /// Fades the weight of two volumes.
    /// </summary>
    /// <param name="volumeOff"></param>
    /// <param name="volumeOn"></param>
    /// <returns></returns>
    private IEnumerator FadeVolumes(Volume volumeOff, Volume volumeOn, float fadeDuration)
    {
        float fadeSpeed = 1 / fadeDuration;

        for (float weight = 1; weight > 0; weight -= fadeSpeed)
        {
            volumeOff.weight = weight;
            volumeOn.weight = 1 - weight;
            yield return new WaitForSeconds(fadeSpeed);
        }
    }
}
