using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurveData : HeightData
{
    private SOCurve curves;
    private float ratio;

    public CurveData(SOHeight so) : base(so)
    {
        this.curves = (SOCurve)so;
    }

    public override void Prepare(WorldGeneratorArgs args, int x, int y)
    {
        this.ratio = 1f / args.Terrain.terrainData.heightmapResolution;
        base.Prepare(args, x, y);
    }

    public override float GetHeight(WorldGeneratorArgs args, int x, int y)
    {
        float xInCurve = x * ratio * this.reference.Scale;
        float yInCurve = y * ratio * this.reference.Scale;

        if (this.curves.InvertXAxis)
            xInCurve = 1 - xInCurve - ratio;
        if (this.curves.InvertYAxis)
            yInCurve = 1 - yInCurve - ratio;

        float xHeight = curves.XAxis.Evaluate(xInCurve);
        float yHeight = curves.YAxis.Evaluate(yInCurve);

        if (this.curves.InvertXHeight)
            xHeight = 1 - xHeight;
        if (this.curves.InvertYHeight)
            yHeight = 1 - yHeight;

        return (xHeight + yHeight) / 2f * this.multiplier + this.addend;
    }
}
