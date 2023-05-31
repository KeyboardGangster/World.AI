using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "WorldAI/Height/From animation curves")]
public class SOCurve : SOHeight
{
    [SerializeField]
    private AnimationCurve xAxis;
    [SerializeField]
    private AnimationCurve yAxis;
    [SerializeField]
    private bool invertXAxis;
    [SerializeField]
    private bool invertYAxis;
    [SerializeField]
    private bool invertXHeight;
    [SerializeField]
    private bool invertYHeight;

    public AnimationCurve XAxis => this.xAxis;
    public AnimationCurve YAxis => this.yAxis;
    public bool InvertXAxis => this.invertXAxis;
    public bool InvertYAxis => this.invertYAxis;
    public bool InvertXHeight => this.invertXHeight;
    public bool InvertYHeight => this.invertYHeight;

    public override HeightData GetHeightData() => new CurveData(this);
}
