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
        public int  Step        { get; set; } = 100;
        public bool FillEnabled { get; set; } = false;
        public int  Degree => _controlPoints.Count - 1;

        // ── Control point management ─────────────────────────────────────────
        public void AddPoint(Point2D p)             { _controlPoints.Add(p);              _dirty = true; }
        public void InsertPoint(int index, Point2D p){ _controlPoints.Insert(index, p);     _dirty = true; }
        public void RemovePoint(int i)              { _controlPoints.RemoveAt(i);          _dirty = true; }
        public void MovePoint(int i, Point2D p)     { _controlPoints[i] = p; /* Pascal unaffected */ }

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

        /// <summary>Stats from a benchmark run (min/max/avg over several reps).</summary>
        public readonly struct BenchStats
        {
            public readonly double Min, Max, Avg;
            public readonly int Samples, Reps;
            public BenchStats(double[] times, int samples, int reps)
            {
                Samples = samples; Reps = reps;
                Min = double.MaxValue; Max = double.MinValue; double sum = 0;
                foreach (var t in times) { if (t < Min) Min = t; if (t > Max) Max = t; sum += t; }
                Avg = sum / times.Length;
            }
            public override string ToString() =>
                $"moy={Avg:F2}ms  min={Min:F2}ms  max={Max:F2}ms  ({Samples:N0}pts × {Reps} runs)";
        }

        /// <summary>Benchmark both Pascal and Casteljau with multiple reps for reliable stats.</summary>
        public (BenchStats pascal, BenchStats casteljau) BenchmarkBoth(int samples = 200_000, int reps = 5)
        {
            if (_controlPoints.Count < 2)
                return (new BenchStats(new double[]{0}, samples, reps),
                        new BenchStats(new double[]{0}, samples, reps));

            var dTimes = new double[reps];
            var kTimes = new double[reps];
            for (int r = 0; r < reps; r++)
            {
                double sink = 0;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i <= samples; i++) { var p = EvaluateDirect((float)i / samples); sink += p.x; }
                sw.Stop();
                dTimes[r] = sw.Elapsed.TotalMilliseconds;
                System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
                _ = sink; // prevent elimination

                sink = 0;
                sw.Restart();
                for (int i = 0; i <= samples; i++) { var p = EvaluateCasteljau((float)i / samples); sink += p.x; }
                sw.Stop();
                kTimes[r] = sw.Elapsed.TotalMilliseconds;
                _ = sink;
            }
            return (new BenchStats(dTimes, samples, reps), new BenchStats(kTimes, samples, reps));
        }

        /// <summary>Benchmark Pascal/Bernstein alone with multiple reps.</summary>
        public BenchStats BenchmarkPascal(int samples = 200_000, int reps = 5)
        {
            if (_controlPoints.Count < 2) return new BenchStats(new double[]{0}, samples, reps);
            var times = new double[reps];
            for (int r = 0; r < reps; r++)
            {
                double sink = 0;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i <= samples; i++) { var p = EvaluateDirect((float)i / samples); sink += p.x; }
                sw.Stop();
                times[r] = sw.Elapsed.TotalMilliseconds;
                _ = sink;
            }
            return new BenchStats(times, samples, reps);
        }
    }
}
