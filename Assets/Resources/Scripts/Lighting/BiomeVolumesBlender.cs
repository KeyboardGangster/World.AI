using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.HighDefinition.CameraSettings;

public class BiomeVolumesBlender : MonoBehaviour
{
    [SerializeField]
    private Transform target;
    [SerializeField]
    private Light sun;

    private WorldGeneratorArgs args;
    private Dictionary<BiomeData, Volume> volumes;
    private BiomeData blendTowards;

    public void Init(WorldGeneratorArgs args)
    {
        this.args = args;
        this.volumes = new Dictionary<BiomeData, Volume>();

        for(int i = 0; i < args.BiomeCount; i++) 
        {
            BiomeData biomeData = args.GetBiome(i);
            //Volume v = Instantiate(biomeData.biome.Volume, this.transform);
            Volume v = this.transform.gameObject.AddComponent<Volume>();
            v.profile = biomeData.biome.Lighting.VolumeProfile;

            if (!this.volumes.ContainsKey(biomeData))
                this.volumes.Add(biomeData, v);
        }

        StartCoroutine(BlendControl());
        StartCoroutine(BlendingCoroutine());
    }

    private IEnumerator BlendControl()
    {
        WaitForSeconds wait = new WaitForSeconds(1f);
        float ratio = args.Terrain.terrainData.heightmapResolution / args.Terrain.terrainData.size.x;

        while(true)
        {
            Vector3Int posInHeightmap = Vector3Int.FloorToInt(this.target.transform.position * ratio);
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

        while(true)
        {
            Vector3Int posInHeightmap = Vector3Int.FloorToInt(this.target.transform.position * ratio);

            for (int i = 0; i < args.BiomeCount; i++)
            {
                BiomeData biomeData = args.GetBiome(i);
                Volume v = this.volumes[biomeData];

                if (biomeData == this.blendTowards)
                    v.weight = Mathf.Min(v.weight + 0.002f, 1);
                else
                    v.weight = Mathf.Max(v.weight - 0.002f, 0);

                sun.colorTemperature = Mathf.Lerp(sun.colorTemperature, biomeData.biome.Lighting.LightTemperature, v.weight);
                sun.color = Color.Lerp(sun.color, biomeData.biome.Lighting.LightFilter, v.weight);
            }

            yield return wait;
        }
    }
}
