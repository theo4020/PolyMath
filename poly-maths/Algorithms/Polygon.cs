using System;
using System.Collections.Generic;
using System.Linq;

namespace PolyMaths.Algorithms
{
    [Serializable]
    public class Polygon
    {
        private static int _idCounter = 0;

        public int Id { get; private set; }
        public string Name { get; set; }
        public List<Point2D> Vertices { get; private set; }
        public bool IsLocked { get; set; }

        public Polygon(List<Point2D> vertices = null, string name = null)
        {
            Id = ++_idCounter;
            Vertices = vertices ?? new List<Point2D>();
            Name = name ?? string.Format("Polygon {0}", Id);
        }

        public bool IsEmpty { get { return Vertices.Count == 0; } }
        public bool IsClosed { get { return Vertices.Count >= 3; } }

        public List<Tuple<Point2D, Point2D>> GetEdges()
        {
            var edges = new List<Tuple<Point2D, Point2D>>();
            if (Vertices.Count < 2) return edges;

            for (int i = 0; i < Vertices.Count; i++)
            {
                Point2D p1 = Vertices[i];
                Point2D p2 = Vertices[(i + 1) % Vertices.Count];
                edges.Add(Tuple.Create(p1, p2));
            }

            return edges;
        }

        public bool IsConvex()
        {
            if (Vertices.Count < 3) return false;

            bool? sign = null;
            int n = Vertices.Count;

            for (int i = 0; i < n; i++)
            {
                Point2D p1 = Vertices[i];
                Point2D p2 = Vertices[(i + 1) % n];
                Point2D p3 = Vertices[(i + 2) % n];

                Point2D v1 = p2 - p1;
                Point2D v2 = p3 - p2;
                float cross = v1.Cross(v2);

                if (Math.Abs(cross) > 1e-6f)
                {
                    bool currentSign = cross > 0;
                    if (!sign.HasValue)
                        sign = currentSign;
                    else if (currentSign != sign.Value)
                        return false;
                }
            }

            return true;
        }

        public Tuple<Point2D, Point2D> GetBoundingBox()
        {
            if (IsEmpty)
                return Tuple.Create(new Point2D(0, 0), new Point2D(0, 0));

            float minX = Vertices.Min(v => v.x);
            float maxX = Vertices.Max(v => v.x);
            float minY = Vertices.Min(v => v.y);
            float maxY = Vertices.Max(v => v.y);

            return Tuple.Create(new Point2D(minX, minY), new Point2D(maxX, maxY));
        }

        public Point2D GetCenter()
        {
            if (IsEmpty) return new Point2D(0, 0);

            float cx = Vertices.Average(v => v.x);
            float cy = Vertices.Average(v => v.y);
            return new Point2D(cx, cy);
        }

        public float GetArea()
        {
            if (Vertices.Count < 3) return 0f;

            float area = 0f;
            int n = Vertices.Count;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += Vertices[i].x * Vertices[j].y;
                area -= Vertices[j].x * Vertices[i].y;
            }

            return Math.Abs(area) / 2f;
        }

        public float GetPerimeter()
        {
            if (Vertices.Count < 2) return 0f;

            float perimeter = 0f;
            foreach (var edge in GetEdges())
            {
                perimeter += edge.Item1.DistanceTo(edge.Item2);
            }

            return perimeter;
        }

        public bool ContainsPoint(Point2D point)
        {
            if (Vertices.Count < 3) return false;

            int n = Vertices.Count;
            bool inside = false;

            int j = n - 1;
            for (int i = 0; i < n; i++)
            {
                float xi = Vertices[i].x, yi = Vertices[i].y;
                float xj = Vertices[j].x, yj = Vertices[j].y;

                if (((yi > point.y) != (yj > point.y)) &&
                    (point.x < (xj - xi) * (point.y - yi) / (yj - yi) + xi))
                {
                    inside = !inside;
                }

                j = i;
            }

            return inside;
        }

        public int? FindVertexAt(Point2D point, float threshold)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                if (Vertices[i].DistanceTo(point) <= threshold)
                    return i;
            }
            return null;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1} vertices]", Name, Vertices.Count);
        }
    }
}
