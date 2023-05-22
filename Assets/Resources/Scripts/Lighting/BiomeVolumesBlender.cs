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
    private Dictionary<VolumeProfile, Volume> volumes;
    private BiomeData blendTowards;

    public void Init(WorldGeneratorArgs args)
    {
        this.args = args;
        this.volumes = new Dictionary<VolumeProfile, Volume>();

        for(int i = 0; i < args.BiomeCount; i++) 
        {
            BiomeData biomeData = args.GetBiome(i);

            if (!this.volumes.ContainsKey(biomeData.biome.Lighting.VolumeProfile))
            {
                Volume v = this.transform.gameObject.AddComponent<Volume>();
                v.profile = biomeData.biome.Lighting.VolumeProfile;
                this.volumes.Add(biomeData.biome.Lighting.VolumeProfile, v);
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
        Volume v = this.volumes[this.blendTowards.biome.Lighting.VolumeProfile];
        v.weight = 1;
        
        while(true)
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

        while(true)
        {
            foreach(KeyValuePair<VolumeProfile, Volume> kvp in this.volumes)
            {
                if (this.blendTowards.biome.Lighting.VolumeProfile == kvp.Key)
                    kvp.Value.weight = Mathf.Min(kvp.Value.weight + 0.002f, 1);
                else
                    kvp.Value.weight = Mathf.Max(kvp.Value.weight - 0.002f, 0);
            }

            Volume current = this.volumes[this.blendTowards.biome.Lighting.VolumeProfile];
            sun.colorTemperature = Mathf.Lerp(sun.colorTemperature, this.blendTowards.biome.Lighting.LightTemperature, current.weight);
            sun.color = Color.Lerp(sun.color, this.blendTowards.biome.Lighting.LightFilter, current.weight);

            yield return wait;
        }
    }
}
