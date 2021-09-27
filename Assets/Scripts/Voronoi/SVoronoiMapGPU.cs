using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SVoronoiMapGPU : MonoBehaviour
{
    //Editor
    public bool UpdateFrame;
    public int NumberOfCells;
    public bool ShowPoints;
    public bool ShowTriangles;
    public bool ShowEdges;
    public bool ShowCells;
    
    
    [Range(0.0f, 0.03f)]
    public float Jitter;
    public int RandomSeed;
    [Range(1, 100)]
    public int Scale;

    private SDelanuator sDelanuator;
    private Vector3[] spherePoints;
    private PlanetChunkGPU[] chunks;
    private int NumChunksWidth = 20;
    private int NumChunksHeight = 20;
    public PlanetChunkGPU PlanetChunkPrefab;

    //Structs
    private Cell[] cells;
    private Edge[] edges;
    private Wedge[] wedges;
    private Vertex[] verticies;

    //ComputeShaders
    public ComputeShader CS_Edges;

    //General
    



    // Start is called before the first frame update
    void Start()
    {
        GenerateTerrainChunks();
        InitializeMap();
    }

   
    // Update is called once per frame
    void Update()
    {
        if (UpdateFrame)
        {
            //NumberOfCells++;
            InitializeMap();
        }
    }

    private void InitializeMap()
    {
        Reset();
        
        GeneratePoints();
        GenerateDelaunayTriangulation();
        SetData();
        GenerateCells();
        ComputeEdges();
        ComputeWedges();
        ComputeVertices();
        AssignEdgesToChunks();
        TriangulateChunks();

    }

    private void Reset()
    {
        sDelanuator = null;
        spherePoints = null;

        cells = null;
        edges = null;
        wedges = null;
        verticies = null;

        foreach(PlanetChunkGPU chunk in chunks)
        {
            chunk.Reset();
        }

        UnityEngine.Random.InitState(RandomSeed);

    }

    private void GenerateTerrainChunks()
    {
        chunks = new PlanetChunkGPU[NumChunksWidth * NumChunksHeight];
        for (int i = 0; i < chunks.Length; i++)
        {
            PlanetChunkGPU chunk = chunks[i] = Instantiate(PlanetChunkPrefab);
            chunk.transform.SetParent(transform);

        }
        
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

            
            Vector3 point = new Vector3((float)x, (float)y, (float)z);
            Vector3 noise = UnityEngine.Random.onUnitSphere*(Jitter);
            point = new Vector3(point.x + noise.x, point.y + noise.y, point.z + noise.z);
            
            point = point.normalized;
            point *= Scale;
            //Debug.LogFormat("Point: {0} Scaled: {1}", point, point2);
            spherePoints[k] = point;


            z = z - dz;
            l = l + dl;
        }
    }

    private void GenerateDelaunayTriangulation()
    {
        sDelanuator = new SDelanuator(spherePoints, Scale);
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
            cell.edgesCount = 0;
            cell.edges = new int[10];
            cells[i] = cell;
            
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
        CS_Edges.SetInt("Scale", Scale);

        CS_Edges.Dispatch(0, edges.Length / 10, 1, 1);

        edges_Buffer.GetData(edges);
        edges_Buffer.Dispose();
        Triangles.Dispose();
        Halfedges.Dispose();
        Points.Dispose();

        //Utils.LogArray("Edges: ", edges);

        foreach (Edge e in edges)
        {
            if (e.active==1)
            {
                cells[e.cell_1].AddEdge(e);
                cells[e.cell_2].AddEdge(e);
            }
        }
    }

    private void ComputeWedges()
    {
        //throw new NotImplementedException();
        /*
         for each wedge, create a vertex, see if that vertex exists.
         if it does not, add it to the vertices and get an id
         if it does, get an id

        and then create the vetices array and triangles array
         
         */
    }

    private void ComputeVertices()
    {
        verticies = new Vertex[edges.Length];
    }

    private void AssignCellsToChunks()
    {
        /*for (int i = 0; i < cells.Length; i++)
        {
            Vector2 polar = VParams.CartesianToPolar(cells[i].position);
            float a = VParams.Map(polar.x, (float)-Math.PI / 2, (float)Math.PI / 2, 0, 1);
            float b = VParams.Map(polar.y, (float)-Math.PI, (float)Math.PI, 0, 1);

            int X = (int)(b * NumChunksWidth);
            int Y = (int)(a * NumChunksHeight);

            mapChunks[Y * NumChunksWidth + X].Cells.Add(cells[i]);
            cells[i].Chunk = mapChunks[Y * NumChunksWidth + X];
        }*/
    }

    private void AssignEdgesToChunks()
    {
        for (int i = 0; i < edges.Length; i++)
        {
            Vector3 edgePosition = (edges[i].A + edges[i].B) / 2;
            Vector2 polar = VParams.CartesianToPolar(edgePosition);
            float a = VParams.Map(polar.x, (float)-Math.PI / 2, (float)Math.PI / 2, 0, 1);
            float b = VParams.Map(polar.y, (float)-Math.PI, (float)Math.PI, 0, 1);

            int X = (int)(b * NumChunksWidth);
            int Y = (int)(a * NumChunksHeight);
           
            chunks[Y * NumChunksWidth + X].edges.Add(edges[i]);
            //edges[i].Chunk = mapChunks[Y * NumChunksWidth + X];
        }
    }

    private void TriangulateChunks()
    {
        foreach(PlanetChunkGPU chunk in chunks)
        {
            chunk.Triangulate();
        }
    }


    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;

        if (ShowPoints)
        {
            Vector3 position;
            for (int i = 0; i < sDelanuator.Points.Length-1; i++)
            {
                position = sDelanuator.Points[i];
                Gizmos.color = Color.yellow;
                //if (position.z < 0) Gizmos.color = Color.red;
                Gizmos.DrawSphere(position, 0.015f);
                //Gizmos.DrawLine(position, lonePoint);
            }
            position = sDelanuator.Points[sDelanuator.Points.Length - 1];
            Gizmos.color = Color.cyan;
            //if (position.z < 0) Gizmos.color = Color.red;
            Gizmos.DrawSphere(position, 0.015f);
        }

        if (ShowTriangles)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                Gizmos.color = new Color(1, 1, 1, 1);

                Gizmos.DrawLine(sDelanuator.Points[edges[i].cell_1], sDelanuator.Points[edges[i].cell_2]);
            }
        }

        if (ShowEdges)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                
                Gizmos.color = new Color(0, 1, 0, 1);
               
                if (edges[i].active == 1) 
                {
                    Gizmos.DrawLine(edges[i].A, edges[i].B);
                }
            }
            
        }

        if (ShowCells)
        {
            
            for (int i = 0; i < cells.Length; i++)
            {

                for (int j = 0; j < cells[i].edgesCount; j++)
                {
                    Gizmos.color = new Color(0, 0, 1, 1);
                    //Gizmos.DrawLine(edges[cells[i].edges[j]].A, edges[cells[i].edges[j]].B);
                    Gizmos.DrawLine(cells[i].position, edges[cells[i].edges[j]].B);
                    Gizmos.DrawLine(cells[i].position, edges[cells[i].edges[j]].A);
                }
                
            }

           
        }
        

        

        

    }
}
