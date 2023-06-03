using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class HeightData
{
    /// <summary>
    /// Incase this is marked, GetHeight is expected to return values in the range of 0 to 1.
    /// </summary>
    [SerializeField]
    public bool isBiomeDistribution = false;
    [SerializeField]
    protected SOHeight reference;
    [SerializeField]
    protected float scale = 1;
    [SerializeField]
    protected float multiplier = 1;
    [SerializeField]
    protected float addend = 0;

    public HeightData(SOHeight so) 
    { 
        this.reference = so; 
    }

    public virtual void Prepare(WorldGeneratorArgs args, int x, int y)
    {
        if (this.isBiomeDistribution)
        {
            this.scale *= this.reference.Scale;
            this.multiplier = 1;
            this.addend = 0;
            this.scale *= args.BiomeScale;
            this.scale /= args.WorldScaleRatio * args.ToyScaleRatio;
            this.addend /= args.ToyScaleRatio;
        }
        else
        {
            this.scale *= this.reference.Scale;
            this.multiplier *= this.reference.HeightMultiplier;
            this.addend += this.reference.HeightAddend;
            this.scale /= args.WorldScaleRatio * args.ToyScaleRatio;
            this.addend /= args.ToyScaleRatio;
            this.addend += args.WaterLevel;
            this.multiplier /= args.ToyScaleRatio;
        }
    }

    public abstract float GetHeight(WorldGeneratorArgs args, int x, int y);
}
