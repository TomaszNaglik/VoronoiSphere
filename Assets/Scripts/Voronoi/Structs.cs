using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Cell
{
    public Vector3 position;
    public int id;
    public int[] edges; //should be initialized to -1.
    public int edgesCount;
    public int[] wedges;

    public void AddEdge(Edge e)
    {
        if (edges == null)
            edges = new int[10];
        
        for (int i = edgesCount; i < edges.Length; i++)
        {
            if (edges[i] == 0)
            {
                edges[i] = e.id;
                edgesCount++;
                break;
            }
        }
    }

}
public struct Edge
{
    public int id;
    public int active;
    public Vector3 A;
    public Vector3 B;

    public Vector3 C1;
    public Vector3 C2;

    public int cell_1;
    public int cell_2;

    
    public override string ToString()
    {
        return "Edge:_" + id + "---" + cell_1 + "<>" + cell_2 + "---" + A.ToString() + "<>" + B.ToString() ;
    }
}

public struct Wedge
{
    public int id;
    public int edge;
    public int cell;

    public int[] vertices; //20 vertices
    public int[] triangles; //24 ints * 3
}

public struct Vertex
{
    public int id;
    public Vector3 position;
    public Vector3 normal;

}

public static class SizeOf
{
    public static int Cell = sizeof(float) * 3 + sizeof(int) * 1;
    public static int Edge = sizeof(float) * 12 + sizeof(int) * 4;
    public static int Wedge = +sizeof(int) * 3 + sizeof(int) * 20 + sizeof(int) * 24*3;
    public static int Vertex = sizeof(float) * 6 + sizeof(int) * 1;
}
