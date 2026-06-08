using Godot;

namespace MathsPower;

// Polyèdre de contrôle b_ij : (m+1) × (n+1) points 3D (z up), ligne-majeur.
// Le calcul de surface utilise les algorithmes de PolyMaths.Algorithms.
public sealed class ControlNet
{
    public int M { get; }
    public int N { get; }
    public Vector3[] Data { get; }

    public ControlNet(int m, int n)
    {
        M = m;
        N = n;
        Data = new Vector3[(m + 1) * (n + 1)];
    }

    public Vector3 Get(int i, int j) => Data[i * (N + 1) + j];
    public void Set(int i, int j, Vector3 v) => Data[i * (N + 1) + j] = v;

    // Conversion vers le format Point3D[,] attendu par BezierSurface.
    public PolyMaths.Algorithms.Point3D[,] ToPoint3DArray()
    {
        var arr = new PolyMaths.Algorithms.Point3D[M + 1, N + 1];
        for (int i = 0; i <= M; i++)
            for (int j = 0; j <= N; j++)
            {
                var v = Get(i, j);
                arr[i, j] = new PolyMaths.Algorithms.Point3D(v.X, v.Y, v.Z);
            }
        return arr;
    }
}
