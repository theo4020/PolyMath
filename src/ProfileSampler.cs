using System.Collections.Generic;
using Godot;
using PolyMaths.Algorithms;

namespace MathsPower;

public enum ProfileCurveKind { Bezier = 0, BSpline = 1, NURBS = 2, Polygon = 3 }

// Échantillonne un polygone de contrôle 2D selon le type de courbe choisi,
// en s'appuyant sur les algorithmes de PolyMaths.Algorithms (BezierCurve,
// BSplineCurve, NURBSCurve). Retourne `nSamples` points.
public static class ProfileSampler
{
    public static List<Vector2> Sample(
        IReadOnlyList<Vector2> ctrl, bool closed, ProfileCurveKind kind, int nSamples,
        int degree = 3, IReadOnlyList<float>? weights = null)
    {
        var result = new List<Vector2>();
        if (ctrl.Count < 2 || nSamples < 2)
            return result;

        // Polygone de contrôle effectif + poids associés (refermés si demandé).
        var pts = new List<Vector2>(ctrl);
        var w = new List<float>(ctrl.Count);
        for (int i = 0; i < ctrl.Count; i++)
            w.Add(weights != null && i < weights.Count ? weights[i] : 1.0f);
        if (closed) { pts.Add(ctrl[0]); w.Add(w[0]); }

        switch (kind)
        {
            case ProfileCurveKind.Bezier: return SampleBezier(pts, nSamples);
            case ProfileCurveKind.BSpline: return SampleBSpline(pts, nSamples, degree, null);
            case ProfileCurveKind.NURBS: return SampleBSpline(pts, nSamples, degree, w);
            default: return SamplePolygon(pts, nSamples);
        }
    }

    // Bézier (BezierCurve d'Enzo, De Casteljau)
    private static List<Vector2> SampleBezier(List<Vector2> pts, int nSamples)
    {
        var curve = new BezierCurve { Step = nSamples - 1 };
        foreach (var p in pts) curve.AddPoint(new Point2D(p.X, p.Y));
        var raw = curve.GetPoints(useCasteljau: true);
        return ToVec(raw, pts, nSamples);
    }

    // B-spline / NURBS (Cox-de Boor d'Enzo). `weights` non nul → NURBS : chaque
    // point porte son poids (1 = comme la B-spline ; > 1 attire la courbe, < 1
    // la repousse), ce qui est l'apport rationnel des poids.
    private static List<Vector2> SampleBSpline(List<Vector2> pts, int nSamples, int degree, IReadOnlyList<float>? weights)
    {
        int deg = Mathf.Clamp(degree, 1, pts.Count - 1);
        BSplineCurve curve;
        if (weights != null)
        {
            var nurbs = new NURBSCurve { Degree = deg, Step = nSamples - 1 };
            for (int i = 0; i < pts.Count; i++)
                nurbs.AddPoint(new Point2D(pts[i].X, pts[i].Y), weights[i]);
            curve = nurbs;
        }
        else
        {
            curve = new BSplineCurve { Degree = deg, Step = nSamples - 1 };
            foreach (var p in pts) curve.AddPoint(new Point2D(p.X, p.Y));
        }
        var raw = curve.GetPoints();
        return ToVec(raw, pts, nSamples);
    }

    // Polygone : segments droits échantillonnés uniformément
    private static List<Vector2> SamplePolygon(List<Vector2> pts, int nSamples)
    {
        var result = new List<Vector2>(nSamples);
        int segs = pts.Count - 1;
        for (int k = 0; k < nSamples; k++)
        {
            float t = (float)k / (nSamples - 1) * segs;
            int seg = Mathf.Min((int)t, segs - 1);
            float local = t - seg;
            result.Add(pts[seg].Lerp(pts[seg + 1], local));
        }
        return result;
    }

    // Convertit des Point2D → Vector2 ; complète à `nSamples` si la courbe
    // a renvoyé moins de points (repli sur le polygone).
    private static List<Vector2> ToVec(List<Point2D> raw, List<Vector2> fallback, int nSamples)
    {
        if (raw.Count < 2)
            return SamplePolygon(fallback, nSamples);
        var result = new List<Vector2>(raw.Count);
        foreach (var p in raw) result.Add(new Vector2(p.x, p.y));
        return result;
    }
}
