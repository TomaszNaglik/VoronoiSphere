using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SVoronoiCell
{
    private int index;
    private Vector3 position;
    private List<SVoronoiCell> neighbours;
    private List<SVoronoiEdge> edges;

    private bool isMapEdge = false;
    private Color color;
    private VoronoiMesh chunk;
    //private Biome biome;
   
    private bool isPlateBorder;
    private float plateInterface;
    private bool isCoastal;
    private bool isLandmass;


    public List<SVoronoiEdge> Edges { get => edges; set => edges = value; }
    public List<SVoronoiCell> Neighbours { get => neighbours; set => neighbours = value; }
    public Vector3 Position { get => position; set => position = value; }
    public bool IsMapEdge { get => isMapEdge; set => isMapEdge = value; }
    public int Index { get => index; set => index = value; }
    public Color Color { get => color; set => color = value; }
    public VoronoiMesh Chunk { get => chunk; set => chunk = value; }
    //public Biome Biome { get => biome; set => biome = value; }
    
    public bool IsPlateBorder { get => isPlateBorder; set => isPlateBorder = value; }
    public float PlateInterface { get => plateInterface; set => plateInterface = value; }
    public bool IsCoastal { get => isCoastal; set => isCoastal = value; }
    public bool IsLandmass { get => isLandmass; set => isLandmass = value; }

    public SVoronoiCell(int _index, Vector3 _position)
    {
        this.Index = _index;
        this.Position = _position;
        this.Neighbours = new List<SVoronoiCell>();
        this.Edges = new List<SVoronoiEdge>();



    }





    internal void SetNeighbours(HashSet<int> _neighbours, SVoronoiCell[] cells)
    {

        /*string log = "";
        List<int> list = _neighbours.ToList();
        for (int i = 0; i< _neighbours.Count; i++)
        {
            log += "" + list[i] + ",";
        }
        Debug.Log("Neighbour indexes: " + log + " Cells count: " + cells.Length);
        */
        foreach (int n in _neighbours)
        {
            this.Neighbours.Add(cells[n]);
        }
        VParams.ClockwiseComparer comparer = new VParams.ClockwiseComparer(this.position);
        this.Neighbours.Sort(comparer);

    }

    public void AddEdge(SVoronoiEdge edge)
    {
        edge.Parent = this;
        Edges.Add(edge);
    }

    internal void UpdateColors()
    {
        foreach (SVoronoiEdge edge in Edges)
        {
            edge.UpdateColors();
        }
    }

    internal void SetEdgeDetails()
    {
        foreach (SVoronoiEdge edge in Edges)
        {
            edge.SetEdgeNeighbours();
        }
    }
    internal void SetCellDetails()
    {
        foreach (SVoronoiEdge edge in Edges)
        {
            
        }
    }

    internal SVoronoiCell FindPreviousNeighbour(int oppositeIndex)
    {
        if (oppositeIndex == 0) return Neighbours[Neighbours.Count - 1];
        return Neighbours[oppositeIndex - 1];
    }

    internal SVoronoiCell FindNextNeighbour(int oppositeIndex)
    {
        if (oppositeIndex == Neighbours.Count - 1) return Neighbours[0];

        return Neighbours[oppositeIndex + 1];
    }
}

