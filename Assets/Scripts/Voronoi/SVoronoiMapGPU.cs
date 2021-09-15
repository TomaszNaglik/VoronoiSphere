using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SVoronoiMapGPU : MonoBehaviour
{
    //Editor
    public bool UpdateFrame;
    public int NumberOfCells;
    public bool ShowPoints;
    public bool ShowTriangles;
    public bool ShowCells;

    private SDelanuator sDelanuator;
    private Vector3[] spherePoints;
    
    //Structs
    private Cell[] cells;
    private Edge[] edges;
    private Wedge[] wedges;
    private Vertex[] verticies;

    //ComputeShaders
    public ComputeShader CS_Edges;


    // Start is called before the first frame update
    void Start()
    {
        
        InitializeMap();
    }

   
    // Update is called once per frame
    void Update()
    {
        if (UpdateFrame)
        {
            NumberOfCells++;
            InitializeMap();
        }
    }

    private void InitializeMap()
    {
        Clear();
        GenerateTerrainChunks();
        GeneratePoints();
        GenerateDelaunayTriangulation();
        SetData();
        GenerateCells();
        ComputeEdges();
        ComputeWedges();
        ComputeVertices();
        AssignCellsToChunks();
        TriangulateChunks();

    }

    private void Clear()
    {
        sDelanuator = null;
        spherePoints = null;

        cells = null;
        edges = null;
        wedges = null;
        verticies = null;
    }

    private void GenerateTerrainChunks()
    {
       // throw new NotImplementedException();
    }

    private void GeneratePoints()
    {
        spherePoints = new Vector3[NumberOfCells];
        double dl = (Math.PI * (3f - Math.Sqrt(5)));
        double dz = 2f / NumberOfCells;
        double l = 0;
        double z = 1 - dz / 2f;

        for (int k = 0; k < NumberOfCells; k++)
        {
            double r = Math.Sqrt(1 - z * z);
            double x = Math.Cos(l) * r;
            double y = Math.Sin(l) * r;

            spherePoints[k] = new Vector3((float)x, (float)y, (float)z);
           
            z = z - dz;
            l = l + dl;
        }
    }

    private void GenerateDelaunayTriangulation()
    {
        sDelanuator = new SDelanuator(spherePoints);
        //Utils.LogArray("Triangles: ",sDelanuator.Triangles);
        //Utils.LogArray("HalfEdges: ",sDelanuator.HalfEdges);
        //Utils.LogArray("Points: ", sDelanuator.Points);
    }

   private void SetData()
    {
        cells = new Cell[sDelanuator.Points.Length];
        edges = new Edge[sDelanuator.Triangles.Length];
        wedges = new Wedge[edges.Length];
        verticies = new Vertex[wedges.Length * 3];
    }

    private void GenerateCells()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = new Cell();
            cell.id = i;
            cell.position = sDelanuator.Points[i];
            
        }
    }
    private void ComputeEdges()
    {
        ComputeBuffer edges_Buffer = new ComputeBuffer(edges.Length, SizeOf.Edge);
        ComputeBuffer Triangles = new ComputeBuffer(edges.Length, sizeof(int));
        ComputeBuffer Halfedges = new ComputeBuffer(edges.Length, sizeof(int));
        ComputeBuffer Points = new ComputeBuffer(edges.Length, sizeof(float) * 3);
        edges_Buffer.SetData(edges);
        Triangles.SetData(sDelanuator.Triangles);
        Halfedges.SetData(sDelanuator.HalfEdges);
        Points.SetData(sDelanuator.Points);
        
        CS_Edges.SetBuffer(0, "Edges_Buffer", edges_Buffer);
        CS_Edges.SetBuffer(0, "Triangles", Triangles);
        CS_Edges.SetBuffer(0, "Halfedges", Halfedges);
        CS_Edges.SetBuffer(0, "Points", Points);

        CS_Edges.Dispatch(0, edges.Length / 10, 1, 1);

        edges_Buffer.GetData(edges);
        edges_Buffer.Dispose();
        Triangles.Dispose();
        Halfedges.Dispose();
        Points.Dispose();

        //Utils.LogArray("Edges: ", edges);

        foreach (Edge e in edges)
        {

        }
    }

    private void ComputeWedges()
    {
        //throw new NotImplementedException();
    }

    private void ComputeVertices()
    {
       // throw new NotImplementedException();
    }

    private void AssignCellsToChunks()
    {
       // throw new NotImplementedException();
    }

    private void TriangulateChunks()
    {
        //throw new NotImplementedException();
    }


    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;

        if (ShowPoints)
        {
            for (int i = 0; i < sDelanuator.Points.Length; i++)
            {
                Vector3 position = sDelanuator.Points[i];
                Gizmos.color = Color.yellow;
                //if (position.z < 0) Gizmos.color = Color.red;
                Gizmos.DrawSphere(position, 0.05f);
                //Gizmos.DrawLine(position, lonePoint);
            }
        }

        if (ShowTriangles)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                Gizmos.color = new Color(1, 1, 1, 1);

                Gizmos.DrawLine(sDelanuator.Points[edges[i].cell_1], sDelanuator.Points[edges[i].cell_2]);
            }
        }

        if (ShowCells)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                //Gizmos.color = new Color(1, 0, 0, 1);
                //Gizmos.DrawSphere(edges[i].A, 0.02f);

                Gizmos.color = new Color(0, 1, 0, 1);
                //Gizmos.DrawSphere(edges[i].B, 0.02f);
                Gizmos.DrawLine(edges[i].A, edges[i].B);
            }
        }
        

        



    }
}
