using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MeshGenerator
{
    // Start is called before the first frame update

    bool isSquare;
    // number of points on each side
    public int N;
    public int M;
    // width and length of the mesh
    public float Lx;
    public float Lz;

    public MeshData meshData;

    public Mesh oceanMesh;
    public MeshGenerator(int N, int M, float Lx, float Lz)
    {
        oceanMesh = new Mesh();
        this.N = N;
        this.M = M;
        this.Lx = Lx;
        this.Lz = Lz;
    }

    public void genVertexAndIndexArray()
    {
        // init mesh data
        meshData = new MeshData(true, N, M);

        // steps in the x and z direction
        float dx = Lx / (N - 1);
        float dz = Lz / (M - 1);

        for (int z = 0; z < M; z++)
        {
            for (int x = 0; x < N; x++)
            {
                int i = x + z * N; // the vertex index
                meshData.vertexArray[i] = new Vector3(x * dx, 0, z * dz);
                meshData.uvArray[i] = new Vector2((float)x / N, (float)z / M);

                // generate the triangle index array
                if (z != M - 1 && x != N - 1)
                {
                    meshData.addTriangle(i, i + N, i + N + 1); // counter clockwise, so it be the right side up from positive y 
                    meshData.addTriangle(i + N + 1, i + 1, i);

                }
            }
        }
    }

    public void updateMesh()
    {
        if (meshData.vertexArray.Length > 256) oceanMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        else oceanMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        oceanMesh.Clear();
        oceanMesh.vertices = meshData.vertexArray;
        oceanMesh.triangles = meshData.trianglesArray;
        oceanMesh.uv = meshData.uvArray;
        oceanMesh.RecalculateNormals();
    }


}

public class MeshData
{
    public int N; // length
    public int M; // width
    // mesh data
    public Vector3[] vertexArray;
    public int[] trianglesArray;
    public Vector2[] uvArray;
    public int triangleIndex;
    public bool isSquare;

    public MeshData(bool isSquare, int length, int width)
    {
        // init arrays
        vertexArray = new Vector3[length * width];
        trianglesArray = new int[(length - 1) * (width - 1) * 6];
        uvArray = new Vector2[length * width];
        this.isSquare = isSquare;
        triangleIndex = 0;
    }

    public void addTriangle(int first, int second, int third)
    {
        trianglesArray[triangleIndex] = first;
        trianglesArray[triangleIndex + 1] = second;
        trianglesArray[triangleIndex + 2] = third;
        triangleIndex += 3;
    }

}
