
using System.Collections.Generic;
using UnityEngine;

public class VParams

{
    public const int ChunksX = 20;
    public const int ChunksY = 10;
    public static int ChunkSize = 10;

    public static int CellRows = ChunksY * ChunkSize;
    public static int CellColumns = ChunksX * ChunkSize;
    public static int CellsActive = CellRows * CellColumns;

    public static float WorldWidth = 50;
    public static float WorldHeight = 25;

    

    public const float solidFactor = 0.95f;
    public const float blendFactor = 1 - solidFactor;

    public static float cellPositionJitterLowerBound = 0.05f;
    public static float cellPositionJitterUpperBound = 0.95f;

    public static SVoronoiMap map;

    

    internal static int GetMapQuadrant(Vector3 position)
    {
        int x = (int)((position.x / VParams.WorldWidth * VParams.CellColumns));
        int y = (int)((position.y / VParams.WorldHeight * VParams.CellRows));
       
        return y * (int)VParams.CellColumns + x; ;
    }

    internal static int GetMapChunk(Vector3 position)
    {
        int x = (int)(position.x / VParams.WorldWidth * VParams.ChunksX);
        int y = (int)(position.y / VParams.WorldHeight * VParams.ChunksY);
        
        return y * (int)VParams.ChunksX + x;

    }
    internal static Vector3 BoundedToMap(Vector3 position)
    {
        position = map.transform.InverseTransformPoint(position);
        position.x = position.x % VParams.WorldWidth;
        if (position.x < 0) position.x += VParams.WorldWidth;
        return position;
    }
    internal static SVoronoiCell GetCellAtPosition(Vector3 clickPosition)
    {

        
        return null;
    }

    internal static SVoronoiCell GetCellWithRay()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {

            return GetCellAtPosition(hit.point);

        }
        return null;
    }
    internal static SVoronoiEdge GetEdgeWithRay()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {

            SVoronoiCell cell =  GetCellAtPosition(hit.point);
            foreach(SVoronoiEdge e in cell.Edges)
            {
                if (PointInTriangle(e.Points[0], e.Points[e.Points.Count-1], cell.Position, BoundedToMap(hit.point))){
                    return e;
                }
            }
        }
        return null;
    }

    private static bool PointInTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
    {
        Vector3 d, e;
        float w1, w2;
        d = b - a;
        e = c - a;
        w1 = (e.x * (a.y - p.y) + e.y * (p.x - a.x)) / (d.x * e.y - d.y * e.x);
        w2 = (p.y - a.y - w1 * d.y) / e.y;
        return (w1 >= 0.0) && (w2 >= 0.0) && ((w1 + w2) <= 1.0);
    }

    internal static Vector3 GetPointOnPlanet(Vector3 p)
    {
        Vector3 result;
        result = p.normalized;
        result = map.Planet_Radius * result;

        return result;
    }

    internal static Vector3 NewVertex(Vector3 v1, Vector3 v2, float blendFactor)
    {
        Vector3 offset = (v2 - v1) * blendFactor;
        Vector3 result = new Vector3() + v1 + offset;
        
        return GetPointOnPlanet(result);

    }

    internal static Vector2 CartesianToPolar(Vector3 p)
    {
        float xzLen = new Vector2(p.x, p.z).magnitude;
        Vector2 result;
        result.y = Mathf.Atan2(p.x, p.z);
        result.x = Mathf.Atan2(-p.y, xzLen);
        
        return result;
    }

    public static Vector3 PolarToCartesian(Vector2 p)
    {
        Vector3 origin = new Vector3(0, 0, 1);
        Quaternion rotation = Quaternion.Euler(p.x, p.y, 0);
        Vector3 result = rotation * origin;
        return result;
    }

    internal static Vector3 GetCellPositionRelativeToMap(Vector3 cellPosition)
    {
        return new Vector3(cellPosition.x / WorldWidth, cellPosition.y / WorldHeight);
    }

    internal static float Map(float input, float input_start, float input_end, float output_start, float output_end)
    {
        return output_start + ((output_end - output_start) / (input_end - input_start)) * (input - input_start);
    }

    public class ClockwiseComparer : IComparer<SVoronoiCell>
    {
        private Vector3 m_Origin;
        public Vector3 origin { get { return m_Origin; } set { m_Origin = value; } }

        public ClockwiseComparer(Vector3 origin)
        {
            m_Origin = origin;
        }

       
        public int Compare(SVoronoiCell first, SVoronoiCell second)
        {
            return IsClockwise(first.Position, second.Position, m_Origin);
        }

        public static int IsClockwise(Vector3 first, Vector3 second, Vector3 origin)
        {
            if (first == second)
                return 0;

            Vector2 firstOffset = first - origin;
            Vector2 secondOffset = second - origin;

            float angle1 = Mathf.Atan2(firstOffset.x, firstOffset.y);
            float angle2 = Mathf.Atan2(secondOffset.x, secondOffset.y);

            if (angle1 < angle2)
                return -1;

            if (angle1 > angle2)
                return 1;

            // Check to see which point is closest
            return (firstOffset.sqrMagnitude < secondOffset.sqrMagnitude) ? -1 : 1;
        }

        
    }

    public class ClockwiseComparerVector3 : IComparer<Vector3>
    {
        private Vector3 m_Origin;
        public Vector3 origin { get { return m_Origin; } set { m_Origin = value; } }

        public ClockwiseComparerVector3(Vector3 origin)
        {
            m_Origin = origin;
        }


        public int Compare(Vector3 first, Vector3 second)
        {
            return IsClockwise(first, second, m_Origin);
        }

        public static int IsClockwise(Vector3 first, Vector3 second, Vector3 origin)
        {
            if (first == second)
                return 0;

            Vector2 firstOffset = first - origin;
            Vector2 secondOffset = second - origin;

            float angle1 = Mathf.Atan2(firstOffset.x, firstOffset.y);
            float angle2 = Mathf.Atan2(secondOffset.x, secondOffset.y);

            if (angle1 < angle2)
                return -1;

            if (angle1 > angle2)
                return 1;

            // Check to see which point is closest
            return (firstOffset.sqrMagnitude < secondOffset.sqrMagnitude) ? -1 : 1;
        }
    }

    


}


