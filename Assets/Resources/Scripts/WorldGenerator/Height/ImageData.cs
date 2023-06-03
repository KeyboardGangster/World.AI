using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageData : HeightData
{
    [SerializeField]
    private SOImage image;
    [SerializeField]
    private Vector2 imageToHeightmapRatio;

    public ImageData(SOHeight so) : base(so)
    {
        this.image = (SOImage) so;
    }

    public override void Prepare(WorldGeneratorArgs args, int x, int y)
    {
        this.imageToHeightmapRatio = new Vector2(
            (float)this.image.Texture.width / args.TerrainData.heightmapResolution,
            (float)this.image.Texture.height / args.TerrainData.heightmapResolution
        );

        base.Prepare(args, x, y);
    }

    public override float GetHeight(WorldGeneratorArgs args, int x, int y)
    {
        float xInImage = x * this.imageToHeightmapRatio.x * this.reference.Scale;
        float yInImage = y * this.imageToHeightmapRatio.y * this.reference.Scale;

        if (this.image.InvertXAxis)
            xInImage = this.image.Texture.width - xInImage - this.imageToHeightmapRatio.x;
        if (this.image.InvertYAxis)
            yInImage = this.image.Texture.height - yInImage - this.imageToHeightmapRatio.y;

        float height = image.Texture.GetPixel(Mathf.FloorToInt(xInImage), Mathf.FloorToInt(yInImage)).grayscale;

        if (this.image.InvertHeight)
            height = 1 - height;

        return height * this.multiplier + this.addend;
    }
}
