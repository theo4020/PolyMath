using System;
using System.Collections.Generic;
using PolyMaths.Algorithms;
using PolyMaths.Utils;

namespace PolyMaths.Tests
{
    public class BSplineTestSuite
    {
        private int _passed, _failed;
        private readonly List<string> _failures = new List<string>();

        public void RunAllTests()
        {
            Logger.Header("BSPLINE / NURBS - TEST SUITE");
            TestUniformNotThroughEndpoints();
            TestClampedThroughEndpoints();
            TestDegree1IsPolyline();
            TestNurbsUnityWeightsEqualsBSpline();
            TestNurbsCircleCardinalPoints();
            TestCoxDeBoorDivisionByZeroGuard();
            TestDemoShapesNoException();
            PrintSummary();
        }

        private void TestUniformNotThroughEndpoints()
        {
            Logger.Section("TEST S1: Uniform BSpline does NOT pass through endpoints");
            var c = new BSplineCurve { Degree = 3 };
            c.AddPoint(new Point2D(0,   0));
            c.AddPoint(new Point2D(100, 200));
            c.AddPoint(new Point2D(200, 200));
            c.AddPoint(new Point2D(300, 0));
            c.SetUniform();

            var pts = c.GetPoints();
            Assert("Produces points", pts.Count > 0);
            if (pts.Count > 0)
            {
                float d0 = pts[0].DistanceTo(new Point2D(0, 0));
                Logger.Data("Distance from curve start to P0", d0);
                Assert("Uniform: start != P0 (dist > 1)", d0 > 1f);
            }
            Logger.SectionEnd();
        }

        private void TestClampedThroughEndpoints()
        {
            Logger.Section("TEST S2: Clamped BSpline DOES pass through endpoints");
            var c = new BSplineCurve { Degree = 3 };
            c.AddPoint(new Point2D(0,   0));
            c.AddPoint(new Point2D(100, 200));
            c.AddPoint(new Point2D(200, 200));
            c.AddPoint(new Point2D(300, 0));
            c.SetClamped();

            var pts = c.GetPoints();
            Assert("Produces points", pts.Count > 0);
            if (pts.Count > 0)
            {
                float d0 = pts[0].DistanceTo(new Point2D(0, 0));
                float d1 = pts[pts.Count - 1].DistanceTo(new Point2D(300, 0));
                Logger.Data("Distance curve start -> P0", d0);
                Logger.Data("Distance curve end   -> Pn", d1);
                Assert("Clamped: starts at P0 (dist < 1)", d0 < 1f);
                Assert("Clamped: ends at Pn   (dist < 1)", d1 < 1f);
            }
            Logger.SectionEnd();
        }

        private void TestDegree1IsPolyline()
        {
            Logger.Section("TEST S3: Degree-1 BSpline = polyline (passes through control points)");
            var c = new BSplineCurve { Degree = 1 };
            c.AddPoint(new Point2D(0,   0));
            c.AddPoint(new Point2D(50, 100));
            c.AddPoint(new Point2D(100, 0));

            var pts = c.GetPoints();
            Assert("Produces points", pts.Count > 0);
            if (pts.Count > 0)
            {
                // A degree-1 BSpline passes through all control points, so
                // minimum distance to P1=(50,100) should be very small
                float minDist = float.MaxValue;
                foreach (var p in pts)
                    minDist = Math.Min(minDist, p.DistanceTo(new Point2D(50, 100)));
                Logger.Data("Min dist to P1 (50,100)", minDist);
                Assert("Degree-1 passes near all control points (dist < 5)", minDist < 5f);
            }
            Logger.SectionEnd();
        }

        private void TestNurbsUnityWeightsEqualsBSpline()
        {
            Logger.Section("TEST S4: NURBS with weights=1 equals BSpline");
            var bs = new BSplineCurve { Degree = 3 };
            var nr = new NURBSCurve  { Degree = 3 };
            var ctrlPts = new Point2D[]
            {
                new Point2D(0,   0),
                new Point2D(50,  100),
                new Point2D(150, 100),
                new Point2D(200, 0)
            };
            foreach (var p in ctrlPts) { bs.AddPoint(p); nr.AddPoint(p, 1f); }

            var ptsBS = bs.GetPoints();
            var ptsNR = nr.GetPoints();

            Assert("Same number of points", ptsBS.Count == ptsNR.Count);
            bool allClose = true;
            for (int i = 0; i < Math.Min(ptsBS.Count, ptsNR.Count); i++)
            {
                if (ptsBS[i].DistanceTo(ptsNR[i]) > 1e-3f)
                {
                    Logger.Data("Mismatch at i=" + i, string.Format("BS={0}  NR={1}", ptsBS[i], ptsNR[i]));
                    allClose = false;
                    break;
                }
            }
            Assert("NURBS(w=1) == BSpline at all sampled points", allClose);
            Logger.SectionEnd();
        }

        private void TestNurbsCircleCardinalPoints()
        {
            Logger.Section("TEST S5: NURBS circle cardinal points are on the circle");
            var center = new Point2D(0, 0);
            float r    = 100f;
            var circle = NURBSCurve.Circle(center, r);
            circle.Step = 200;
            var pts = circle.GetPoints();
            Assert("Circle produces points", pts.Count > 0);

            if (pts.Count > 0)
            {
                // Find minimum distance to cardinal points: (r,0), (0,r), (-r,0), (0,-r)
                float dRight = float.MaxValue, dTop = float.MaxValue,
                      dLeft  = float.MaxValue, dBot = float.MaxValue;
                foreach (var p in pts)
                {
                    dRight = Math.Min(dRight, p.DistanceTo(new Point2D( r,  0)));
                    dTop   = Math.Min(dTop,   p.DistanceTo(new Point2D( 0,  r)));
                    dLeft  = Math.Min(dLeft,  p.DistanceTo(new Point2D(-r,  0)));
                    dBot   = Math.Min(dBot,   p.DistanceTo(new Point2D( 0, -r)));
                }
                Logger.Data("Min dist to ( r, 0)", dRight);
                Logger.Data("Min dist to ( 0, r)", dTop);
                Logger.Data("Min dist to (-r, 0)", dLeft);
                Logger.Data("Min dist to ( 0,-r)", dBot);
                Assert("Circle passes near ( r, 0) (dist < 5)", dRight < 5f);
                Assert("Circle passes near ( 0, r) (dist < 5)", dTop   < 5f);
                Assert("Circle passes near (-r, 0) (dist < 5)", dLeft  < 5f);
                Assert("Circle passes near ( 0,-r) (dist < 5)", dBot   < 5f);
            }
            Logger.SectionEnd();
        }

        private void TestCoxDeBoorDivisionByZeroGuard()
        {
            Logger.Section("TEST S6: Cox-de Boor 0/0 guard (degenerate: fewer points than degree)");
            var c = new BSplineCurve { Degree = 3 };
            // Only 2 control points for degree 3 → degenerate (empty result expected, no throw)
            c.AddPoint(new Point2D(0,   0));
            c.AddPoint(new Point2D(100, 100));

            bool threw = false;
            try { c.GetPoints(); }
            catch (Exception e) { threw = true; Logger.Error("Exception: " + e.Message); }
            Assert("No exception on degenerate curve (n < degree)", !threw);
            Logger.SectionEnd();
        }

        private void TestDemoShapesNoException()
        {
            Logger.Section("TEST S7: All NURBS demo shapes evaluate without exception");
            bool threw = false;
            try
            {
                var circle    = NURBSCurve.Circle   (new Point2D(200, 200), 80f);
                var ellipse   = NURBSCurve.Ellipse  (new Point2D(200, 200), 120f, 60f);
                var parabola  = NURBSCurve.Parabola (new Point2D(200, 300), 100f);
                var hyperbola = NURBSCurve.Hyperbola(new Point2D(200, 200), 60f,  80f);

                var pc = circle.GetPoints();
                var pe = ellipse.GetPoints();
                var pp = parabola.GetPoints();
                var ph = hyperbola.GetPoints();

                Logger.Data("Circle points",    pc.Count);
                Logger.Data("Ellipse points",   pe.Count);
                Logger.Data("Parabola points",  pp.Count);
                Logger.Data("Hyperbola points", ph.Count);

                Assert("Circle produces points",    pc.Count > 0);
                Assert("Ellipse produces points",   pe.Count > 0);
                Assert("Parabola produces points",  pp.Count > 0);
                Assert("Hyperbola produces points", ph.Count > 0);
            }
            catch (Exception e)
            {
                threw = true;
                Logger.Error("Exception: " + e.Message);
            }
            Assert("All demo shapes evaluate without exception", !threw);
            Logger.SectionEnd();
        }

        private void Assert(string name, bool cond)
        {
            if (cond) { Logger.Success(name); _passed++; }
            else      { Logger.Error(name);   _failed++; _failures.Add(name); }
        }

        private void PrintSummary()
        {
            Logger.Header(string.Format("BSPLINE TESTS: {0} passed, {1} failed", _passed, _failed));
            foreach (var f in _failures) Logger.Error("  - " + f);
        }
    }
}
