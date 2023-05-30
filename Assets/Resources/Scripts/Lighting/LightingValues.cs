using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/LightingValues", order = 1)]
public class LightingValues : ScriptableObject
{
    [Header("Sun")]
    public float sunTemperature;
    public Color filterColor;

    [Header("Fog")]
    public float attenuation;
    public Color tintColor;

    [Header("Clouds")]
    public VolumetricClouds.CloudPresets cloudPreset;
}
