using System;
using System.Collections.Generic;

namespace PolyMaths.Algorithms
{
    public struct Matrix3x3
    {
        private float[,] m;

        public Matrix3x3(float[,] values)
        {
            if (values.GetLength(0) != 3 || values.GetLength(1) != 3)
                throw new ArgumentException("Matrix must be 3x3");

            m = new float[3, 3];
            Array.Copy(values, m, 9);
        }

        public static Matrix3x3 Identity
        {
            get
            {
                return new Matrix3x3(new float[,]
                {
                    { 1, 0, 0 },
                    { 0, 1, 0 },
                    { 0, 0, 1 }
                });
            }
        }

        public static Matrix3x3 Translation(float tx, float ty)
        {
            return new Matrix3x3(new float[,]
            {
                { 1, 0, tx },
                { 0, 1, ty },
                { 0, 0,  1 }
            });
        }

        public static Matrix3x3 Rotation(float angleRadians)
        {
            float cos = (float)Math.Cos(angleRadians);
            float sin = (float)Math.Sin(angleRadians);

            return new Matrix3x3(new float[,]
            {
                { cos, -sin, 0 },
                { sin,  cos, 0 },
                {   0,    0, 1 }
            });
        }

        public static Matrix3x3 Scaling(float sx, float sy)
        {
            return new Matrix3x3(new float[,]
            {
                { sx,  0, 0 },
                {  0, sy, 0 },
                {  0,  0, 1 }
            });
        }

        public static Matrix3x3 Shearing(float shx, float shy)
        {
            return new Matrix3x3(new float[,]
            {
                { 1,   shx, 0 },
                { shy, 1,   0 },
                { 0,   0,   1 }
            });
        }

        public static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b)
        {
            float[,] result = new float[3, 3];

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    result[i, j] = 0;
                    for (int k = 0; k < 3; k++)
                    {
                        result[i, j] += a.m[i, k] * b.m[k, j];
                    }
                }
            }

            return new Matrix3x3(result);
        }

        public Point2D TransformPoint(Point2D point)
        {
            float x = m[0, 0] * point.x + m[0, 1] * point.y + m[0, 2];
            float y = m[1, 0] * point.x + m[1, 1] * point.y + m[1, 2];
            float w = m[2, 0] * point.x + m[2, 1] * point.y + m[2, 2];

            if (Math.Abs(w - 1f) > 1e-6f && Math.Abs(w) > 1e-6f)
            {
                x /= w;
                y /= w;
            }

            return new Point2D(x, y);
        }

        public List<Point2D> TransformPoints(List<Point2D> points)
        {
            var result = new List<Point2D>(points.Count);
            foreach (var point in points)
            {
                result.Add(TransformPoint(point));
            }
            return result;
        }

        public override string ToString()
        {
            return string.Format("[{0:F2} {1:F2} {2:F2}]\n[{3:F2} {4:F2} {5:F2}]\n[{6:F2} {7:F2} {8:F2}]",
                m[0, 0], m[0, 1], m[0, 2],
                m[1, 0], m[1, 1], m[1, 2],
                m[2, 0], m[2, 1], m[2, 2]);
        }
    }
}
