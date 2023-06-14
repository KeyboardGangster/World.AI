using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CamTargetPicker : MonoBehaviour
{
    [SerializeField]
    private Transform[] targets;

    [SerializeField]
    private Transform[] targets_morning;
    [SerializeField]
    private Transform[] targets_evening;

    public Transform[] getTargets(WorldGeneratorArgs args, float timeOfDay)
    {
        float ratio = args.TerrainData.heightmapResolution / args.TerrainData.size.x;

        HashSet<Transform> targets = new HashSet<Transform>();

        while(targets.Count < 5)
        {
            Transform target = this.PickTarget(timeOfDay);

            if (!targets.Contains(target))
            {
                //adjust height
                Vector3 newPos = target.position;

                newPos.y = args.GetHeight(target.position.x, target.position.z); //fallback, I think this doesn't work at runtime for some reason. Only with pregenerated worlds.*/

                newPos.y += Random.Range(2f, 5f);
                target.position = newPos;

                //adjust rotation
                float xRotation = 0;

                if (IsMorning(timeOfDay) && this.targets_morning.Contains(target))
                {
                    float delta = Mathf.InverseLerp(6f, 8.5f, timeOfDay);
                    xRotation = Mathf.Lerp(0, -15, delta);
                }
                else if (IsEarlyNight(timeOfDay) && this.targets_morning.Contains(target)) 
                {
                    float delta = Mathf.InverseLerp(18.5f, 20f, timeOfDay);
                    xRotation = Mathf.Lerp(0, -15, delta);
                }
                else if (IsEvening(timeOfDay) && this.targets_evening.Contains(target))
                {
                    float delta = Mathf.InverseLerp(15.5f, 18f, timeOfDay);
                    xRotation = Mathf.Lerp(-20, 0, delta);
                }
                else if (IsLateNight(timeOfDay) && this.targets_evening.Contains(target))
                {
                    float delta = Mathf.InverseLerp(3.5f, 5.4f, timeOfDay);
                    xRotation = Mathf.Lerp(-20, 0, delta);
                }

                Vector3 rotation = target.rotation.eulerAngles;
                rotation.x = xRotation;
                target.rotation = Quaternion.Euler(rotation);

                //check for blocked view
                //Some raycast stuff idk...

                targets.Add(target);
            }
        }

        return targets.ToArray();
    }

    private bool IsMorning(float timeOfDay) => timeOfDay > 6 && timeOfDay <= 8.5f; //6 = -0 x-rotation, 8.5 = -15 x-rotation

    private bool IsEarlyNight(float timeOfDay) => timeOfDay >= 18.5f && timeOfDay < 20; //18.5 = -0 x-rotation, 20 = -15 x-rotation

    private bool IsEvening(float timeOfDay) => timeOfDay > 15.5f && timeOfDay <= 18; //15.5 = -20 x-rotation, 18 = -0 x-rotation

    private bool IsLateNight(float timeOfDay) => timeOfDay >= 3.5f && timeOfDay <= 5.4f; //3.5 = -20 x-rotation, 5.4 = -0 x-rotation

    private Transform PickTarget(float timeOfDay)
    {
        if (Random.Range(0f, 1f) < 0.2f && (this.IsMorning(timeOfDay) || this.IsEarlyNight(timeOfDay)))
        {
            return this.targets_morning[Random.Range(0, this.targets_morning.Length)];
        }
        else if (Random.Range(0f, 1f) < 0.2f && (this.IsEvening(timeOfDay) || this.IsLateNight(timeOfDay)))
        {
            return this.targets_evening[Random.Range(0, this.targets_evening.Length)];
        }
        else
        {
            return this.targets[Random.Range(0, this.targets.Length)];
        }
    }
}
