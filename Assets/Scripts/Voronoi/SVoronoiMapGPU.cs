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


    private int trianglesPerEdge = 2;
    private int[] TriangleIndices;
    private Vector3[] TriangleVertices;
    private int[] EdgeInMeshCounters;

    //ComputeShaders
    public ComputeShader CS_Edges;
    public ComputeShader CS_Mesh;

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
        //ComputeMeshData();
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

        TriangleIndices = null;
        EdgeInMeshCounters = null;
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
            //point *= Scale;
            //Debug.LogFormat("Point: {0} Scaled: {1}", point, point2);
            spherePoints[k] = point;


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


        TriangleIndices = new int[trianglesPerEdge * 3 * edges.Length];
        EdgeInMeshCounters = new int[chunks.Length];
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
        int maxNumEdgesPerChunk = 300;
        int trianglesPerEdge = 2;
        int verticesPerBigTriangle = 3;
        int edgeStride = trianglesPerEdge * verticesPerBigTriangle;
        int chunkStride = maxNumEdgesPerChunk * edgeStride;
        int numChunks = chunks.Length;
        int BufferSize = chunkStride * numChunks;
        


        ComputeBuffer Edges = new ComputeBuffer(edges.Length, SizeOf.Edge);
        ComputeBuffer Vertices = new ComputeBuffer(BufferSize, sizeof(float)*3);
        ComputeBuffer Indices = new ComputeBuffer(BufferSize, sizeof(int));
        ComputeBuffer ChunkCounters = new ComputeBuffer(chunks.Length, sizeof(int));
        Edges.SetData(edges);
        Vertices.SetData(TriangleVertices);
        Indices.SetData(TriangleIndices);
        ChunkCounters.SetData(EdgeInMeshCounters);


        CS_Mesh.SetBuffer(0, "Edges", Edges);
        CS_Mesh.SetBuffer(0, "TriangleVertices", Vertices);
        CS_Mesh.SetBuffer(0, "TriangleIndices", Indices);
        CS_Mesh.SetBuffer(0, "ChunkIndices", ChunkCounters);

        
        CS_Mesh.SetInt("TrianglesPerEdge", trianglesPerEdge);
        CS_Mesh.SetInt("VerticesPerBigTriangle", verticesPerBigTriangle);
        CS_Mesh.SetInt("ChunkStride", chunkStride);
        CS_Mesh.SetInt("EdgeStride", edgeStride);
        
        //Utils.LogArray("Edge counters: ", EdgeInMeshCounters);
        
        CS_Mesh.Dispatch(0, edges.Length / 10, 1, 1);

        ChunkCounters.GetData(EdgeInMeshCounters);
        ChunkCounters.Dispose();

        for (int i = 0; i < chunks.Length; i++)
        {
            Vector3[] vertices = new Vector3[EdgeInMeshCounters[i]* trianglesPerEdge * verticesPerBigTriangle];
            int[] indices = new int[EdgeInMeshCounters[i] * trianglesPerEdge * verticesPerBigTriangle];
            Vertices.GetData(vertices, 0, i * chunkStride , EdgeInMeshCounters[i]);
            Indices.GetData(indices, 0, i * chunkStride, EdgeInMeshCounters[i]);
            chunks[i].SetMeshData(vertices, indices);
        }
        
        Edges.Dispose();
        Vertices.Dispose();
        Indices.Dispose();

        //Debug.LogFormat("Edges count: {0}",edges.Length);
        //chunks[0].PrintData();
        //Debug.Log(edges.Length);
        Utils.LogArray("Edge counters: ", EdgeInMeshCounters);
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
            edges[i].chunkID = Y * NumChunksWidth + X;
            //if(maxEdges < chunks[Y * NumChunksWidth + X].edges.Count) maxEdges = chunks[Y * NumChunksWidth + X].edges.Count;
            //edges[i].Chunk = mapChunks[Y * NumChunksWidth + X];
        }
        //Debug.LogFormat("Max number of edges: {0}, Max number of vertices per chunk: {1}", maxEdges, maxEdges * 6);
        
        int maxEdges = 0;

        for (int i = 0; i < edges.Length; i++)
        {
            if(edges[i].chunkID < 0) Debug.Log(edges[i]);
        }
        for (int i = 0; i < edges.Length; i++)
        {
            //Debug.LogFormat("Edges count: {0}, current edge index: {1}, chunkID: {2} out of {3} chunks.", edges.Length, i, edges[i].chunkID, chunks.Length);
            chunks[edges[i].chunkID].edges.Add(edges[i]);
            if (maxEdges < chunks[edges[i].chunkID].edges.Count) maxEdges = chunks[edges[i].chunkID].edges.Count;
        }
        Debug.LogFormat("Max number of edges: {0}, Max number of vertices per chunk: {1}", maxEdges, maxEdges * 6);

        int[] edgeCount = new int[chunks.Length];
        for (int i = 0; i < edgeCount.Length; i++)
        {
            edgeCount[i] = chunks[i].edges.Count;
        }
        Utils.LogArray("Edges in Chunks: ", edgeCount);
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
