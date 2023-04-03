using UnityEngine;

/// <summary>
/// Used during generation. Holds the data needed to create a mesh.
/// </summary>
public class MeshData
{
    /// <summary>
    /// The 3D Position of each point on the mesh.
    /// </summary>
    public Vector3[] vertices;
    /// <summary>
    /// The corresponding indices of vertices for the triangle-shape
    /// </summary>
    public int[] triangles;
    /// <summary>
    /// The UV-coordinates for each point on the mesh.
    /// </summary>
    public Vector2[] uvs;
    /// <summary>
    /// The normal vectors for each vertex. Used for lighting calculations.
    /// </summary>
    public Vector3[] normals;

    /// <summary>
    /// The current index when adding indices to triangles.
    /// </summary>
    private int triangleIndex;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="meshWidth">The width of the rectangular mesh.</param>
    /// <param name="meshHeight">The height of the rectangular mesh.</param>
    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        normals = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];

        triangleIndex = 0;
    }

    /// <summary>
    /// Adds the vertices' index to the triangles array.
    /// </summary>
    /// <param name="v1Index">Index of vertex 1.</param>
    /// <param name="v2Index">Index of vertex 2.</param>
    /// <param name="v3Index">Index of vertex 3.</param>
    public void AddTriangle(int v1Index, int v2Index, int v3Index)
    {
        this.triangles[this.triangleIndex++] = v1Index;
        this.triangles[this.triangleIndex++] = v2Index;
        this.triangles[this.triangleIndex++] = v3Index;
    }

    /// <summary>
    /// Creates a mesh Unity can work with.
    /// </summary>
    /// <returns>The mesh, ready to use.</returns>
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = this.vertices;
        mesh.uv = this.uvs;
        mesh.triangles = this.triangles;
        mesh.normals = this.normals;
        //mesh.RecalculateNormals();
        return mesh;
    }
}
