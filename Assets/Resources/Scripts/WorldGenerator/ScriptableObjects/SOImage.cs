using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "WorldAI/Height/From image")]
public class SOImage : SOHeight
{
    [SerializeField]
    private Texture2D texture;

    [SerializeField]
    private bool invertXAxis;
    [SerializeField]
    private bool invertYAxis;
    [SerializeField]
    private bool invertHeight;

    public Texture2D Texture => this.texture;
    public bool InvertXAxis => this.invertXAxis;
    public bool InvertYAxis => this.invertYAxis;
    public bool InvertHeight => this.invertHeight;

    public override HeightData GetHeightData() => new ImageData(this);
}
