using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class SVoronoiMapGPU : MonoBehaviour
{
    //Editor
    public bool UpdateFrame;

    public bool Render;
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
    private List<Edge> activeEdges;
    private Wedge[] wedges;
    private Vertex[] verticies;


    private int trianglesPerEdge = 2;
    private int[] TriangleIndices;
    private Vector3[] TriangleVertices;
    private int[] chunkEdgeCounters;


    //ComputeShaders
    public ComputeShader CS_Edges;
    public ComputeShader CS_Mesh;
    public ComputeShader CS_HeightMap;

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
            NumberOfCells++;
            InitializeMap();
        }
        
    }

    
    private void InitializeMap()
    {
        //Utils.StartTimer();
        Reset();
        //Utils.LogTime("Reset");
        GeneratePoints();
        //Utils.LogTime("GeneratePoints");
        GenerateDelaunayTriangulation();
        //Utils.LogTime("GenerateDelaunayTriangulation");
        SetData();
        //Utils.LogTime("SetData");
        GenerateCells();
        //Utils.LogTime("GenerateCells");
        ComputeEdges();
        //Utils.LogTime("ComputeEdges");
        AssignEdgesToChunks();
        //Utils.LogTime("AssignEdgesToChunks");
        if(Render) ComputeMeshData();
        //Utils.LogTime("ComputeMeshData");
        //TriangulateChunks();
        //Utils.LogTime("TriangulateChunks");
        //Utils.StopTimer();
        
    }

    private void Reset()
    {
        sDelanuator = null;
        spherePoints = null;

        cells = null;
        edges = null;
        wedges = null;
        verticies = null;
        activeEdges = null;
        chunkEdgeCounters = null;
        TriangleIndices = null;
        
        TriangleVertices = null;

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

            spherePoints[k] = point;


            z = z - dz;
            l = l + dl;
        }
    }

    private void GenerateDelaunayTriangulation()
    {
        sDelanuator = new SDelanuator(spherePoints);
        
    }

   private void SetData()
    {
        cells = new Cell[sDelanuator.Points.Length];
        edges = new Edge[sDelanuator.Triangles.Length];
        wedges = new Wedge[edges.Length];
        verticies = new Vertex[wedges.Length * 3];
        activeEdges = new List<Edge>();
        chunkEdgeCounters = new int[chunks.Length];

        TriangleIndices = new int[trianglesPerEdge * 3 * edges.Length];
        
        TriangleVertices = new Vector3[trianglesPerEdge * 3 * edges.Length];
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
        CS_Edges.SetInt("NumChunksWidth", NumChunksWidth);
        CS_Edges.SetInt("NumChunksHeight", NumChunksHeight);

        CS_Edges.Dispatch(0, edges.Length / 10, 1, 1);

        edges_Buffer.GetData(edges);
        edges_Buffer.Dispose();
        Triangles.Dispose();
        Halfedges.Dispose();
        Points.Dispose();

        foreach (Edge e in edges)
        {
            if (e.active==1)
            {
                cells[e.cell_1].AddEdge(e);
                cells[e.cell_2].AddEdge(e);
                activeEdges.Add(e);
            }

        }
        edges = activeEdges.ToArray();
    }

    private void ComputeMeshData()
    {
        /*
         * Create 2 2D buffers for vertices and indices
         * pass in Edges
         * pass in Mesh data counters
         * 
         * in the shader, go through each edge
         * - identify which chunk it is
         * - add data to vertices and indices in the current count
         * - atomic increment of count of that chunk
         * 
         * outside of shader
         * get vertex and index data per chunk
         * 
         */
        //Utils.StartTimer();
        int trianglesPerEdge = 2;
        int verticesPerBigTriangle = 3;
        int edgeStride = trianglesPerEdge * verticesPerBigTriangle;
        int BufferSize = edgeStride * edges.Length;

        
        //assume tht edges is ordered by chunk
        

        ComputeBuffer Edges = new ComputeBuffer(edges.Length, SizeOf.Edge);
        ComputeBuffer Vertices = new ComputeBuffer(TriangleVertices.Length, sizeof(float)*3);
        ComputeBuffer Indices = new ComputeBuffer(TriangleVertices.Length, sizeof(int));
        ComputeBuffer ChunkCounters = new ComputeBuffer(chunks.Length, sizeof(int));
        Edges.SetData(edges);
        Vertices.SetData(TriangleVertices);
        Indices.SetData(TriangleIndices);
        ChunkCounters.SetData(chunkEdgeCounters);


        CS_Mesh.SetBuffer(0, "Edges", Edges);
        CS_Mesh.SetBuffer(0, "TriangleVertices", Vertices);
        CS_Mesh.SetBuffer(0, "TriangleIndices", Indices);
        
               
        CS_Mesh.SetInt("EdgeStride", edgeStride);
        //Utils.LogTime("Setup CS_Mesh shader");
        CS_Mesh.Dispatch(0, edges.Length / 100, 1, 1);
        //Utils.LogTime("Distpached CS_Mesh shader");


        int cumulatedStride = 0;
        for (int i = 0; i < chunks.Length; i++)
        {
            int chunkStride = chunks[i].edges.Count * trianglesPerEdge * verticesPerBigTriangle;
            Vector3[] vertices = new Vector3[chunkStride];
            int[] indices = new int[chunkStride];
            Vertices.GetData(vertices, 0, cumulatedStride , chunkStride);
            Indices.GetData(indices, 0, cumulatedStride, chunkStride);
            chunks[i].SetMeshData(vertices, indices);
            cumulatedStride += chunkStride;
        }
        //Utils.LogTime("Finished looping");
        Edges.Dispose();
        Vertices.Dispose();
        Indices.Dispose();
        ChunkCounters.Dispose();

        //Utils.LogTime("Finished disposing");
        //Utils.StopTimer();

    }
       

    

    private void AssignEdgesToChunks()
    {
        //overrides what was calculates in CS_Edges
        for (int i = 0; i < edges.Length; i++)
        {
            Vector3 edgePosition = (edges[i].A + edges[i].B) / 2;
            Vector2 polar = VParams.CartesianToPolar(edgePosition);
            float a = VParams.Map(polar.x, (float)-Math.PI / 2, (float)Math.PI / 2, 0, 1);
            float b = VParams.Map(polar.y, (float)-Math.PI, (float)Math.PI, 0, 1);

            int X = (int)(b * NumChunksWidth);
            int Y = (int)(a * NumChunksHeight);
           
            chunks[Y * NumChunksWidth + X].edges.Add(edges[i]);
            edges[i].chunkID = Y * NumChunksWidth + X;
            chunkEdgeCounters[edges[i].chunkID]++;
           
        }

        List<Edge> orderedEdges = new List<Edge>();
        int[] edgeCount = new int[chunks.Length];
        for (int i = 0; i < chunks.Length; i++)
        {
            edgeCount[i] = chunks[i].edges.Count;
            orderedEdges.AddRange(chunks[i].edges);
        }
        edges = orderedEdges.ToArray();
        
              
        
        //Utils.LogArray("Edges in Chunks: ", edgeCount);
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
                position = sDelanuator.Points[i] * Scale;
                Gizmos.color = Color.yellow;
                //if (position.z < 0) Gizmos.color = Color.red;
                Gizmos.DrawSphere(position, 0.1f);
                //Gizmos.DrawLine(position, lonePoint);
            }
            position = sDelanuator.Points[sDelanuator.Points.Length - 1] * Scale;
            Gizmos.color = Color.cyan;
            //if (position.z < 0) Gizmos.color = Color.red;
            Gizmos.DrawSphere(position, 0.1f);
        }

        if (ShowTriangles)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                Gizmos.color = new Color(1, 1, 1, 1);

                Gizmos.DrawLine(sDelanuator.Points[edges[i].cell_1]*Scale, sDelanuator.Points[edges[i].cell_2] * Scale);
            }
        }

        if (ShowEdges)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                
                Gizmos.color = new Color(0, 1, 0, 1);
               
                if (edges[i].active == 1) 
                {
                    Gizmos.DrawLine(edges[i].A, edges[i].B );
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
