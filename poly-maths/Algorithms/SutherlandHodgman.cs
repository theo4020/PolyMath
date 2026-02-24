using System;
using System.Collections.Generic;

namespace PolyMaths.Algorithms
{
    public class SutherlandHodgman
    {
        private const float EPSILON = 1e-10f;

        public Polygon ClipPolygon(Polygon subject, Polygon clipWindow)
        {
            if (clipWindow == null)
            {
                Console.WriteLine("Warning: No clipping window defined");
                return subject;
            }

            var PL = new List<Point2D>(subject.Vertices);
            var windows = EnsureClockwise(clipWindow.Vertices);

            if (windows.Count < 3)
                return subject;

            for (int i = 0; i < windows.Count; i++)
            {
                Point2D A = windows[i];
                Point2D B = windows[(i + 1) % windows.Count];

                var PS = new List<Point2D>();
                int N1 = PL.Count;

                Point2D? F = null;
                Point2D? S = null;

                for (int j = 0; j < N1; j++)
                {
                    Point2D P = PL[j];

                    if (j == 0)
                    {
                        F = S = P;
                    }
                    else
                    {
                        if (Visible(S.Value, A, B) != Visible(P, A, B))
                        {
                            Point2D I = Intersection(S.Value, P, A, B);
                            PS.Add(I);
                        }
                    }

                    S = P;
                    if (Visible(S.Value, A, B))
                        PS.Add(S.Value);
                }

                if (PS.Count > 0 && F.HasValue)
                {
                    if (Visible(S.Value, A, B) != Visible(F.Value, A, B))
                    {
                        Point2D I = Intersection(S.Value, F.Value, A, B);
                        PS.Add(I);
                    }
                }

                PL = PS;
            }

            return new Polygon(PL);
        }

        private List<Point2D> EnsureClockwise(List<Point2D> vertices)
        {
            if (vertices.Count < 3)
                return vertices;

            float area = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                int j = (i + 1) % vertices.Count;
                area += (vertices[j].x - vertices[i].x) * (vertices[j].y + vertices[i].y);
            }

            if (area > 0)
            {
                var reversed = new List<Point2D>(vertices);
                reversed.Reverse();
                return reversed;
            }

            return vertices;
        }

        private bool Visible(Point2D P, Point2D A, Point2D B)
        {
            float dxEdge = B.x - A.x;
            float dyEdge = B.y - A.y;
            float dxPoint = P.x - A.x;
            float dyPoint = P.y - A.y;
            float cross = (dxEdge * dyPoint) - (dyEdge * dxPoint);
            return cross >= 0;
        }

        private Point2D Intersection(Point2D S, Point2D P, Point2D A, Point2D B)
        {
            float x1 = S.x, y1 = S.y;
            float x2 = P.x, y2 = P.y;
            float x3 = A.x, y3 = A.y;
            float x4 = B.x, y4 = B.y;

            float denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

            if (Math.Abs(denom) < EPSILON)
                return S;

            float tNum = (x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4);
            float t = tNum / denom;

            float ix = x1 + t * (x2 - x1);
            float iy = y1 + t * (y2 - y1);

            return new Point2D(ix, iy);
        }
    }
}
