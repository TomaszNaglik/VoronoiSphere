using DelaunatorSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class SDelanuator
{
    int[] triangles;
    int[] halfEdges;
    Vector3[] points;
    private Delaunator delaunator;
    public List<Vector3> edgePoints;
    
    //private static float Planet_Radius = 10;

    public SDelanuator(Vector3[] _points)
    {
        delaunator = new Delaunator(ProjectStereographically(_points));
        Triangles = delaunator.Triangles;
        HalfEdges = delaunator.Halfedges;
        Points = _points;
        
        Augment();
        
    }
    public SDelanuator(Delaunator delaunator, Vector3[] _points)
    {
        Triangles = delaunator.Triangles;
        HalfEdges = delaunator.Halfedges;
        Points = _points;
        Augment();
    }



    public int[] Triangles { get => triangles; set => triangles = value; }
    public int[] HalfEdges { get => halfEdges; set => halfEdges = value; }
    public Vector3[] Points { get => points; set => points = value; }

    public void AddTriangle(int a, int b, int c)
    {
        List<int> t = Triangles.ToList();
        t.Add(a);
        t.Add(b);
        t.Add(c);

        Triangles = t.ToArray();
    }

    public void AddHalfEdge(int a)
    {
        List<int> t = HalfEdges.ToList();
        t.Add(a);
        

        HalfEdges = t.ToArray();
    }

    public void AddPoint(Vector3 a)
    {
        List<Vector3> t = Points.ToList();
        t.Add(a);


        Points = t.ToArray();
    }

    private void  Augment()
    {

        
        Vector3 lonePoint;
        edgePoints = new List<Vector3>();
        List<int> edgeIndices = new List<int>();

        for (int IndexEdgeFrom = 0; IndexEdgeFrom < Triangles.Length; IndexEdgeFrom++)
        {
            int IndexEdgeTo = HalfEdges[IndexEdgeFrom];
            int IndexOfPointFrom = Triangles[IndexEdgeFrom];

            if (IndexEdgeTo == -1)
            {

                edgeIndices.Add(IndexOfPointFrom);
                edgePoints.Add(Points[IndexOfPointFrom]);
               

            }
        }


        
        lonePoint = Vector3.zero;
        for (int i = 0; i < edgePoints.Count; i++)
        {
            lonePoint += edgePoints[i];
        }
        lonePoint /= edgePoints.Count;
        lonePoint = lonePoint.normalized;
        List<Vector3> controlPoints = new List<Vector3>(edgePoints);
        List<int> controlIndices = new List<int>(edgeIndices);

            
        edgePoints.Sort(new VParams.ClockwiseComparerVector3(lonePoint));

        for(int i = 0; i< edgePoints.Count; i++)
        {
            for(int j=0; j < edgePoints.Count; j++)
            {
                if(edgePoints[i] == controlPoints[j])
                {
                    edgeIndices[i] = controlIndices[j];
                }
            }
        }




        AddPoint(lonePoint);
        int startIndexOfAddedTriangles = Triangles.Length;
        for(int i=0; i < edgePoints.Count; i++) // edgePoints.Count
        {

            AddTriangle(edgeIndices[(i + 1) % (edgeIndices.Count)],edgeIndices[i], Points.Length - 1);

        }

        AddHalfEdges(startIndexOfAddedTriangles, Points.Length-1);

       
    }

    private void AddHalfEdges(int startIndexOfAddedTriangles, int lonePointIndex)
    {
        
        int InititalHalfEdgeCount = HalfEdges.Length;
        //Inititalize remaining halfedges to -1
        for(int i= InititalHalfEdgeCount; i < Triangles.Length; i++)
        {
            AddHalfEdge(-1);
        }
        //Utils.LogArray("Triangles: ", Triangles);
        //Utils.LogArray("HalfEdges after: ", HalfEdges);
        for (int i= InititalHalfEdgeCount; i< Triangles.Length; i++)
        {
            int p1 = Triangles[i];
            int p2 = (i +1) % 3 == 0 ? Triangles[i - 2] : Triangles[i + 1];
            int h = FindIndexOfEdge(p2, p1);
            /*if(h==-2)
            {
                 Utils.LogArray("Triangles: ", Triangles);
                Utils.LogArray("HalfEdges after: ", HalfEdges);
                Debug.LogFormat("Halfedges length {0} - h: {1}, Triangles length: {2}, p1: {3}, p2: {4}, i: {5}", HalfEdges.Length, h, Triangles.Length, p1, p2, i);
            }*/
            HalfEdges[i] = h;

            //Debug.LogFormat("Halfedges length {0} - h: {1}, Triangles length: {2}, p1: {3}, p2: {4}, i: {5}", HalfEdges.Length, h, Triangles.Length, p1, p2,i);
            if (HalfEdges[h] == -1)
            {
                HalfEdges[h] = i;
            }
        }


    }

    private int FindIndexOfEdge(int p1, int p2)
    {
        for(int i = 0; i < Triangles.Length; i++)
        {
            int v1 = Triangles[i];
            int v2 = (i + 1) % 3 == 0 ? Triangles[i - 2] : Triangles[i + 1];

            if (v1 == p1 && v2 == p2)
                return i;
        }
        return -2;
    }

    public int NextHalfedge(int e)
    {
        return (e % 3 == 2) ? e - 2 : e + 1; 
    }

    private IPoint[] ProjectStereographically(Vector3[] spherePoints)
    {
        int size = 1;
        IPoint[] projected = new IPoint[spherePoints.Length];
        for (int i = 0; i < spherePoints.Length; i++)
        {
            Vector3 projectedPoint = new Vector3();
            projectedPoint.x = spherePoints[i].x / (size - spherePoints[i].z) * size;
            projectedPoint.y = spherePoints[i].y / (size - spherePoints[i].z) * size;
            projectedPoint.z = 0;

            projected[i] = new Point(projectedPoint.x, projectedPoint.y);
        }

        return projected;
    }

}
