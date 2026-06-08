using System;
using System.Collections.Generic;

namespace PolyMaths.Algorithms
{
    /// <summary>
    /// NURBS curve (Non-Uniform Rational B-Spline).
    /// Extends BSplineCurve by adding rational weights to each control point.
    /// C(t) = Σ(N_i,p(t) * w_i * P_i) / Σ(N_i,p(t) * w_i)
    /// </summary>
    public class NURBSCurve : BSplineCurve
    {
        protected List<float> _weights = new List<float>();
        public IReadOnlyList<float> Weights => _weights;

        /// <summary>Add a weighted control point.</summary>
        public void AddPoint(Point2D p, float weight)
        {
            _controlPoints.Add(p);
            _weights.Add(weight);
            RebuildKnots();
        }

        /// <summary>Override base AddPoint to also add weight = 1 (same as B-spline).</summary>
        public override void AddPoint(Point2D p)
        {
            AddPoint(p, 1f);
        }

        public void SetWeight(int i, float w)
        {
            if (i >= 0 && i < _weights.Count)
                _weights[i] = w;
        }

        /// <summary>
        /// Rational evaluation: C(t) = Σ(N_i * w_i * P_i) / Σ(N_i * w_i)
        /// </summary>
        public override Point2D Evaluate(float t)
        {
            float nx = 0f, ny = 0f, denom = 0f;
            for (int i = 0; i <= N; i++)
            {
                float b = Basis(i, Degree, t) * _weights[i];
                nx    += b * _controlPoints[i].x;
                ny    += b * _controlPoints[i].y;
                denom += b;
            }
            if (Math.Abs(denom) < 1e-10f)
                return _controlPoints.Count > 0 ? _controlPoints[0] : default;
            return new Point2D(nx / denom, ny / denom);
        }

        // ── Factory methods ──────────────────────────────────────────────────

        /// <summary>
        /// Exact NURBS circle using 9 control points, degree 2.
        /// Weight for corner points = sqrt(2)/2 ≈ 0.7071.
        /// </summary>
        public static NURBSCurve Circle(Point2D center, float r)
        {
            float w = (float)Math.Sqrt(2.0) / 2f; // cos(45°)
            var c = new NURBSCurve { Degree = 2 };
            // 9 control points going counter-clockwise
            c.AddPoint(new Point2D(center.x + r,  center.y    ), 1f);
            c.AddPoint(new Point2D(center.x + r,  center.y + r), w);
            c.AddPoint(new Point2D(center.x,      center.y + r), 1f);
            c.AddPoint(new Point2D(center.x - r,  center.y + r), w);
            c.AddPoint(new Point2D(center.x - r,  center.y    ), 1f);
            c.AddPoint(new Point2D(center.x - r,  center.y - r), w);
            c.AddPoint(new Point2D(center.x,      center.y - r), 1f);
            c.AddPoint(new Point2D(center.x + r,  center.y - r), w);
            c.AddPoint(new Point2D(center.x + r,  center.y    ), 1f);
            c.SetClamped();
            return c;
        }

        /// <summary>NURBS ellipse: same structure as circle but with semi-axes a, b.</summary>
        public static NURBSCurve Ellipse(Point2D center, float a, float b)
        {
            float w = (float)Math.Sqrt(2.0) / 2f;
            var c = new NURBSCurve { Degree = 2 };
            c.AddPoint(new Point2D(center.x + a, center.y    ), 1f);
            c.AddPoint(new Point2D(center.x + a, center.y + b), w);
            c.AddPoint(new Point2D(center.x,     center.y + b), 1f);
            c.AddPoint(new Point2D(center.x - a, center.y + b), w);
            c.AddPoint(new Point2D(center.x - a, center.y    ), 1f);
            c.AddPoint(new Point2D(center.x - a, center.y - b), w);
            c.AddPoint(new Point2D(center.x,     center.y - b), 1f);
            c.AddPoint(new Point2D(center.x + a, center.y - b), w);
            c.AddPoint(new Point2D(center.x + a, center.y    ), 1f);
            c.SetClamped();
            return c;
        }

        /// <summary>NURBS parabola arc (3 control points, degree 2, weight=0.5 for middle).</summary>
        public static NURBSCurve Parabola(Point2D vertex, float scale)
        {
            var c = new NURBSCurve { Degree = 2 };
            c.AddPoint(new Point2D(vertex.x - scale, vertex.y + scale), 1f);
            c.AddPoint(new Point2D(vertex.x,          vertex.y        ), 0.5f);
            c.AddPoint(new Point2D(vertex.x + scale, vertex.y + scale), 1f);
            c.SetClamped();
            return c;
        }

        /// <summary>NURBS hyperbola arc (right branch, 3 control points, degree 2, weight &lt; 1).</summary>
        public static NURBSCurve Hyperbola(Point2D center, float a, float b)
        {
            // Right branch: control polygon forms a "V" shape opening right
            float w = (float)Math.Sqrt(2.0) / 2f * 0.5f; // < 1/sqrt(2) gives hyperbola
            var c = new NURBSCurve { Degree = 2 };
            c.AddPoint(new Point2D(center.x + a, center.y - b), 1f);
            c.AddPoint(new Point2D(center.x + a, center.y    ), w);
            c.AddPoint(new Point2D(center.x + a, center.y + b), 1f);
            c.SetClamped();
            return c;
        }
    }
}
