using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PolyMaths.Algorithms
{
    /// <summary>
    /// Bézier curve of arbitrary degree n (n+1 control points).
    /// Provides both direct (Bernstein/Pascal) and iterative de Casteljau evaluation.
    /// </summary>
    public class BezierCurve
    {
        private readonly List<Point2D> _controlPoints = new List<Point2D>();
        private long[][] _pascalTriangle; // Full Pascal's triangle up to current degree
        private long[] _pascalRow;        // Current degree row: C(n,0)..C(n,n)
        private bool _dirty = true;       // True when degree changed → triangle cache invalid

        // ── Public properties ────────────────────────────────────────────────
        public IReadOnlyList<Point2D> ControlPoints => _controlPoints;
        public int Step { get; set; } = 100;
        public int Degree => _controlPoints.Count - 1;

        // ── Control point management ─────────────────────────────────────────
        public void AddPoint(Point2D p)        { _controlPoints.Add(p);              _dirty = true; }
        public void RemovePoint(int i)         { _controlPoints.RemoveAt(i);         _dirty = true; }
        public void MovePoint(int i, Point2D p){ _controlPoints[i] = p; /* Pascal unaffected by position change */ }

        public void ApplyTransform(Matrix3x3 m)
        {
            for (int i = 0; i < _controlPoints.Count; i++)
                _controlPoints[i] = m.TransformPoint(_controlPoints[i]);
        }

        // ── Pascal's triangle cache ──────────────────────────────────────────
        /// <summary>
        /// Builds the full Pascal's triangle up to row n using the additive rule:
        ///   T[i][0] = T[i][i] = 1
        ///   T[i][j] = T[i-1][j-1] + T[i-1][j]   (1 ≤ j ≤ i-1)
        /// The n-th row gives binomial coefficients C(n,0)..C(n,n).
        /// </summary>
        private void RebuildPascal()
        {
            int n = Degree;
            _pascalTriangle = new long[n + 1][];

            for (int i = 0; i <= n; i++)
            {
                _pascalTriangle[i] = new long[i + 1];
                _pascalTriangle[i][0] = 1;
                _pascalTriangle[i][i] = 1;
                for (int j = 1; j < i; j++)
                    _pascalTriangle[i][j] = _pascalTriangle[i - 1][j - 1] + _pascalTriangle[i - 1][j];
            }

            _pascalRow = _pascalTriangle[n]; // C(n,0), C(n,1), ..., C(n,n)
            _dirty = false;
        }

        // ── Evaluation ──────────────────────────────────────────────────────
        /// <summary>Direct Bernstein formula: B(t) = Σ C(n,i) * t^i * (1-t)^(n-i) * Pi</summary>
        public Point2D EvaluateDirect(float t)
        {
            int n = Degree;
            if (n < 0) return default;
            if (n == 0) return _controlPoints[0];
            if (_dirty) RebuildPascal();

            float u = 1f - t;
            float x = 0f, y = 0f;
            for (int i = 0; i <= n; i++)
            {
                float b = _pascalRow[i] * (float)Math.Pow(t, i) * (float)Math.Pow(u, n - i);
                x += b * _controlPoints[i].x;
                y += b * _controlPoints[i].y;
            }
            return new Point2D(x, y);
        }

        /// <summary>Iterative de Casteljau: reduces degree r times in-place.</summary>
        public Point2D EvaluateCasteljau(float t)
        {
            int n = _controlPoints.Count;
            if (n == 0) return default;
            if (n == 1) return _controlPoints[0];

            var d = new Point2D[n];
            for (int i = 0; i < n; i++) d[i] = _controlPoints[i];

            for (int r = 1; r < n; r++)
                for (int i = 0; i < n - r; i++)
                    d[i] = d[i] * (1f - t) + d[i + 1] * t;

            return d[0];
        }

        /// <summary>Returns Step+1 curve points sampled uniformly over [0,1].</summary>
        public List<Point2D> GetPoints(bool useCasteljau = false)
        {
            var pts = new List<Point2D>(Step + 1);
            if (_controlPoints.Count < 2) return pts;
            for (int i = 0; i <= Step; i++)
            {
                float t = (float)i / Step;
                pts.Add(useCasteljau ? EvaluateCasteljau(t) : EvaluateDirect(t));
            }
            return pts;
        }

        /// <summary>Benchmark both methods over many samples. Returns (directMs, casteljauMs).</summary>
        public (long directMs, long casteljauMs) BenchmarkBoth(int samples = 10000)
        {
            if (_controlPoints.Count < 2) return (0, 0);
            var sw = Stopwatch.StartNew();
            for (int i = 0; i <= samples; i++) EvaluateDirect((float)i / samples);
            sw.Stop(); long dMs = sw.ElapsedMilliseconds;

            sw.Restart();
            for (int i = 0; i <= samples; i++) EvaluateCasteljau((float)i / samples);
            sw.Stop();
            return (dMs, sw.ElapsedMilliseconds);
        }
    }
}
