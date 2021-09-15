﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct Cell
{
    public Vector3 position;
    public int id;

}
public struct Edge
{
    public int id;
    public Vector3 A;
    public Vector3 B;

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
    public static int Edge = sizeof(float) * 6 + sizeof(int) * 3;
    public static int Wedge = +sizeof(int) * 3;
    public static int Vertex = sizeof(float) * 6;
}