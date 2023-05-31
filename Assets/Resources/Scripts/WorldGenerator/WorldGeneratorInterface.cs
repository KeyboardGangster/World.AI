using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WorldGeneratorInterface : MonoBehaviour
{
    public abstract void GenerateWorld(bool preview = false);
}
