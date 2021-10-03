using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetChunkGPU : MonoBehaviour
{
    private static int GlobalIndex = 0;
    private MeshCollider meshCollider;
    public int index;
    private List<int> indices;
    private List<Vector3> vertices;
    private List<Color> colors;
    public List<Edge> edges;
    private Mesh mesh;

    void Awake()
    {

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        index = GlobalIndex++;
        indices = new List<int>();
        vertices = new List<Vector3>();
        colors = new List<Color>();
        edges = new List<Edge>();
        
    }


    
    void Update()
    {
        
    }

    public void Triangulate()
    {
        vertices.Clear();
        indices.Clear();
        colors.Clear();
        mesh.Clear();
        foreach (Edge e in edges)
        {

            TriangulateEdge(e);


        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();

        meshCollider.sharedMesh = mesh;

    }

    private void TriangulateEdge(Edge e)
    {
        AddTriangle(e.C2,e.A,e.B);
        AddTriangle(e.C1, e.B, e.A);
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int VertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        indices.Add(VertexIndex);
        indices.Add(VertexIndex + 1);
        indices.Add(VertexIndex + 2);
    }

    internal void Reset()
    {
        if(vertices != null)
        {
            vertices.Clear();
            indices.Clear();
            colors.Clear();
            mesh.Clear();
            edges.Clear();
        }
        

    }

    internal void SetMeshData(Vector3[] vertices, int[] indices)
    {
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    internal void PrintData()
    {
        Utils.LogArray("Chunk: " + index + " Vertices: ", mesh.vertices);
        Utils.LogArray("Chunk: " + index + " Indices: ", mesh.triangles);
    }
}
