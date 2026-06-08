using System.Collections.Generic;
using Godot;

namespace MathsPower;

// Surfaces paramétrées du Cours 1 — formules verbatim du polycopié (z up).
public static class Surfaces
{
    // Sphère, § I : (θ, φ) ↦ (R cosθ sinφ, R sinθ sinφ, R cosφ).
    public static SurfaceGrid Sphere(float r, int m, int p)
    {
        var grid = new SurfaceGrid(m, p);
        for (int i = 0; i <= m; i++)
        {
            float theta = Mathf.Tau * i / m;
            float st = Mathf.Sin(theta), ct = Mathf.Cos(theta);
            for (int j = 0; j <= p; j++)
            {
                float phi = Mathf.Pi * j / p;
                float sp = Mathf.Sin(phi), cp = Mathf.Cos(phi);
                grid.Set(i, j, new Vector3(r * ct * sp, r * st * sp, r * cp));
            }
        }
        grid.ComputeNormals(true);
        return grid;
    }

    // Cylindre droit, (θ, h) ↦ (R cosθ, R sinθ, h).
    public static SurfaceGrid Cylinder(float r, float h, int m, int p)
    {
        var grid = new SurfaceGrid(m, p);
        for (int i = 0; i <= m; i++)
        {
            float theta = Mathf.Tau * i / m;
            float st = Mathf.Sin(theta), ct = Mathf.Cos(theta);
            for (int j = 0; j <= p; j++)
            {
                float hh = h * j / p;
                grid.Set(i, j, new Vector3(r * ct, r * st, hh));
            }
        }
        grid.ComputeNormals(false);
        return grid;
    }

    // Cône — cylindre à rayon variable (Thalès) : Rh = (1 - h/H) R.
    public static SurfaceGrid Cone(float r, float h, int m, int p)
    {
        var grid = new SurfaceGrid(m, p);
        for (int i = 0; i <= m; i++)
        {
            float theta = Mathf.Tau * i / m;
            float st = Mathf.Sin(theta), ct = Mathf.Cos(theta);
            for (int j = 0; j <= p; j++)
            {
                float hh = h * j / p;
                float rh = (1.0f - hh / h) * r;
                grid.Set(i, j, new Vector3(rh * ct, rh * st, hh));
            }
        }
        grid.ComputeNormals(false);
        return grid;
    }

    // Extrusion simple : x = f(t)·[h(p-1)+1], y = g(t)·[…], z = h·H.
    public static SurfaceGrid ExtrusionSimple(IReadOnlyList<Vector2> profile, float h, float pCoef, int p)
    {
        int m = System.Math.Max(profile.Count - 1, 0);
        var grid = new SurfaceGrid(m, p);
        for (int i = 0; i <= m; i++)
        {
            Vector2 pt = profile[i];
            for (int j = 0; j <= p; j++)
            {
                float hNorm = (float)j / p;
                float factor = hNorm * (pCoef - 1.0f) + 1.0f;
                grid.Set(i, j, new Vector3(pt.X * factor, pt.Y * factor, hNorm * h));
            }
        }
        grid.ComputeNormals(false);
        return grid;
    }

    // Révolution autour de (OZ ↔ OY polycopié) : σ(t, θ) = (f cosθ, f sinθ, g).
    public static SurfaceGrid Revolution(IReadOnlyList<Vector2> profile, int p)
    {
        int m = System.Math.Max(profile.Count - 1, 0);
        var grid = new SurfaceGrid(m, p);
        for (int i = 0; i <= m; i++)
        {
            Vector2 pt = profile[i];
            for (int j = 0; j <= p; j++)
            {
                float theta = Mathf.Tau * j / p;
                float st = Mathf.Sin(theta), ct = Mathf.Cos(theta);
                grid.Set(i, j, new Vector3(pt.X * ct, pt.X * st, pt.Y));
            }
        }
        grid.ComputeNormals(true);
        return grid;
    }

    // Extrusion généralisée : forme F glissée le long d'une âme A.
    // V = dA/ds (diffs finies), N = (0,0,1), U = (V∧N)/|V∧N|,
    // σ(t, s) = A(s) + x_f(t) U + y_f(t) N.
    public static SurfaceGrid ExtrusionGeneralisee(
        IReadOnlyList<Vector2> forme, IReadOnlyList<Vector2> ameXy, float[]? ameZ)
    {
        int pCount = ameXy.Count;

        var a = new Vector3[pCount];
        for (int i = 0; i < pCount; i++)
        {
            float z = ameZ != null ? ameZ[i] : 0.0f;
            a[i] = new Vector3(ameXy[i].X, ameXy[i].Y, z);
        }

        var s = new float[pCount];
        for (int i = 0; i < pCount; i++)
            s[i] = (float)i / (pCount - 1);

        var v = new Vector3[pCount];
        for (int i = 1; i < pCount - 1; i++)
        {
            float ds = s[i + 1] - s[i - 1];
            v[i] = (a[i + 1] - a[i - 1]) / ds;
        }
        v[0] = (a[1] - a[0]) / (s[1] - s[0]);
        v[pCount - 1] = (a[pCount - 1] - a[pCount - 2]) / (s[pCount - 1] - s[pCount - 2]);

        var nAxis = new Vector3(0, 0, 1);
        var u = new Vector3[pCount];
        for (int i = 0; i < pCount; i++)
        {
            Vector3 cross = v[i].Cross(nAxis);
            float len = cross.Length();
            u[i] = len > 1e-15f ? cross / len : new Vector3(1, 0, 0);
        }

        int m = System.Math.Max(forme.Count - 1, 0);
        int p = pCount - 1;
        var grid = new SurfaceGrid(m, p);
        for (int i = 0; i <= m; i++)
        {
            Vector2 f = forme[i];
            for (int j = 0; j <= p; j++)
                grid.Set(i, j, a[j] + u[j] * f.X + nAxis * f.Y);
        }
        grid.ComputeNormals(false);
        return grid;
    }
}
