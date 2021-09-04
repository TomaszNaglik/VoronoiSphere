using DelaunatorSharp;
using DelaunatorSharp.Unity.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using UnityEngine;
using Debug = UnityEngine.Debug;

public class SVoronoiMap : MonoBehaviour
{
    public int Number_Of_Points;
    public float Planet_Radius;
    public float GizmoSize;
    
    public int PointsPerChunk;
    public int NumChunksHeight;
    public int NumChunksWidth;

    public VoronoiMesh VoronoiMeshPrefab;
    private Vector3[] spherePoints;
    private Vector3[] projectedPoints;
    private Vector3[] learpedPoints;
    private IPoint[] projectedIPoints;
    private SDelanuator sDelanuator;
    private SVoronoiCell[] cells;
    
    private HashSet<int>[] Neighbours;
    private Vector3[] TriangleCenters;
     private VoronoiMesh[] mapChunks;

    public bool UpdateMap;
    [Range(0, 25)]
    public float JitterAmount;


    [Range(0,1)]
    public float offset;

    void Start()
    {
        VParams.map = this;
        
        InitializeMap();
    }

    void Update()
    {
        
        if(UpdateMap) InitializeMap();
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
            InitializeMap();
        }
        if (Input.GetMouseButtonDown(0))
        {
            
            HandleInput();
        }
    }

    void InitializeMap()
    {
        Clear();
        
        

        //mesh = GetComponent<MeshFilter>().mesh;
        mapChunks = GenerateMapChunks();
        spherePoints = GeneratePoints();
        projectedPoints = ProjectStereographically(spherePoints);
        learpedPoints = LearpedPoints(spherePoints, projectedPoints, offset);
        projectedIPoints = PointsFromVector3(projectedPoints);
        
         
        sDelanuator = new SDelanuator(new Delaunator(projectedIPoints), learpedPoints);
        learpedPoints = sDelanuator.Points;
        


        cells = GenerateVoronoiCell(sDelanuator.Points);
        SetupCells(cells);
        AssignCellsToChunks(cells);
        foreach (VoronoiMesh chunk in mapChunks)
        {
            chunk.Triangulate();
        }
        

    }

    private void Clear()
    {
        projectedIPoints = null;
        //delaunator = null;
        sDelanuator = null;
        cells = null;
        mapChunks = null;
        
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }


    }

    private Vector3 GetTriangleCenter ( Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ca = c - a;
        Vector3 ba = b - a;

        Vector3 baca = Vector3.Cross(ba, ca);
        float invDenominator = 0.5f / baca.sqrMagnitude;

        Vector3 numerator = Vector3.Cross(ca.sqrMagnitude * baca, ba) +
                          ba.sqrMagnitude * Vector3.Cross(ca, baca);

        Vector3 scaledNumerator = invDenominator * numerator;
        return a + scaledNumerator;



    }
    private void SetupCells(SVoronoiCell[] cells)
    {
        //Find Neighbours
        Neighbours = new HashSet<int>[sDelanuator.Points.Length];
        TriangleCenters = new Vector3[sDelanuator.Triangles.Length / 3];

        for (int i = 0; i < Neighbours.Length; i++)
        {
            Neighbours[i] = new HashSet<int>();
        }

        int IndexOfPointFrom, IndexOfPointFrom1, IndexOfPointFrom2, IndexOfPointFrom3;
        int IndexOfPointTo;
        int IndexEdgeTo;


        
        for (int IndexEdgeFrom = 0; IndexEdgeFrom < sDelanuator.Triangles.Length; IndexEdgeFrom += 3)
        {
            IndexEdgeTo = sDelanuator.HalfEdges[IndexEdgeFrom];
            IndexOfPointFrom1 = sDelanuator.Triangles[IndexEdgeFrom];
            IndexOfPointFrom2 = sDelanuator.Triangles[IndexEdgeFrom+1];
            IndexOfPointFrom3 = sDelanuator.Triangles[IndexEdgeFrom+2];
            IndexOfPointTo = sDelanuator.Triangles[sDelanuator.NextHalfedge(IndexEdgeFrom)];
            
            TriangleCenters[IndexEdgeFrom / 3] = GetTriangleCenter(sDelanuator.Points[IndexOfPointFrom1], sDelanuator.Points[IndexOfPointFrom2], sDelanuator.Points[IndexOfPointFrom3]);
            
        }

        for (int IndexEdgeFrom = 0; IndexEdgeFrom < sDelanuator.Triangles.Length; IndexEdgeFrom++)
        {
            IndexEdgeTo = sDelanuator.HalfEdges[IndexEdgeFrom];
            IndexOfPointFrom = sDelanuator.Triangles[IndexEdgeFrom];
            IndexOfPointTo = sDelanuator.Triangles[sDelanuator.NextHalfedge(IndexEdgeFrom)];

            Neighbours[IndexOfPointFrom].Add(IndexOfPointTo);
            Neighbours[IndexOfPointTo].Add(IndexOfPointFrom);

        }

        

        //Set neighbours inside cells
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].SetNeighbours(Neighbours[i], cells);
        }

        //Set edges
        for (int i = 0; i < sDelanuator.Triangles.Length; i++)
        {
            if (sDelanuator.HalfEdges[i] != -1)
            {
                int IndexOfFirstCellThatSharesEdge = sDelanuator.Triangles[i];
                int IndexOfSecondCellThatSharesEdge = sDelanuator.Triangles[sDelanuator.NextHalfedge(i)];
                int FirstTriangeOfEdge = i / 3;
                int SecondTriangleOfEdge = sDelanuator.HalfEdges[i] / 3;
                cells[IndexOfFirstCellThatSharesEdge].AddEdge(new SVoronoiEdge(TriangleCenters[FirstTriangeOfEdge], TriangleCenters[SecondTriangleOfEdge]));
                cells[IndexOfSecondCellThatSharesEdge].AddEdge(new SVoronoiEdge(TriangleCenters[FirstTriangeOfEdge], TriangleCenters[SecondTriangleOfEdge]));
            }


        }
        cells.ToList().ForEach(cell => cell.SetEdgeDetails());
       
    }
    private VoronoiMesh[] GenerateMapChunks()
    {
        VoronoiMesh[] chunks = new VoronoiMesh[NumChunksWidth*NumChunksHeight];
        for (int i = 0; i < chunks.Length; i++)
        {
            VoronoiMesh chunk = chunks[i] = Instantiate(VoronoiMeshPrefab);
            chunk.transform.SetParent(transform);

        }
        return chunks;
    }
    private SVoronoiCell[] GenerateVoronoiCell(Vector3[] points)
    {
        List<SVoronoiCell> cells = new List<SVoronoiCell>();

        for (int i = 0; i < points.Length; i++)
        {
            SVoronoiCell cell = new SVoronoiCell(i, points[i]);
            cells.Add(cell);
        }

        return cells.ToArray();
    }

    private void AssignCellsToChunks(SVoronoiCell[] cells)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            Vector2 polar = VParams.CartesianToPolar(cells[i].Position);
            float a = VParams.Map(polar.x, (float)-Math.PI / 2, (float)Math.PI / 2, 0, 1);
            float b = VParams.Map(polar.y, (float)-Math.PI, (float)Math.PI, 0, 1);

            int X = (int)(b * NumChunksWidth);
            int Y = (int)(a * NumChunksHeight);

            mapChunks[Y * NumChunksWidth + X].Cells.Add(cells[i]);
            cells[i].Chunk = mapChunks[Y * NumChunksWidth + X];
        }
    }

    

    private IPoint[] PointsFromVector3(Vector3[] projectedPoints)
    {
        IPoint[] result = new IPoint[projectedPoints.Length];
        for(int i = 0; i< result.Length; i++)
        {
            result[i] = new Point(projectedPoints[i].x, projectedPoints[i].y);
        }

        return result;
    }

    private Vector3[] ProjectStereographically(Vector3[] spherePoints)
    {
        Vector3[] projected = new Vector3[spherePoints.Length];
        for (int i = 0; i < spherePoints.Length; i++) 
        {
            Vector3 projectedPoint = new Vector3();
            projectedPoint.x = spherePoints[i].x / (Planet_Radius - spherePoints[i].z) * Planet_Radius;
            projectedPoint.y = spherePoints[i].y / (Planet_Radius - spherePoints[i].z) * Planet_Radius;
            projectedPoint.z = 0;

            projected[i] = projectedPoint;
        }

        return projected;
    }

    Vector3[] GeneratePoints()
    {   
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        Vector3[] points = new Vector3[Number_Of_Points];
        
            /*
            dlong := pi*(3-sqrt(5))  ~2.39996323 
            dz:= 2.0 / N
            long := 0
            z:= 1 - dz / 2
            for k := 0..N - 1
            r    := sqrt(1 - z * z)
            node[k] := (cos(long) * r, sin(long) * r, z)
            z    := z - dz
            long := long + dlong
            */
            

            

            double dl = (Math.PI * (3f - Math.Sqrt(5)));
            double dz = 2f / Number_Of_Points;
            double l = 0;
            double z = 1 - dz / 2f;

            for (int k = 0; k < Number_Of_Points; k++)
            {
                double r = Math.Sqrt(1 - z * z);
                double x = Math.Cos(l) * r;
                double y = Math.Sin(l) * r;

                points[k] = new Vector3((float)x, (float)z, (float)y) * Planet_Radius;
                points[k] = Vector3.Cross(points[k],  Offset()).normalized * Planet_Radius;
                z = z - dz;
                l = l + dl;

            }
        
       
        


        TimeSpan ts = stopWatch.Elapsed;
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
       Debug.Log("Generating points: " + elapsedTime);
        return points;

    }

    private Vector3 Offset()
    {
        return new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) * JitterAmount;
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            TouchCell(hit.point);
        }
    }

    void TouchCell(Vector3 position)
    {
        //VoronoiCell cell = VParams.GetCellAtPosition(position);// 

    }

    Vector3[] LearpedPoints(Vector3[] spherePoints, Vector3[] projectedPoints, float factor)
    {
        Vector3[] result = new Vector3[spherePoints.Length];

        for(int i=0; i<result.Length; i++)
        {
            result[i] = Vector3.Lerp(spherePoints[i], projectedPoints[i], factor);
        }

        return result;
    }

    /*void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;

        for(int i=0; i<sDelanuator.edgePoints.Count;i++)
        {
            Vector3 position = sDelanuator.edgePoints[i];
            Gizmos.color = Color.yellow;
            if (position.z < 0) Gizmos.color = Color.red;
            Gizmos.DrawSphere(position, GizmoSize);
            //Gizmos.DrawLine(position, lonePoint);
        }

        

    }*/
}
