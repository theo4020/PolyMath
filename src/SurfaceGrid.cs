using Godot;

namespace MathsPower;

// Grille de surface échantillonnée (m+1) × (p+1). Stockage ligne-majeur :
// Data[i * (p+1) + j]. Points et normales en convention polycopié (z up) —
// la conversion vers Godot (y up) se fait dans MeshBuilder.
public sealed class SurfaceGrid
{
    public int M { get; }
    public int P { get; }
    public Vector3[] Data { get; }
    public Vector3[] Normals { get; }

    public SurfaceGrid(int m, int p)
    {
        M = m;
        P = p;
        int n = (m + 1) * (p + 1);
        Data = new Vector3[n];
        Normals = new Vector3[n];
        for (int i = 0; i < n; i++)
            Normals[i] = new Vector3(0, 0, 1);
    }

    public void Set(int i, int j, Vector3 v) => Data[i * (P + 1) + j] = v;
    public Vector3 Get(int i, int j) => Data[i * (P + 1) + j];

    // Calcule les normales par différences finies sur la grille.
    // `flip` inverse le produit vectoriel : pour sphère/révolution, ∂u × ∂v
    // donne la normale interne, donc on inverse pour l'externe.
    public void ComputeNormals(bool flip)
    {
        int np = P + 1;
        for (int i = 0; i <= M; i++)
        {
            for (int j = 0; j <= P; j++)
            {
                int iPrev = i > 0 ? i - 1 : 0;
                int iNext = i < M ? i + 1 : M;
                int jPrev = j > 0 ? j - 1 : 0;
                int jNext = j < P ? j + 1 : P;

                Vector3 du = Data[iNext * np + j] - Data[iPrev * np + j];
                Vector3 dv = Data[i * np + jNext] - Data[i * np + jPrev];
                Vector3 nRaw = flip ? dv.Cross(du) : du.Cross(dv);
                float len = nRaw.Length();

                if (len > 1e-9f)
                {
                    Normals[i * np + j] = nRaw / len;
                }
                else
                {
                    // Sommet dégénéré (pôle, apex) : direction vers un voisin valide.
                    int fallbackJ = j == 0 ? System.Math.Min(1, P) : System.Math.Max(P - 1, 0);
                    Vector3 dir = Data[i * np + j] - Data[i * np + fallbackJ];
                    float l = dir.Length();
                    Normals[i * np + j] = l > 1e-9f ? dir / l : new Vector3(0, 0, 1);
                }
            }
        }
    }

    public (float min, float max) ZRange()
    {
        float zmin = float.PositiveInfinity, zmax = float.NegativeInfinity;
        foreach (var v in Data)
        {
            if (v.Z < zmin) zmin = v.Z;
            if (v.Z > zmax) zmax = v.Z;
        }
        return (zmin, zmax);
    }
}
