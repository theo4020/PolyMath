using System;

namespace PolyMaths.Algorithms
{
    [Serializable]
    public struct Point2D : IEquatable<Point2D>
    {
        public float x;
        public float y;

        public Point2D(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Point2D operator +(Point2D a, Point2D b)
            => new Point2D(a.x + b.x, a.y + b.y);

        public static Point2D operator -(Point2D a, Point2D b)
            => new Point2D(a.x - b.x, a.y - b.y);

        public static Point2D operator *(Point2D p, float scalar)
            => new Point2D(p.x * scalar, p.y * scalar);

        public static Point2D operator *(float scalar, Point2D p)
            => new Point2D(p.x * scalar, p.y * scalar);

        public float Dot(Point2D other)
            => x * other.x + y * other.y;

        public float Cross(Point2D other)
            => x * other.y - y * other.x;

        public float Magnitude
            => (float)Math.Sqrt(x * x + y * y);

        public Point2D Normalized
        {
            get
            {
                float mag = Magnitude;
                return mag > 0 ? new Point2D(x / mag, y / mag) : new Point2D(0, 0);
            }
        }

        public float DistanceTo(Point2D other)
            => (this - other).Magnitude;

        public bool Equals(Point2D other)
            => Math.Abs(x - other.x) < 1e-6f && Math.Abs(y - other.y) < 1e-6f;

        public override string ToString()
            => string.Format("({0:F2}, {1:F2})", x, y);
    }
}
