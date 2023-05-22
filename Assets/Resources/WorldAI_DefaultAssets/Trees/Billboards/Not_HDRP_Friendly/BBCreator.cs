using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BBCreator : MonoBehaviour
{
    //public Texture2D billboardTexture;
    public Material billBoardMaterial;

    // Start is called before the first frame update
    public void CreateBillboard()
    {
        BillboardAsset bb = new BillboardAsset();
        int totalWidth = this.billBoardMaterial.mainTexture.width;
        int totalHeight = this.billBoardMaterial.mainTexture.height;

        float widthNormalized = 1f / 3f;
        float heightNormalized = 1f / 3f;

        bb.material = this.billBoardMaterial;
        bb.width = widthNormalized;
        bb.height = heightNormalized;
        bb.bottom = 0;
        bb.name = "Billboard";
        bb.SetImageTexCoords(new Vector4[]
        {
            new Vector4(0 * widthNormalized, 2 * heightNormalized, widthNormalized, heightNormalized),
            new Vector4(1 * widthNormalized, 2 * heightNormalized, widthNormalized, heightNormalized),
            new Vector4(2 * widthNormalized, 2 * heightNormalized, widthNormalized, heightNormalized),
            new Vector4(0 * widthNormalized, 1 * heightNormalized, widthNormalized, heightNormalized),
            new Vector4(1 * widthNormalized, 1 * heightNormalized, widthNormalized, heightNormalized),
            new Vector4(2 * widthNormalized, 1 * heightNormalized, widthNormalized, heightNormalized),
            new Vector4(0 * widthNormalized, 0 * heightNormalized, widthNormalized, heightNormalized),
            new Vector4(1 * widthNormalized, 0 * heightNormalized, widthNormalized, heightNormalized)
        });
        bb.SetVertices(new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        });
        bb.SetIndices(new ushort[]
        {
            0, 2, 3, 0, 3, 1
        });
        AssetDatabase.CreateAsset(bb, "Assets/Resources/Materials/TerrainTrees/Billboards/bb.asset");
    }
}
