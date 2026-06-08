using Godot;
using System;
using System.Collections.Generic;

namespace PolyMaths.Algorithms
{
    public class BezierSurface
    {
        private readonly Point3D[,] _controlPoints;

        public BezierSurface(Point3D[,] controlPoints)
        {
            _controlPoints = controlPoints;
        }

        public Point3D EvaluateDirect(float u, float v)
        {
            int numRows = _controlPoints.GetLength(0);
            int numCols = _controlPoints.GetLength(1);

            var intermediateCurve = new BezierCurve();

            for (int i = 0; i < numRows; i++)
            {
                var rowCurve = new BezierCurve();
                for (int j = 0; j < numCols; j++)
                {
                    rowCurve.AddPoint3D(_controlPoints[i, j]);
                }

                Point3D evaluatedRowPoint = rowCurve.EvaluateDirect3D(v);
                intermediateCurve.AddPoint3D(evaluatedRowPoint);
            }

            return intermediateCurve.EvaluateDirect3D(u);
        }
        
        public Point3D EvaluateDoubleCasteljau(float u, float v)
        {
            int numRows = _controlPoints.GetLength(0);
            int numCols = _controlPoints.GetLength(1);

            var intermediateCurve = new BezierCurve();

            for (int i = 0; i < numRows; i++)
            {
                var rowCurve = new BezierCurve();
                for (int j = 0; j < numCols; j++)
                {
                    rowCurve.AddPoint3D(_controlPoints[i, j]);
                }

                Point3D evaluatedRowPoint = rowCurve.EvaluateCasteljau3D(v);
                intermediateCurve.AddPoint3D(evaluatedRowPoint);
            }

            return intermediateCurve.EvaluateCasteljau3D(u);
        }
        
        private (Point3D[] left, Point3D[] right) SubdivideCurve1D(Point3D[] points)
        {
            int n = points.Length;
            Point3D[,] intermediate = new Point3D[n, n];

            for (int i = 0; i < n; i++) 
                intermediate[0, i] = points[i];

            // Algorithme de de Casteljau figé à t = 0.5f
            for (int r = 1; r < n; r++)
            {
                for (int i = 0; i < n - r; i++)
                {
                    intermediate[r, i] = intermediate[r - 1, i] * 0.5f + intermediate[r - 1, i + 1] * 0.5f;
                }
            }

            Point3D[] left = new Point3D[n];
            Point3D[] right = new Point3D[n];

            for (int i = 0; i < n; i++)
            {
                left[i] = intermediate[i, 0];
                right[i] = intermediate[n - 1 - i, i];
            }

            return (left, right);
        }

        public Point3D[][,] Subdivide()
        {
            int numRows = _controlPoints.GetLength(0);
            int numCols = _controlPoints.GetLength(1);

            Point3D[,] leftMesh = new Point3D[numRows, numCols];
            Point3D[,] rightMesh = new Point3D[numRows, numCols];

            for (int i = 0; i < numRows; i++)
            {
                Point3D[] row = new Point3D[numCols];
                for (int j = 0; j < numCols; j++) row[j] = _controlPoints[i, j];

                var (leftRow, rightRow) = SubdivideCurve1D(row);

                for (int j = 0; j < numCols; j++)
                {
                    leftMesh[i, j] = leftRow[j];
                    rightMesh[i, j] = rightRow[j];
                }
            }

            var quadrantNW = new Point3D[numRows, numCols];
            var quadrantSW = new Point3D[numRows, numCols];
            var quadrantNE = new Point3D[numRows, numCols];
            var quadrantSE = new Point3D[numRows, numCols];

            for (int j = 0; j < numCols; j++)
            {
                Point3D[] colLeft = new Point3D[numRows];
                Point3D[] colRight = new Point3D[numRows];
                for (int i = 0; i < numRows; i++)
                {
                    colLeft[i] = leftMesh[i, j];
                    colRight[i] = rightMesh[i, j];
                }

                var (topLeft, bottomLeft) = SubdivideCurve1D(colLeft);
                var (topRight, bottomRight) = SubdivideCurve1D(colRight);

                for (int i = 0; i < numRows; i++)
                {
                    quadrantNW[i, j] = topLeft[i];
                    quadrantSW[i, j] = bottomLeft[i];
                    quadrantNE[i, j] = topRight[i];
                    quadrantSE[i, j] = bottomRight[i];
                }
            }

            return new Point3D[][,] { quadrantNW, quadrantNE, quadrantSW, quadrantSE };
        }
    }
}