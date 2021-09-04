using DelaunatorSharp;
using SimplexNoise;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SVoronoiEdge
{
    private List<Vector3> points;
    private SVoronoiCell parent;
    private SVoronoiCell opposite;
    private SVoronoiCell left;
    private SVoronoiCell right;
    private Color leftColor;
    private Color rightColor;
    private Color parentColor;
    private Color oppositeColor;

    public List<Vector3> Points { get => points; set => points = value; }
    public SVoronoiCell Parent { get => parent; set => parent = value; }
    public SVoronoiCell Opposite { get => opposite; set => opposite = value; }
    public SVoronoiCell Left { get => left; set => left = value; }
    public SVoronoiCell Right { get => right; set => right = value; }
    public Color LeftColor { get => leftColor; set => leftColor = value; }
    public Color RightColor { get => rightColor; set => rightColor = value; }
    public Color ParentColor { get => parentColor; set => parentColor = value; }
    public Color OppositeColor { get => oppositeColor; set => oppositeColor = value; }

    public SVoronoiEdge(Vector3 P, Vector3 Q)
    {
        Points = new List<Vector3>();
        Points.Add(P);
        Points.Add(Q);

       

    }
    public void UpdateColors()
    {
        this.ParentColor = parent.Color;
        this.LeftColor = (this.ParentColor + this.Left.Color + this.Opposite.Color) / 3f;
        this.RightColor = (this.ParentColor + this.Right.Color + this.Opposite.Color) / 3f;
        this.OppositeColor = (this.ParentColor + this.Opposite.Color) / 2f;
    }


    public void SetEdgeNeighbours()
    {
        //find opposite
        bool OppositeIsFound = false;
        int OppositeIndex = -1;
        for (int i = 0; i < parent.Neighbours.Count; i++)
        {
            SVoronoiCell cell = parent.Neighbours[i];
            foreach (SVoronoiEdge edge in cell.Edges)
            {
                List<Vector3> nPoints = edge.Points;
                Vector3 n1 = nPoints[0];
                Vector3 n2 = nPoints[nPoints.Count - 1];
                Vector3 p1 = points[0];
                Vector3 p2 = points[points.Count - 1];


                if ((n1 == p1 || n2 == p1) && (n1 == p2 || n2 == p2))
                {
                    this.opposite = cell;
                    OppositeIsFound = true;
                    OppositeIndex = i;
                    break;
                }
            }
            if (OppositeIsFound) break;

        }
        this.Left = parent.FindPreviousNeighbour(OppositeIndex);
        this.Right = parent.FindNextNeighbour(OppositeIndex);
        //UpdateColors();


    }

    


    Vector3 c;
    Vector3 l1;
    Vector3 l2;
    Vector3 l3;
    Vector3 r1;
    Vector3 r2;
    Vector3 r3;
    Vector3 t0;
    Vector3 t1;
    Vector3 t2;
    Vector3 t3;
    Vector3 t4;
    Vector3 i1;
    Vector3 i2;
    Vector3 i3;
    Vector3 R0;
    Vector3 R1;
    Vector3 R2;
    Vector3 R3;
    Vector3 R4;

    public List<Vector3> LOD1_Vertices = new List<Vector3>();
    public List<Vector3> LOD2_Vertices = new List<Vector3>();
    public List<Vector3> LOD3_Vertices = new List<Vector3>();

    

    public void CalculateTriangles()
    {
        c = N(parent.Position);
        t0 = N(Points[0]);
        t4 = N(Points[1]);
        t1 = N(Vector3.Lerp(t0, t4, 0.25f));
        t2 = N(Vector3.Lerp(t0, t4, 0.50f));
        t3 = N(Vector3.Lerp(t0, t4, 0.75f));

        l1 = N(Vector3.Lerp(t0, c, 0.25f));
        l2 = N(Vector3.Lerp(t0, c, 0.50f));
        l3 = N(Vector3.Lerp(t0, c, 0.75f));

        r1 = N(Vector3.Lerp(t4, c, 0.25f));
        r2 = N(Vector3.Lerp(t4, c, 0.50f));
        r3 = N(Vector3.Lerp(t4, c, 0.75f));

        i1 = N(Vector3.Lerp(l1, r1, 0.333f));
        i2 = N(Vector3.Lerp(l1, r1, 0.666f));
        i3 = N(Vector3.Lerp(l2, r2, 0.50f));

        //LOD1
        AddTriangle(c, t0, t4, LOD1_Vertices);

        //LOD2
        AddTriangle(c , l2, r2, LOD2_Vertices);
        AddTriangle(l2, t2, r2, LOD2_Vertices);
        AddTriangle(l2, t0, t2, LOD2_Vertices);
        AddTriangle(r2, t2, t4, LOD2_Vertices);


        // 3 levels of LOD
        // unless coastal tile
        //
        //R0------R1-----R2-----R3-----R4
        // \   \   |  \  |   /  |  /   /
        //  t0----t1-----t2-----t3----t4
        //   \    /\     /\     /\    /
        //    \  /  \   /  \   /  \  /
        //     l1-----i1-----i2----r1
        //      \     /\     /\    /
        //       \   /  \   /  \  /  
        //        l2-----i3-----r2
        //         \     /\     /
        //          \   /  \   /
        //           l3------r3
        //            \      /
        //             \    /
        //               c
        //LOD3
        AddTriangle(c, l3, r3, LOD3_Vertices);
        AddTriangle(l3, l2, i3, LOD3_Vertices);
        AddTriangle(l3, i3, r3, LOD3_Vertices);
        AddTriangle(r3, i3, r2, LOD3_Vertices);

        AddTriangle(l2, l1, i1, LOD3_Vertices);
        AddTriangle(l1, t0, t1, LOD3_Vertices);
        AddTriangle(l1, t1, i1, LOD3_Vertices);
        AddTriangle(i1, t1, t2, LOD3_Vertices);

        AddTriangle(l2, i1, i3, LOD3_Vertices);
        AddTriangle(i3, i2, r2, LOD3_Vertices);
        AddTriangle(i3, i1, i2, LOD3_Vertices);
        AddTriangle(i1, t2, i2, LOD3_Vertices);

        AddTriangle(r2, i2, r1, LOD3_Vertices);
        AddTriangle(i2, t2, t3, LOD3_Vertices);
        AddTriangle(i2, t3, r1, LOD3_Vertices);
        AddTriangle(r1, t3, t4, LOD3_Vertices);

    }

    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, List<Vector3> vertices)
    {
        int VertexIndex = vertices.Count;

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        
    }

    private Vector3 N(Vector3 input)
    {
        Vector3 result = input.normalized * 100;
        float elevation = SimplexNoise.Noise.Generate(result);
        return result * (1 + elevation / 1000);
    }




}

