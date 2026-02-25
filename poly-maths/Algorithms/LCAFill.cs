using System;
using System.Collections.Generic;

namespace PolyMaths.Algorithms
{
    public class LCAFill
    {
        public class EdgeEntry : IComparable<EdgeEntry>
        {
            public float YMax { get; set; }
            public float XCurrent { get; set; }
            public float InvSlope { get; set; }

            public EdgeEntry(float yMax, float xCurrent, float invSlope)
            {
                YMax = yMax;
                XCurrent = xCurrent;
                InvSlope = invSlope;
            }

            public int CompareTo(EdgeEntry other)
            {
                return XCurrent.CompareTo(other.XCurrent);
            }

            public override string ToString()
            {
                return string.Format("[yMax={0:F1}, x={1:F2}, 1/m={2:F3}]", YMax, XCurrent, InvSlope);
            }
        }

        private List<EdgeEntry> activeEdgeTable = new List<EdgeEntry>();
        private List<List<EdgeEntry>> scanLineTable;
        private int yMinGlobal;
        private int yMaxGlobal;

        public List<Tuple<Point2D, Point2D>> FillPolygon(Polygon polygon)
        {
            if (polygon == null || polygon.Vertices.Count < 3)
                return new List<Tuple<Point2D, Point2D>>();

            BuildScanLineTable(polygon);

            var segments = new List<Tuple<Point2D, Point2D>>();
            activeEdgeTable.Clear();

            for (int y = yMinGlobal; y < yMaxGlobal; y++)
            {
                UpdateActiveEdges(y);
                segments.AddRange(GetFillSegments(y));

                foreach (var edge in activeEdgeTable)
                {
                    edge.XCurrent += edge.InvSlope;
                }
            }

            return segments;
        }

        private void BuildScanLineTable(Polygon polygon)
        {
            var bbox = polygon.GetBoundingBox();
            Point2D minP = bbox.Item1;
            Point2D maxP = bbox.Item2;

            yMinGlobal = (int)Math.Floor(minP.y);
            yMaxGlobal = (int)Math.Ceiling(maxP.y);

            int size = Math.Max(0, yMaxGlobal + 1);
            scanLineTable = new List<List<EdgeEntry>>(size);
            for (int i = 0; i < size; i++)
                scanLineTable.Add(new List<EdgeEntry>());

            foreach (var edge in polygon.GetEdges())
            {
                Point2D p1 = edge.Item1;
                Point2D p2 = edge.Item2;

                if (Math.Abs(p1.y - p2.y) < 1e-6f)
                    continue;

                float yMin, yMax, xAtYMin, dx, dy;
                if (p1.y < p2.y)
                {
                    yMin = p1.y;
                    yMax = p2.y;
                    xAtYMin = p1.x;
                    dx = p2.x - p1.x;
                    dy = p2.y - p1.y;
                }
                else
                {
                    yMin = p2.y;
                    yMax = p1.y;
                    xAtYMin = p2.x;
                    dx = p1.x - p2.x;
                    dy = p1.y - p2.y;
                }

                float invSlope = dx / dy;

                int yStart = (int)Math.Ceiling(yMin);
                int yEndExcl = (int)Math.Ceiling(yMax);

                if (yStart >= yEndExcl)
                    continue;

                // Advance x from the bottom vertex (yMin) to the first pixel scanline (yStart).
                // Without this, every edge whose yMin is not an integer starts at the wrong x.
                xAtYMin += (yStart - yMin) * invSlope;

                var entry = new EdgeEntry(yEndExcl, xAtYMin, invSlope);

                if (yStart >= 0 && yStart < scanLineTable.Count)
                {
                    scanLineTable[yStart].Add(entry);
                }
            }
        }

        private void UpdateActiveEdges(int y)
        {
            if (y >= 0 && y < scanLineTable.Count)
            {
                activeEdgeTable.AddRange(scanLineTable[y]);
            }

            activeEdgeTable.RemoveAll(e => y >= e.YMax);
            activeEdgeTable.Sort();
        }

        private List<Tuple<Point2D, Point2D>> GetFillSegments(int y)
        {
            var segments = new List<Tuple<Point2D, Point2D>>();
            int n = activeEdgeTable.Count;

            if (n < 2)
                return segments;

            for (int i = 0; i < n - 1; i += 2)
            {
                float x1 = activeEdgeTable[i].XCurrent;
                float x2 = activeEdgeTable[i + 1].XCurrent;

                if (x1 > x2)
                {
                    float temp = x1;
                    x1 = x2;
                    x2 = temp;
                }

                int xs = (int)Math.Ceiling(x1);
                int xe = (int)Math.Floor(x2);

                if (xs <= xe)
                {
                    segments.Add(Tuple.Create(new Point2D(xs, y), new Point2D(xe, y)));
                }
            }

            return segments;
        }

        public Tuple<int, int> GetYRange()
        {
            return Tuple.Create(yMinGlobal, yMaxGlobal);
        }

        public int GetActiveEdgeCount()
        {
            return activeEdgeTable.Count;
        }

        public List<EdgeEntry> GetActiveEdges()
        {
            return new List<EdgeEntry>(activeEdgeTable);
        }
    }
}
