using System.Collections.Generic;
using Godot;

namespace MathsPower;

// Évaluation de courbes de Bézier par De Casteljau, en 2D.
public static class Bezier
{
    // De Casteljau scalaire en 2D pour un unique paramètre t ∈ [0, 1].
    public static Vector2 DeCasteljau2D(IReadOnlyList<Vector2> points, float t)
    {
        int k = points.Count - 1;
        var buf = new Vector2[points.Count];
        for (int i = 0; i < points.Count; i++)
            buf[i] = points[i];
        for (int r = 1; r <= k; r++)
            for (int i = 0; i <= k - r; i++)
                buf[i] = buf[i].Lerp(buf[i + 1], t);
        return buf[0];
    }

    // Échantillonne une courbe de Bézier 2D en nSamples points (t = 0 → 1).
    // Si closed, on duplique le premier point en fin de polygone.
    public static List<Vector2> BezierCurve2D(IReadOnlyList<Vector2> control, int nSamples, bool closed)
    {
        var result = new List<Vector2>();
        if (control.Count == 0)
            return result;
        if (control.Count == 1)
        {
            for (int i = 0; i < nSamples; i++)
                result.Add(control[0]);
            return result;
        }

        var pts = new List<Vector2>(control);
        if (closed)
            pts.Add(pts[0]);

        if (nSamples == 0)
            return result;
        if (nSamples == 1)
        {
            result.Add(DeCasteljau2D(pts, 0.0f));
            return result;
        }

        float denom = nSamples - 1;
        for (int k = 0; k < nSamples; k++)
            result.Add(DeCasteljau2D(pts, k / denom));
        return result;
    }
}
