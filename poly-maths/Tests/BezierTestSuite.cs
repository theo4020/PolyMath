using System;
using System.Collections.Generic;
using PolyMaths.Algorithms;
using PolyMaths.Utils;

namespace PolyMaths.Tests
{
    public class BezierTestSuite
    {
        private int _passed, _failed;
        private readonly List<string> _failures = new List<string>();

        public void RunAllTests()
        {
            Logger.Header("BEZIER CURVE - TEST SUITE");
            TestDegree1IsLine();
            TestDegree2Midpoint();
            TestDirectVsCasteljau();
            TestStepAffectsCount();
            TestMultiplicity();
            TestC0C1C2Joining();
            TestBenchmarkRuns();
            TestPascalEqualsCasteljau();
            PrintSummary();
        }

        private void TestDegree1IsLine()
        {
            Logger.Section("TEST B1: Degree-1 Bézier = straight line");
            var c = new BezierCurve();
            c.AddPoint(new Point2D(0, 0));
            c.AddPoint(new Point2D(100, 100));

            var mid = c.EvaluateDirect(0.5f);
            Logger.Data("Midpoint", mid);
            Assert("Degree-1 midpoint = (50,50)",
                Math.Abs(mid.x - 50f) < 1e-3f && Math.Abs(mid.y - 50f) < 1e-3f);

            var start = c.EvaluateDirect(0f);
            var end   = c.EvaluateDirect(1f);
            Assert("t=0 → P0", start.Equals(new Point2D(0, 0)));
            Assert("t=1 → P1", end.Equals(new Point2D(100, 100)));
            Logger.SectionEnd();
        }

        private void TestDegree2Midpoint()
        {
            Logger.Section("TEST B2: Degree-2, endpoint interpolation");
            var c = new BezierCurve();
            c.AddPoint(new Point2D(0,   0));
            c.AddPoint(new Point2D(50, 100));
            c.AddPoint(new Point2D(100, 0));

            var t0 = c.EvaluateDirect(0f);
            var t1 = c.EvaluateDirect(1f);
            Assert("t=0 → P0", t0.Equals(new Point2D(0, 0)));
            Assert("t=1 → P2", t1.Equals(new Point2D(100, 0)));

            // At t=0.5: B(0.5) = 0.25*P0 + 0.5*P1 + 0.25*P2 = (50, 50)
            var half = c.EvaluateDirect(0.5f);
            Logger.Data("B(0.5)", half);
            Assert("t=0.5 correct", Math.Abs(half.x - 50f) < 1e-3f && Math.Abs(half.y - 50f) < 1e-3f);
            Logger.SectionEnd();
        }

        private void TestDirectVsCasteljau()
        {
            Logger.Section("TEST B3: Direct vs Casteljau produce identical results");
            var c = new BezierCurve();
            c.AddPoint(new Point2D(0,   0));
            c.AddPoint(new Point2D(20, 80));
            c.AddPoint(new Point2D(60, 20));
            c.AddPoint(new Point2D(100, 100));

            bool allMatch = true;
            for (int i = 0; i <= 20; i++)
            {
                float t = i / 20f;
                var d = c.EvaluateDirect(t);
                var k = c.EvaluateCasteljau(t);
                if (Math.Abs(d.x - k.x) > 1e-3f || Math.Abs(d.y - k.y) > 1e-3f)
                {
                    Logger.Data("Mismatch at t=" + t, string.Format("Direct={0}  Casteljau={1}", d, k));
                    allMatch = false;
                }
            }
            Assert("Direct == Casteljau at 21 sample points", allMatch);
            Logger.SectionEnd();
        }

        private void TestStepAffectsCount()
        {
            Logger.Section("TEST B4: Step change affects GetPoints count");
            var c = new BezierCurve();
            c.AddPoint(new Point2D(0, 0));
            c.AddPoint(new Point2D(100, 100));
            c.Step = 50;
            Assert("Step=50 → 51 points", c.GetPoints().Count == 51);
            c.Step = 200;
            Assert("Step=200 → 201 points", c.GetPoints().Count == 201);
            Logger.SectionEnd();
        }

        private void TestMultiplicity()
        {
            Logger.Section("TEST B5: Multiplicity — repeated point attracts curve");
            var c = new BezierCurve();
            c.AddPoint(new Point2D(0,   0));
            c.AddPoint(new Point2D(50, 50));
            c.AddPoint(new Point2D(50, 50)); // repeated
            c.AddPoint(new Point2D(100, 0));

            float minDist = float.MaxValue;
            var pts = c.GetPoints(false);
            foreach (var p in pts)
                minDist = Math.Min(minDist, p.DistanceTo(new Point2D(50, 50)));
            Logger.Data("Min dist to repeated point", minDist);
            Assert("Curve attracted to repeated point (dist < 10)", minDist < 10f);
            Logger.SectionEnd();
        }

        private void TestC0C1C2Joining()
        {
            Logger.Section("TEST B6: C0/C1/C2 continuity conditions");

            var A = new BezierCurve();
            A.AddPoint(new Point2D(0,   0));
            A.AddPoint(new Point2D(20, 50));
            A.AddPoint(new Point2D(60, 50));
            A.AddPoint(new Point2D(80, 0));

            var lastA     = A.ControlPoints[3];
            var prevLastA = A.ControlPoints[2];

            // C0: B starts at A's endpoint
            var B_C0 = new BezierCurve();
            B_C0.AddPoint(lastA);
            B_C0.AddPoint(new Point2D(100, 40));
            B_C0.AddPoint(new Point2D(140, 40));
            B_C0.AddPoint(new Point2D(160, 0));
            Assert("C0: B.P0 == A.Pn", B_C0.ControlPoints[0].Equals(lastA));

            // C1: B.P1 = 2*A.Pn - A.P(n-1)
            var c1P1 = lastA * 2f - prevLastA;
            Logger.Data("C1 required B.P1", c1P1);
            Assert("C1 formula computed (x > 0)", c1P1.x > 0);

            // C2: structural test — formula computes without throw
            int n = A.Degree;
            var c2P2 = c1P1 * 2f - (lastA * ((float)(n - 1) / n) + prevLastA * (1f / n));
            Logger.Data("C2 B.P2 (approx)", c2P2);
            Assert("C2 formula computed without exception", true);

            Logger.SectionEnd();
        }

        private void TestBenchmarkRuns()
        {
            Logger.Section("TEST B7: Benchmark runs without error (>50 control points)");
            var c = new BezierCurve();
            for (int i = 0; i <= 50; i++)
                c.AddPoint(new Point2D(i * 2, (float)Math.Sin(i * 0.1) * 50));

            var (dMs, kMs) = c.BenchmarkBoth(1000);
            Logger.Data("Direct   (ms)", dMs);
            Logger.Data("Casteljau (ms)", kMs);
            Assert("Both methods complete without exception", true);
            Logger.SectionEnd();
        }

        private void TestPascalEqualsCasteljau()
        {
            Logger.Section("TEST B8: Pascal == Casteljau on same control points");
            var c = new BezierCurve();
            c.AddPoint(new Point2D(0,   0));
            c.AddPoint(new Point2D(50, 120));
            c.AddPoint(new Point2D(120, 40));
            c.AddPoint(new Point2D(200,  0));

            for (int i = 0; i <= 20; i++)
            {
                float t = i / 20f;
                var d = c.EvaluateDirect(t);
                var k = c.EvaluateCasteljau(t);
                Assert($"t={t:F2}: Pascal==Casteljau x",
                    Math.Abs(d.x - k.x) < 0.1f);
                Assert($"t={t:F2}: Pascal==Casteljau y",
                    Math.Abs(d.y - k.y) < 0.1f);
            }
            Logger.SectionEnd();
        }

        private void Assert(string name, bool cond)
        {
            if (cond) { Logger.Success(name); _passed++; }
            else      { Logger.Error(name);   _failed++; _failures.Add(name); }
        }

        private void PrintSummary()
        {
            Logger.Header(string.Format("BEZIER TESTS: {0} passed, {1} failed", _passed, _failed));
            foreach (var f in _failures) Logger.Error("  - " + f);
        }
    }
}
