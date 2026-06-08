using System;
using System.Collections.Generic;

namespace PolyMaths.Algorithms
{
    /// <summary>
    /// B-Spline curve of degree p with n+1 control points.
    /// Uses the Cox-de Boor recurrence for basis function evaluation.
    /// Supports uniform (open) and clamped knot vectors.
    /// </summary>
    public class BSplineCurve
    {
        protected List<Point2D> _controlPoints = new List<Point2D>();
        protected List<float>   _knots         = new List<float>();

        public IReadOnlyList<Point2D> ControlPoints => _controlPoints;
        public IReadOnlyList<float>   KnotVector    => _knots;

        public int  Degree    { get; set; } = 3;
        public int  Step      { get; set; } = 100;
        public bool IsClamped { get; protected set; } = true;

        /// <summary>n = last control point index = ControlPoints.Count - 1</summary>
        public int N => _controlPoints.Count - 1;

        // ── Control point management ─────────────────────────────────────────
        public virtual void AddPoint(Point2D p)
        {
            _controlPoints.Add(p);
            RebuildKnots();
        }

        public void RemovePoint(int i)
        {
            if (i < 0 || i >= _controlPoints.Count) return;
            _controlPoints.RemoveAt(i);
            RebuildKnots();
        }

        public void MovePoint(int i, Point2D p)
        {
            if (i >= 0 && i < _controlPoints.Count)
                _controlPoints[i] = p;
        }

        public void ApplyTransform(Matrix3x3 m)
        {
            for (int i = 0; i < _controlPoints.Count; i++)
                _controlPoints[i] = m.TransformPoint(_controlPoints[i]);
        }

        // ── Knot vector builders ─────────────────────────────────────────────
        public void SetUniform()  { IsClamped = false; RebuildKnots(); }
        public void SetClamped()  { IsClamped = true;  RebuildKnots(); }

        protected virtual void RebuildKnots()
        {
            int n = N;
            int p = Degree;
            // Knot vector size: n + p + 2 values (indices 0..n+p+1)
            int m = n + p + 1;

            _knots = new List<float>(m + 1);

            if (IsClamped)
            {
                // Clamped: p+1 zeros, interior uniform values, p+1 ones
                for (int i = 0; i <= m; i++)
                {
                    if      (i <= p)     _knots.Add(0f);
                    else if (i >= m - p) _knots.Add(1f);
                    else                 _knots.Add((float)(i - p) / (m - 2 * p));
                }
            }
            else
            {
                // Uniform open: 0, 1/m, 2/m, ..., 1
                for (int i = 0; i <= m; i++)
                    _knots.Add((float)i / m);
            }
        }

        // ── Cox-de Boor basis ────────────────────────────────────────────────
        /// <summary>
        /// Computes N(i, p, t) using the Cox-de Boor recurrence.
        /// 0/0 is treated as 0 (standard convention).
        /// </summary>
        protected float Basis(int i, int p, float t)
        {
            if (p == 0)
            {
                // Base case: 1 if t is in [knot[i], knot[i+1])
                if (i < 0 || i + 1 >= _knots.Count) return 0f;
                return (_knots[i] <= t && t < _knots[i + 1]) ? 1f : 0f;
            }

            float left = 0f, right = 0f;

            // Left term: (t - knot[i]) / (knot[i+p] - knot[i]) * N(i, p-1, t)
            if (i + p < _knots.Count)
            {
                float denom = _knots[i + p] - _knots[i];
                if (Math.Abs(denom) > 1e-10f)
                    left = (t - _knots[i]) / denom * Basis(i, p - 1, t);
            }

            // Right term: (knot[i+p+1] - t) / (knot[i+p+1] - knot[i+1]) * N(i+1, p-1, t)
            if (i + p + 1 < _knots.Count && i + 1 < _knots.Count)
            {
                float denom = _knots[i + p + 1] - _knots[i + 1];
                if (Math.Abs(denom) > 1e-10f)
                    right = (_knots[i + p + 1] - t) / denom * Basis(i + 1, p - 1, t);
            }

            return left + right;
        }

        // ── Evaluation ──────────────────────────────────────────────────────
        /// <summary>Evaluates the B-spline curve at parameter t: C(t) = Σ N(i,p,t) * Pi</summary>
        public virtual Point2D Evaluate(float t)
        {
            float x = 0f, y = 0f;
            for (int i = 0; i <= N; i++)
            {
                float b = Basis(i, Degree, t);
                x += b * _controlPoints[i].x;
                y += b * _controlPoints[i].y;
            }
            return new Point2D(x, y);
        }

        /// <summary>
        /// Returns Step+1 curve points sampled over the valid parameter domain [knot[p], knot[n+1]].
        /// Returns empty list if not enough control points.
        /// </summary>
        public virtual List<Point2D> GetPoints()
        {
            var pts = new List<Point2D>(Step + 1);
            if (_controlPoints.Count <= Degree || _knots.Count == 0) return pts;

            float tMin = _knots[Degree];
            float tMax = _knots[N + 1];
            if (tMax <= tMin) return pts;

            for (int i = 0; i <= Step; i++)
            {
                float t = tMin + (tMax - tMin) * (float)i / Step;
                // Clamp last sample slightly before tMax to stay within valid knot span
                if (i == Step) t = tMax - 1e-6f;
                pts.Add(Evaluate(t));
            }
            return pts;
        }
    }
}
