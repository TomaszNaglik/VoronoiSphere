
using System;
using System.Collections.Generic;
//using System.Numerics;
using UnityEngine;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoronoiMesh : MonoBehaviour
{
    static int MeshIndex = 0;
    public int Index;
    Mesh mesh;
    List<Vector3> vertices;
    List<int> indices;
    List<Color> colors;
    List<SVoronoiCell> cells;

    MeshCollider meshCollider;
    SVoronoiMap map;

    public float columnOffset;
    public int column;

    public List<SVoronoiCell> Cells { get => cells; set => cells = value; }
    public SVoronoiMap Map { get => map; set => map = value; }

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        Index = MeshIndex++;
        
        vertices = new List<Vector3>();
        indices = new List<int>();
        colors = new List<Color>();
        Cells = new List<SVoronoiCell>();
        
    }

    private void Update()
    {
       
        
        
    }

    public void UpdateColors()
    {
        colors.Clear();
        foreach (SVoronoiCell cell in cells)
        {
            
            UpdateColor(cell);
            

        }
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        meshCollider.sharedMesh = mesh;
    }

    private void UpdateColor(SVoronoiCell cell)
    {
        foreach (SVoronoiEdge e in cell.Edges)
        {
            e.UpdateColors();

            
            AddTriangleColor(e.ParentColor, e.ParentColor, e.ParentColor);

            AddTriangleColor(e.LeftColor, e.OppositeColor, e.ParentColor);
            
            AddTriangleColor(e.OppositeColor, e.OppositeColor, e.ParentColor);
            
            AddTriangleColor(e.OppositeColor, e.OppositeColor, e.ParentColor);
            
            AddTriangleColor(e.OppositeColor, e.RightColor, e.ParentColor);

            
            AddTriangleColor(e.ParentColor, e.OppositeColor, e.ParentColor);
            
            AddTriangleColor(e.ParentColor, e.OppositeColor, e.ParentColor);
           
            AddTriangleColor(e.ParentColor, e.OppositeColor, e.ParentColor);
        }
    }
    public void Triangulate()
    {
        vertices.Clear();
        indices.Clear();
        colors.Clear();
        mesh.Clear();
        foreach (SVoronoiCell cell in cells)
        {
            
                TriangulateCell(cell);
            
                
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        
        meshCollider.sharedMesh = mesh;
        
    }

    

    private void TriangulateCell(SVoronoiCell cell)
    {
        //  v3--b1---b2---b3--v4
        //   \  /\   /\   /\  /
        //    \/  \ /  \ /  \/
        //     v1--i1--i2--v2
        //      \__________/
        //       \  /\    /
        //        \/__\ _/
        //         \    /
        //        Center
        //

        //  t0----t1-----t2-----t3----t4
        //   \    /\     /\     /\    /
        //    \  /  \   /  \   /  \  /
        //     l1-----01-----i2----r1
        //      \     /\     /\    /
        //       \   /  \   /  \  /  
        //        l2-----i3-----r2
        //         \     /\     /
        //          \   /  \   /
        //           l3------r3
        //            \      /
        //             \    /
        //               c

        // 3 levels of LOD
        // unless coastal tile
        //
        //R0------R1-----R2-----R3-----R4
        // \   \   |  \  |   /  |  /   /
        //  t0----t1-----t2-----t3----t4
        //   \    /\     /\     /\    /
        //    \  /  \   /  \   /  \  /
        //     l1-----01-----i2----r1
        //      \     /\     /\    /
        //       \   /  \   /  \  /  
        //        l2-----i3-----r2
        //         \     /\     /
        //          \   /  \   /
        //           l3------r3
        //            \      /
        //             \    /
        //               c



        foreach (SVoronoiEdge e in cell.Edges)
        {
            e.UpdateColors();
            e.CalculateTriangles();
            AddTriangles(e.LOD3_Vertices);
            /*Vector3 center = e.Parent.Position;
            Vector3 v3 = e.Points[0];
            Vector3 v4 = e.Points[1];
            Vector3 v1 = VParams.NewVertex(center, v3, VParams.solidFactor);
            Vector3 v2 = VParams.NewVertex(center, v4, VParams.solidFactor);

            Vector3 bridge = (v3-center + v4-center) * 0.5f * VParams.blendFactor;
            Vector3 b1 = v1 + bridge;
            Vector3 b2 = VParams.NewVertex(v3, v4, 0.50f);
            Vector3 b3 = v2 + bridge;
            Vector3 i1 = VParams.NewVertex(v1, v2, 0.3333f);
            Vector3 i2 = VParams.NewVertex(v1, v2, 0.6666f);

            Vector3 Ncenter = N(center);
            Vector3 Nv1 = N(v1);
            Vector3 Nv2 = N(v2);
            Vector3 Nv3 = N(v3);
            Vector3 Nv4 = N(v4);
            Vector3 Nb1 = N(b1);
            Vector3 Nb2 = N(b2);
            Vector3 Nb3 = N(b3);
            Vector3 Ni1 = N(i1);
            Vector3 Ni2 = N(i2);

            //AddTriangle(center, v3, v4);
            //AddTriangleColor(e.ParentColor, e.ParentColor, e.ParentColor);
            AddTriangle(Ncenter, Nv1, Ni1);
            AddTriangleColor(e.ParentColor, e.ParentColor, e.ParentColor);
            AddTriangle(Ncenter, Ni1, Ni2);
            AddTriangleColor(e.ParentColor, e.ParentColor, e.ParentColor);
            AddTriangle(Ncenter, Ni2, Nv2);
            AddTriangleColor(e.ParentColor, e.ParentColor, e.ParentColor);

            AddTriangle(Nv3, Nb1, Nv1);
            AddTriangleColor(e.LeftColor, e.OppositeColor, e.ParentColor);
            AddTriangle(Nb1, Nb2, Ni1);
            AddTriangleColor(e.OppositeColor, e.OppositeColor, e.ParentColor);
            AddTriangle(Nb2, Nb3, Ni2);
            AddTriangleColor(e.OppositeColor, e.OppositeColor, e.ParentColor);
            AddTriangle(Nb3, Nv4, Nv2);
            AddTriangleColor(e.OppositeColor, e.RightColor, e.ParentColor);

            AddTriangle(Nv1, Nb1, Ni1);
            AddTriangleColor(e.ParentColor, e.OppositeColor, e.ParentColor);
            AddTriangle(Ni1, Nb2, Ni2);
            AddTriangleColor(e.ParentColor, e.OppositeColor, e.ParentColor);
            AddTriangle(Ni2, Nb3, Nv2);
            AddTriangleColor(e.ParentColor, e.OppositeColor, e.ParentColor);
            
            //AddQuad(v3, v4, v2, v1);
            //AddQuad(v1, v2, v3, v4);
            //AddQuadColor(e.MainColor, e.MainColor, e.LeftColor, e.RightColor);*/


        }
    }

    private void AddTriangles(List<Vector3> _vertices)
    {
        int VertexIndex = vertices.Count;
        vertices.AddRange(_vertices);
        for(int i = 0; i< _vertices.Count; i++)
        {
            indices.Add(VertexIndex + i);
        }
    }

    private void AddTriangleColor(Color main, Color left, Color right)
    {
        colors.Add(main);
        colors.Add(left);
        colors.Add(right);

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

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);
        indices.Add(vertexIndex);
        indices.Add(vertexIndex + 2);
        indices.Add(vertexIndex + 1);
        indices.Add(vertexIndex + 1);
        indices.Add(vertexIndex + 2);
        indices.Add(vertexIndex + 3);
    }

    private void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }




}
