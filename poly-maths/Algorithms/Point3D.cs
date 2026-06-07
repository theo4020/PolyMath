using Godot;
using System;

namespace PolyMaths.Algorithms
{
	[Serializable]
	public struct Point3D
	{
		public float x;
		public float y;
		public float z;

		public Point3D(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static Point3D operator +(Point3D a, Point3D b) => new Point3D(a.x + b.x, a.y + b.y, a.z + b.z);
		public static Point3D operator *(Point3D p, float scalar) => new Point3D(p.x * scalar, p.y * scalar, p.z * scalar);
		public static Point3D operator *(float scalar, Point3D p) => new Point3D(p.x * scalar, p.y * scalar, p.z * scalar);
	}
}
