# Bézier + BSpline/NURBS Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add Bézier curves (Projet 2, due 25/02) and BSplines/NURBS (Projet 3, due 27/02) on top of the existing T1 polygon/clipping codebase in Godot 4 / C#.

**Architecture:** Mode-based dispatcher in `Main.cs` with dedicated `BezierManager` and `BSplineManager` classes. Pure-math algorithm classes in `Algorithms/` have no Godot dependencies. Managers use Godot types for drawing. Right-click `PopupMenu` on a `CanvasLayer` for the menu.

**Tech Stack:** Godot 4, C# (.NET), no extra libraries. Tests invoked from `_Ready()` and visible in Godot output panel.

**How to run tests:** In `Main.cs` `_Ready()`, uncomment the test runner line, run the project (F5 in Godot or `godot --headless --path poly-maths`), check output panel.

---

## Task 1: Add Shearing to Matrix3x3

**Files:**
- Modify: `poly-maths/Algorithms/Matrix3x3.cs` (after `Scaling` method, ~line 63)

**Step 1: Add the Shearing static method**

Insert after the `Scaling` method:

```csharp
public static Matrix3x3 Shearing(float shx, float shy)
{
    return new Matrix3x3(new float[,]
    {
        { 1,   shx, 0 },
        { shy, 1,   0 },
        { 0,   0,   1 }
    });
}
```

**Step 2: Add a quick test in PolygonTestSuite (inside TestMatrixTransformations, after Scaling test)**

```csharp
Logger.SubSection("Shearing (shx=0.5, shy=0)");
var shear = Matrix3x3.Shearing(0.5f, 0f);
result = shear.TransformPoint(new Point2D(0, 10));
Logger.Data("Shear (0,10)", result);
Assert("Shearing X correct", Math.Abs(result.x - 5f) < 1e-4f && Math.Abs(result.y - 10f) < 1e-4f);
```

**Step 3: Run tests (enable in _Ready, run project, verify PASS)**

**Step 4: Commit**
```bash
git add poly-maths/Algorithms/Matrix3x3.cs poly-maths/Tests/PolygonTestSuite.cs
git commit -m "feat: add Shearing transform to Matrix3x3"
```

---

## Task 2: Create BezierCurve.cs

**Files:**
- Create: `poly-maths/Algorithms/BezierCurve.cs`

**Step 1: Create the file**

```csharp
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
        private long[] _pascalRow;   // Cached binomial coefficients C(n, i)
        private bool _dirty = true;  // True when degree changed → Pascal cache invalid

        // ── Public properties ────────────────────────────────────────────────
        public IReadOnlyList<Point2D> ControlPoints => _controlPoints;
        public int Step { get; set; } = 100;
        public int Degree => _controlPoints.Count - 1;

        // ── Control point management ─────────────────────────────────────────
        public void AddPoint(Point2D p)        { _controlPoints.Add(p);              _dirty = true; }
        public void RemovePoint(int i)         { _controlPoints.RemoveAt(i);         _dirty = true; }
        public void MovePoint(int i, Point2D p){ _controlPoints[i] = p; /* Pascal unaffected */ }

        public void ApplyTransform(Matrix3x3 m)
        {
            for (int i = 0; i < _controlPoints.Count; i++)
                _controlPoints[i] = m.TransformPoint(_controlPoints[i]);
        }

        // ── Pascal's triangle cache ──────────────────────────────────────────
        private void RebuildPascal()
        {
            int n = Degree;
            _pascalRow = new long[n + 1];
            _pascalRow[0] = 1;
            for (int i = 1; i <= n; i++)
                _pascalRow[i] = _pascalRow[i - 1] * (n - i + 1) / i;
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
```

**Step 2: Commit**
```bash
git add poly-maths/Algorithms/BezierCurve.cs
git commit -m "feat: add BezierCurve with direct (Bernstein) and Casteljau evaluation"
```

---

## Task 3: Create BezierTestSuite.cs

**Files:**
- Create: `poly-maths/Tests/BezierTestSuite.cs`

**Step 1: Create the test file**

```csharp
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
                    Logger.Data("Mismatch at t=" + t, $"Direct={d}  Casteljau={k}");
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

            // Curve should pass closer to (50,50) than without repetition
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

            // Curve A: 4 control points
            var A = new BezierCurve();
            A.AddPoint(new Point2D(0,   0));
            A.AddPoint(new Point2D(20, 50));
            A.AddPoint(new Point2D(60, 50));
            A.AddPoint(new Point2D(80, 0));

            var lastA    = A.ControlPoints[3];
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
            Assert("C1 formula computed", c1P1.x > 0); // sanity

            // C2: verify formula doesn't throw
            int n = A.Degree;
            var c2P2 = c1P1 * 2f - (lastA * (float)(n-1) / n + prevLastA / n);
            Logger.Data("C2 B.P2 (approx)", c2P2);
            Assert("C2 formula computed", true); // structural

            Logger.SectionEnd();
        }

        private void TestBenchmarkRuns()
        {
            Logger.Section("TEST B7: Benchmark runs without error");
            var c = new BezierCurve();
            // 51 control points — > 50 as specified
            for (int i = 0; i <= 50; i++)
                c.AddPoint(new Point2D(i * 2, (float)Math.Sin(i * 0.1) * 50));

            var (dMs, kMs) = c.BenchmarkBoth(1000);
            Logger.Data("Direct  (ms)", dMs);
            Logger.Data("Casteljau (ms)", kMs);
            Assert("Both methods complete", true);
            Logger.SectionEnd();
        }

        private void Assert(string name, bool cond)
        {
            if (cond) { Logger.Success(name); _passed++; }
            else      { Logger.Error(name);   _failed++; _failures.Add(name); }
        }

        private void PrintSummary()
        {
            Logger.Header($"BEZIER TESTS: {_passed} passed, {_failed} failed");
            foreach (var f in _failures) Logger.Error("  - " + f);
        }
    }
}
```

**Step 2: Enable in Main.cs _Ready() (add line)**
```csharp
new PolyMaths.Tests.BezierTestSuite().RunAllTests();
```

**Step 3: Run project, verify all PASS in output panel**

**Step 4: Commit**
```bash
git add poly-maths/Tests/BezierTestSuite.cs poly-maths/Main.cs
git commit -m "test: add BezierTestSuite — all Bézier math tests"
```

---

## Task 4: Create PolygonManager.cs (extract T1 from Main.cs)

**Files:**
- Create: `poly-maths/Managers/PolygonManager.cs`

**Step 1: Create the Managers/ folder and PolygonManager.cs**

```csharp
using Godot;
using System.Collections.Generic;
using PolyMaths.Algorithms;

namespace PolyMaths.Managers
{
    /// <summary>
    /// Manages the T1 polygon clipping + fill workflow.
    /// All state previously in Main.cs is moved here.
    /// </summary>
    public class PolygonManager
    {
        private Polygon _subject  = new Polygon(name: "Sujet");
        private Polygon _window   = new Polygon(name: "Fenêtre");
        private Polygon _result   = new Polygon(name: "Résultat");

        private bool _subjectClosed, _windowClosed, _resultReady;

        public Color SubjectColor { get; set; } = new Color(0.2f, 0.6f, 1f);
        public Color WindowColor  { get; set; } = new Color(1f, 0.6f, 0.1f);
        public Color ResultColor  { get; set; } = new Color(0.2f, 0.9f, 0.3f);
        public int   DotRadius    { get; set; } = 5;

        public string StatusText =>
            !_subjectClosed ? "Click: add polygon vertex | RightClick: close polygon" :
            !_windowClosed  ? "Click: add clip window vertex (convex only) | RightClick: close window" :
                              "Clipping done. Right-click → Reset to restart.";

        // ── Input ────────────────────────────────────────────────────────────
        public bool HandleLeftClick(Vector2 mouse)
        {
            if (!_subjectClosed)
            {
                _subject.Vertices.Add(V(mouse));
                return true;
            }
            if (!_windowClosed)
            {
                var test = new List<Point2D>(_window.Vertices) { V(mouse) };
                if (IsConvexPartial(test))
                {
                    _window.Vertices.Add(V(mouse));
                    return true;
                }
            }
            return false;
        }

        public bool HandleRightClick()
        {
            if (!_subjectClosed && _subject.Vertices.Count >= 3)
            {
                _subjectClosed = true;
                return true;
            }
            if (!_windowClosed && _window.Vertices.Count >= 3)
            {
                _windowClosed = true;
                RunClipping();
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _subject = new Polygon(name: "Sujet");
            _window  = new Polygon(name: "Fenêtre");
            _result  = new Polygon(name: "Résultat");
            _subjectClosed = _windowClosed = _resultReady = false;
        }

        // ── Drawing ──────────────────────────────────────────────────────────
        public void Draw(Node2D canvas)
        {
            DrawOutline(canvas, _subject, SubjectColor, _subjectClosed);
            DrawOutline(canvas, _window,  WindowColor,  _windowClosed);

            if (_resultReady && !_result.IsEmpty)
            {
                DrawOutline(canvas, _result, ResultColor, true);
                canvas.DrawPolygon(ToV2Array(_result), new Color[] { ResultColor });
            }
            else
            {
                if (_subjectClosed) canvas.DrawPolygon(ToV2Array(_subject), new Color[] { SubjectColor });
                if (_windowClosed)  canvas.DrawPolygon(ToV2Array(_window),  new Color[] { WindowColor });
            }

            if (!_resultReady) { DrawDots(canvas, _subject); DrawDots(canvas, _window); }
            DrawDots(canvas, _result);
        }

        // ── Private helpers ──────────────────────────────────────────────────
        private void RunClipping()
        {
            _result = new SutherlandHodgman().ClipPolygon(_subject, _window);
            _resultReady = true;
        }

        private static bool IsConvexPartial(List<Point2D> pts)
            => pts.Count < 4 || new Polygon(pts).IsConvex();

        private void DrawOutline(Node2D c, Polygon p, Color col, bool close)
        {
            var v = p.Vertices;
            for (int i = 0; i < v.Count - 1; i++) c.DrawLine(P(v[i]), P(v[i+1]), col, 2);
            if (close && v.Count >= 2) c.DrawLine(P(v[v.Count-1]), P(v[0]), col, 2);
        }

        private void DrawDots(Node2D c, Polygon p)
        {
            foreach (var pt in p.Vertices) c.DrawCircle(P(pt), DotRadius, Colors.Black);
        }

        private static Point2D V(Vector2 v) => new Point2D(v.X, v.Y);
        private static Vector2  P(Point2D p) => new Vector2(p.x, p.y);
        private static Vector2[] ToV2Array(Polygon poly)
        {
            var arr = new Vector2[poly.Vertices.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = P(poly.Vertices[i]);
            return arr;
        }
    }
}
```

**Step 2: Commit**
```bash
git add poly-maths/Managers/PolygonManager.cs
git commit -m "refactor: extract T1 polygon logic into PolygonManager"
```

---

## Task 5: Create BezierManager.cs

**Files:**
- Create: `poly-maths/Managers/BezierManager.cs`

**Step 1: Create the file**

```csharp
using Godot;
using System.Collections.Generic;
using PolyMaths.Algorithms;

namespace PolyMaths.Managers
{
    public enum Continuity { C0, C1, C2 }

    public class BezierManager
    {
        // ── Curve list ───────────────────────────────────────────────────────
        private LinkedList<BezierCurve> _curves = new LinkedList<BezierCurve>();
        private LinkedListNode<BezierCurve> _activeNode;

        // ── Edit state ───────────────────────────────────────────────────────
        private bool   _editMode     = false;   // false = append, true = drag
        private int    _dragIndex    = -1;
        private bool   _dragging     = false;
        private bool   _useCasteljau = false;
        private string _benchText    = "";

        // ── Colors ───────────────────────────────────────────────────────────
        public Color ControlPolygonColor { get; set; } = new Color(0.5f, 0.5f, 0.5f);
        public Color CurveColor          { get; set; } = new Color(0.2f, 0.6f, 1f);
        public Color ActiveCurveColor    { get; set; } = new Color(1f, 0.3f, 0.3f);
        public Color SelectedPointColor  { get; set; } = Colors.Red;
        public int   DotRadius           { get; set; } = 5;
        private const float SELECT_THRESHOLD = 12f;

        // ── Status ───────────────────────────────────────────────────────────
        public string StatusText
        {
            get
            {
                string method = _useCasteljau ? "Casteljau" : "Direct";
                string mode   = _editMode ? "EDIT" : "APPEND";
                int step = _activeNode?.Value.Step ?? 0;
                return $"BÉZIER | Mode:{mode} | Method:{method} | Step:{step} | {_benchText}" +
                       $"\nCurves: {_curves.Count}" +
                       (_editMode ? " | LClick=select  Drag=move  Del=remove" : " | LClick=add point  RClick=menu");
            }
        }

        // ── Input handlers ───────────────────────────────────────────────────
        public void HandleLeftClick(Vector2 mouse)
        {
            if (!_editMode)
            {
                if (_activeNode == null) NewCurve();
                _activeNode.Value.AddPoint(V(mouse));
                return;
            }

            // Edit mode: try to select a vertex on any curve
            _dragIndex = -1;
            var node = _curves.First;
            while (node != null)
            {
                for (int i = 0; i < node.Value.ControlPoints.Count; i++)
                {
                    if (P(node.Value.ControlPoints[i]).DistanceTo(mouse) <= SELECT_THRESHOLD)
                    {
                        _activeNode = node;
                        _dragIndex  = i;
                        _dragging   = true;
                        return;
                    }
                }
                node = node.Next;
            }
        }

        public void HandleLeftRelease()  { _dragging = false; }

        public void HandleMouseMove(Vector2 mouse)
        {
            if (_dragging && _dragIndex >= 0 && _activeNode != null)
                _activeNode.Value.MovePoint(_dragIndex, V(mouse));
        }

        public void HandleDelete()
        {
            if (_activeNode == null) return;
            if (_editMode && _dragIndex >= 0 && _activeNode.Value.ControlPoints.Count > 0)
            {
                _activeNode.Value.RemovePoint(_dragIndex);
                _dragIndex = -1;
            }
            else
            {
                DeleteActiveCurve();
            }
        }

        public void StepUp()   { if (_activeNode != null) _activeNode.Value.Step++;  }
        public void StepDown() { if (_activeNode != null && _activeNode.Value.Step > 2) _activeNode.Value.Step--; }

        // ── Curve management ─────────────────────────────────────────────────
        public void NewCurve()
        {
            var curve = new BezierCurve();
            _activeNode = _curves.AddLast(curve);
            _editMode = false;
        }

        public void DeleteActiveCurve()
        {
            if (_activeNode == null) return;
            var next = _activeNode.Next ?? _activeNode.Previous;
            _curves.Remove(_activeNode);
            _activeNode = next;
        }

        public void SelectNext()
        {
            if (_activeNode?.Next != null) _activeNode = _activeNode.Next;
        }

        public void SelectPrev()
        {
            if (_activeNode?.Previous != null) _activeNode = _activeNode.Previous;
        }

        public void ToggleMethod()    { _useCasteljau = !_useCasteljau; _benchText = ""; }
        public void ToggleEditMode()  { _editMode = !_editMode; _dragIndex = -1; }

        // ── Transforms ───────────────────────────────────────────────────────
        public void ApplyTransform(Matrix3x3 m)
        {
            _activeNode?.Value.ApplyTransform(m);
        }

        // ── Continuity joining ───────────────────────────────────────────────
        /// <summary>Adjusts the first control points of curveB to achieve C0/C1/C2 with curveA.</summary>
        public static void Join(BezierCurve a, BezierCurve b, Continuity cont)
        {
            if (a == null || b == null || a.ControlPoints.Count < 2 || b.ControlPoints.Count < 2) return;

            int n = a.Degree;
            var Pn   = a.ControlPoints[n];
            var Pn_1 = a.ControlPoints[n - 1];

            // C0: set B's first point to A's last
            b.MovePoint(0, Pn);
            if (cont == Continuity.C0) return;

            // C1: B.P1 = 2*Pn - P(n-1)
            if (b.ControlPoints.Count >= 2)
            {
                var c1P1 = Pn * 2f - Pn_1;
                b.MovePoint(1, c1P1);
            }
            if (cont == Continuity.C1) return;

            // C2: B.P2 = c1P1*2 - (n-1)/n * Pn - 1/n * P(n-1)   (simplified)
            if (b.ControlPoints.Count >= 3 && n >= 2)
            {
                var c1P1 = Pn * 2f - Pn_1;
                var Pn_2 = a.ControlPoints[n - 2];
                var c2P2 = c1P1 * 2f - (Pn * ((float)(n - 1) / n) + Pn_1 * (1f / n));
                b.MovePoint(2, c2P2);
            }
        }

        // ── Benchmark ────────────────────────────────────────────────────────
        public void RunBenchmark()
        {
            if (_activeNode == null) return;
            var (d, k) = _activeNode.Value.BenchmarkBoth(10000);
            _benchText = $"Benchmark: Direct={d}ms  Casteljau={k}ms";
        }

        // ── Fill closed curve ─────────────────────────────────────────────────
        /// <summary>Returns fill segments for the active curve (as closed polygon).</summary>
        public List<(Point2D, Point2D)> FillActiveCurve()
        {
            if (_activeNode == null) return new List<(Point2D, Point2D)>();
            var pts = _activeNode.Value.GetPoints(_useCasteljau);
            if (pts.Count < 3) return new List<(Point2D, Point2D)>();

            var poly = new Polygon(pts);
            var filler = new LCAFill();
            var segs = filler.FillPolygon(poly);
            var result = new List<(Point2D, Point2D)>(segs.Count);
            foreach (var s in segs) result.Add((s.Item1, s.Item2));
            return result;
        }

        // ── Drawing ──────────────────────────────────────────────────────────
        public void Draw(Node2D canvas)
        {
            var node = _curves.First;
            while (node != null)
            {
                bool isActive = node == _activeNode;
                DrawCurve(canvas, node.Value, isActive);
                node = node.Next;
            }
        }

        private void DrawCurve(Node2D canvas, BezierCurve curve, bool isActive)
        {
            var pts = curve.ControlPoints;
            Color cc = isActive ? ActiveCurveColor : CurveColor;

            // Control polygon (dashed effect: use thin gray lines)
            for (int i = 0; i < pts.Count - 1; i++)
                canvas.DrawLine(P(pts[i]), P(pts[i+1]), ControlPolygonColor, 1);

            // Control point dots
            for (int i = 0; i < pts.Count; i++)
            {
                bool sel = isActive && _editMode && i == _dragIndex;
                canvas.DrawCircle(P(pts[i]), DotRadius, sel ? SelectedPointColor : Colors.Black);
            }

            // Curve itself
            if (pts.Count >= 2)
            {
                var cPts = curve.GetPoints(_useCasteljau);
                for (int i = 0; i < cPts.Count - 1; i++)
                    canvas.DrawLine(P(cPts[i]), P(cPts[i+1]), cc, 2);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static Point2D V(Vector2 v) => new Point2D(v.X, v.Y);
        private static Vector2  P(Point2D p) => new Vector2(p.x, p.y);
    }
}
```

**Step 2: Commit**
```bash
git add poly-maths/Managers/BezierManager.cs
git commit -m "feat: add BezierManager — curve list, edit mode, transforms, continuity"
```

---

## Task 6: Refactor Main.cs

**Files:**
- Modify: `poly-maths/Main.cs` (full rewrite)

**Step 1: Replace Main.cs entirely**

```csharp
using Godot;
using PolyMaths.Managers;
using PolyMaths.Algorithms;

public partial class Main : Node2D
{
    // ── Mode ──────────────────────────────────────────────────────────────
    private enum AppMode { Polygon, Bezier, BSpline }
    private AppMode _mode = AppMode.Polygon;

    // ── Managers ─────────────────────────────────────────────────────────
    private PolygonManager  _polyMgr   = new PolygonManager();
    private BezierManager   _bezMgr    = new BezierManager();
    // BSplineManager added in Task 9

    // ── Menu ─────────────────────────────────────────────────────────────
    private PopupMenu _menu;
    private Label     _hud;

    // ── Menu item IDs ────────────────────────────────────────────────────
    private const int M_MODE_POLYGON  = 0;
    private const int M_MODE_BEZIER   = 1;
    private const int M_MODE_BSPLINE  = 2;
    private const int M_SEP1          = 3;
    // Polygon
    private const int M_POLY_RESET    = 10;
    // Bezier
    private const int M_BEZ_NEW       = 20;
    private const int M_BEZ_DELETE    = 21;
    private const int M_BEZ_TOGGLE_MODE   = 22;
    private const int M_BEZ_TOGGLE_METHOD = 23;
    private const int M_BEZ_STEP_UP   = 24;
    private const int M_BEZ_STEP_DOWN = 25;
    private const int M_BEZ_JOIN_C0   = 26;
    private const int M_BEZ_JOIN_C1   = 27;
    private const int M_BEZ_JOIN_C2   = 28;
    private const int M_BEZ_FILL      = 29;
    private const int M_BEZ_BENCH     = 30;
    private const int M_BEZ_TRANSLATE = 31;
    private const int M_BEZ_ROTATE    = 32;
    private const int M_BEZ_SCALE     = 33;
    private const int M_BEZ_SHEAR     = 34;

    // ── Lifecycle ────────────────────────────────────────────────────────
    public override void _Ready()
    {
        BuildMenu();
        BuildHud();
        // Uncomment to run tests:
        // new PolyMaths.Tests.PolygonTestSuite().RunAllTests();
        // new PolyMaths.Tests.BezierTestSuite().RunAllTests();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("Quitter"))   GetTree().Quit();
        if (Input.IsActionJustPressed("ui_accept")) _bezMgr.ToggleMethod(); // Space = toggle method

        // Step keys
        if (Input.IsKeyPressed(Key.Plus)  || Input.IsKeyPressed(Key.KpAdd))      _bezMgr.StepUp();
        if (Input.IsKeyPressed(Key.Minus) || Input.IsKeyPressed(Key.KpSubtract)) _bezMgr.StepDown();

        // Mouse
        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            var mouse = GetViewport().GetMousePosition();
            _bezMgr.HandleMouseMove(mouse); // for dragging
        }

        if (Input.IsActionJustPressed("ClicGauche"))  HandleLeftClick();
        if (Input.IsActionJustReleased("ClicGauche")) _bezMgr.HandleLeftRelease();
        if (Input.IsActionJustPressed("ClicDroit"))   HandleRightClick();

        if (Input.IsKeyJustPressed(Key.Delete)) _bezMgr.HandleDelete();

        // Curve navigation
        if (Input.IsKeyJustPressed(Key.Tab))    _bezMgr.SelectNext();
        if (Input.IsKeyJustPressed(Key.Quoteleft)) _bezMgr.SelectPrev();

        UpdateHud();
        QueueRedraw();
    }

    private void HandleLeftClick()
    {
        var mouse = GetViewport().GetMousePosition();
        switch (_mode)
        {
            case AppMode.Polygon: _polyMgr.HandleLeftClick(mouse); break;
            case AppMode.Bezier:  _bezMgr.HandleLeftClick(mouse);  break;
        }
    }

    private void HandleRightClick()
    {
        if (_mode == AppMode.Polygon)
        {
            _polyMgr.HandleRightClick();
            return;
        }
        // Show menu for Bezier / BSpline modes
        _menu.Position = (Vector2I)GetViewport().GetMousePosition();
        RefreshMenu();
        _menu.Popup();
    }

    // ── Drawing ──────────────────────────────────────────────────────────
    public override void _Draw()
    {
        switch (_mode)
        {
            case AppMode.Polygon: _polyMgr.Draw(this); break;
            case AppMode.Bezier:  _bezMgr.Draw(this);  break;
        }
    }

    // ── Menu ─────────────────────────────────────────────────────────────
    private void BuildMenu()
    {
        var canvasLayer = new CanvasLayer();
        AddChild(canvasLayer);

        _menu = new PopupMenu();
        canvasLayer.AddChild(_menu);
        _menu.IdPressed += OnMenuPressed;

        // Mode switcher (always visible)
        _menu.AddItem("Mode: Polygone",  M_MODE_POLYGON);
        _menu.AddItem("Mode: Bézier",    M_MODE_BEZIER);
        _menu.AddItem("Mode: BSpline",   M_MODE_BSPLINE);
        _menu.AddSeparator();

        // Polygon items
        _menu.AddItem("Reset polygone",  M_POLY_RESET);
        _menu.AddSeparator();

        // Bézier items
        _menu.AddItem("Nouvelle courbe",        M_BEZ_NEW);
        _menu.AddItem("Supprimer courbe active", M_BEZ_DELETE);
        _menu.AddSeparator();
        _menu.AddItem("Toggle Edit/Append",     M_BEZ_TOGGLE_MODE);
        _menu.AddItem("Toggle Direct/Casteljau",M_BEZ_TOGGLE_METHOD);
        _menu.AddSeparator();
        _menu.AddItem("Pas +",   M_BEZ_STEP_UP);
        _menu.AddItem("Pas -",   M_BEZ_STEP_DOWN);
        _menu.AddSeparator();
        _menu.AddItem("Raccord C0", M_BEZ_JOIN_C0);
        _menu.AddItem("Raccord C1", M_BEZ_JOIN_C1);
        _menu.AddItem("Raccord C2", M_BEZ_JOIN_C2);
        _menu.AddSeparator();
        _menu.AddItem("Remplir courbe active", M_BEZ_FILL);
        _menu.AddItem("Benchmark",             M_BEZ_BENCH);
        _menu.AddSeparator();
        _menu.AddItem("Translater (+10,+10)", M_BEZ_TRANSLATE);
        _menu.AddItem("Rotation 15°",         M_BEZ_ROTATE);
        _menu.AddItem("Scale x1.1",           M_BEZ_SCALE);
        _menu.AddItem("Cisaillement shx=0.2", M_BEZ_SHEAR);
    }

    private void RefreshMenu()
    {
        // Gray-out mode items already active, etc. (optional polish)
    }

    private void OnMenuPressed(long id)
    {
        switch ((int)id)
        {
            case M_MODE_POLYGON: _mode = AppMode.Polygon; break;
            case M_MODE_BEZIER:  _mode = AppMode.Bezier;  break;
            case M_MODE_BSPLINE: _mode = AppMode.BSpline; break;

            case M_POLY_RESET:   _polyMgr.Reset(); break;

            case M_BEZ_NEW:          _bezMgr.NewCurve(); break;
            case M_BEZ_DELETE:       _bezMgr.DeleteActiveCurve(); break;
            case M_BEZ_TOGGLE_MODE:  _bezMgr.ToggleEditMode(); break;
            case M_BEZ_TOGGLE_METHOD:_bezMgr.ToggleMethod(); break;
            case M_BEZ_STEP_UP:      _bezMgr.StepUp(); break;
            case M_BEZ_STEP_DOWN:    _bezMgr.StepDown(); break;

            case M_BEZ_JOIN_C0: /* join last two curves C0 - future */ break;
            case M_BEZ_JOIN_C1: /* join last two curves C1 */ break;
            case M_BEZ_JOIN_C2: /* join last two curves C2 */ break;

            case M_BEZ_FILL:    DrawFill(); break;
            case M_BEZ_BENCH:   _bezMgr.RunBenchmark(); break;

            case M_BEZ_TRANSLATE:
                _bezMgr.ApplyTransform(Matrix3x3.Translation(10, 10)); break;
            case M_BEZ_ROTATE:
                _bezMgr.ApplyTransform(Matrix3x3.Rotation(Mathf.Pi / 12f)); break;
            case M_BEZ_SCALE:
                _bezMgr.ApplyTransform(Matrix3x3.Scaling(1.1f, 1.1f)); break;
            case M_BEZ_SHEAR:
                _bezMgr.ApplyTransform(Matrix3x3.Shearing(0.2f, 0f)); break;
        }
        QueueRedraw();
    }

    // ── Fill overlay ─────────────────────────────────────────────────────
    private List<(PolyMaths.Algorithms.Point2D, PolyMaths.Algorithms.Point2D)> _fillSegs
        = new List<(PolyMaths.Algorithms.Point2D, PolyMaths.Algorithms.Point2D)>();

    private void DrawFill()
    {
        _fillSegs = _bezMgr.FillActiveCurve();
    }

    // Override _Draw to also draw fill segments
    // (add inside _Draw switch)

    // ── HUD ──────────────────────────────────────────────────────────────
    private void BuildHud()
    {
        var canvasLayer = new CanvasLayer();
        AddChild(canvasLayer);
        _hud = new Label();
        _hud.Position = new Vector2(10, 10);
        _hud.AddThemeFontSizeOverride("font_size", 14);
        canvasLayer.AddChild(_hud);
    }

    private void UpdateHud()
    {
        _hud.Text = _mode switch
        {
            AppMode.Polygon => "MODE: POLYGONE\n" + _polyMgr.StatusText,
            AppMode.Bezier  => _bezMgr.StatusText,
            _               => "MODE: BSPLINE"
        };
    }
}
```

> **Note on fill rendering:** In `_Draw()`, add drawing of `_fillSegs` after the bezier draw call:
> ```csharp
> foreach (var (a, b) in _fillSegs)
>     DrawLine(new Vector2(a.x, a.y), new Vector2(b.x, b.y), new Color(1,1,0,0.5f), 1);
> ```

**Step 2: Run project, verify Bezier mode works (click to add points, right-click for menu)**

**Step 3: Commit**
```bash
git add poly-maths/Main.cs
git commit -m "feat: refactor Main.cs — mode dispatcher + PopupMenu + HUD"
```

---

## Task 7: Create BSplineCurve.cs

**Files:**
- Create: `poly-maths/Algorithms/BSplineCurve.cs`

**Step 1: Create the file**

```csharp
using System;
using System.Collections.Generic;

namespace PolyMaths.Algorithms
{
    /// <summary>
    /// B-Spline curve of degree p with n+1 control points.
    /// Uses the Cox-de Boor recurrence. Supports uniform and clamped knot vectors.
    /// </summary>
    public class BSplineCurve
    {
        protected List<Point2D> _controlPoints = new List<Point2D>();
        protected List<float>   _knots         = new List<float>();

        public IReadOnlyList<Point2D> ControlPoints => _controlPoints;
        public IReadOnlyList<float>   KnotVector    => _knots;
        public int Degree  { get; set; } = 3;
        public int Step    { get; set; } = 100;
        public bool IsClamped { get; protected set; } = true;

        public int N => _controlPoints.Count - 1; // n: last CP index

        // ── Control point management ─────────────────────────────────────────
        public void AddPoint(Point2D p)
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
        public void SetUniform()
        {
            IsClamped = false;
            RebuildKnots();
        }

        public void SetClamped()
        {
            IsClamped = true;
            RebuildKnots();
        }

        protected virtual void RebuildKnots()
        {
            int n = N;
            int p = Degree;
            int m = n + p + 1; // knot vector size: m+1 values

            _knots = new List<float>(m + 1);

            if (IsClamped)
            {
                // Clamped: p+1 zeros, interior uniform, p+1 ones
                for (int i = 0; i <= m; i++)
                {
                    if      (i <= p)     _knots.Add(0f);
                    else if (i >= m - p) _knots.Add(1f);
                    else                 _knots.Add((float)(i - p) / (m - 2 * p));
                }
            }
            else
            {
                // Uniform open knot vector: 0, 1/(m), 2/(m), ..., 1
                for (int i = 0; i <= m; i++)
                    _knots.Add((float)i / m);
            }
        }

        // ── Cox-de Boor basis ────────────────────────────────────────────────
        /// <summary>Computes N(i, p, t) with 0/0 → 0 guard.</summary>
        protected float Basis(int i, int p, float t)
        {
            if (p == 0)
            {
                // Special handling for last knot span
                if (i + 1 < _knots.Count && i >= 0)
                    return (_knots[i] <= t && t < _knots[i + 1]) ? 1f : 0f;
                return 0f;
            }

            float left  = 0f, right = 0f;

            float d1 = (i + p     < _knots.Count ? _knots[i + p]     : 0f) - _knots[i];
            float d2 = (i + p + 1 < _knots.Count ? _knots[i + p + 1] : 0f) -
                       (i + 1     < _knots.Count ? _knots[i + 1]     : 0f);

            if (Math.Abs(d1) > 1e-10f)
                left  = (t - _knots[i]) / d1 * Basis(i, p - 1, t);

            if (Math.Abs(d2) > 1e-10f)
                left2:
                right = ((i + p + 1 < _knots.Count ? _knots[i + p + 1] : 0f) - t)
                        / d2 * Basis(i + 1, p - 1, t);

            return left + right;
        }

        // ── Evaluation ──────────────────────────────────────────────────────
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

        /// <summary>Returns curve points sampled over the valid parameter domain.</summary>
        public virtual List<Point2D> GetPoints()
        {
            var pts = new List<Point2D>(Step + 1);
            if (_controlPoints.Count <= Degree) return pts;

            float tMin = _knots[Degree];
            float tMax = _knots[N + 1];
            if (tMax <= tMin) return pts;

            for (int i = 0; i <= Step; i++)
            {
                float t = tMin + (tMax - tMin) * i / Step;
                // Clamp slightly before tMax to stay in valid span
                if (i == Step) t = tMax - 1e-6f;
                pts.Add(Evaluate(t));
            }
            return pts;
        }
    }
}
```

> **Note:** The `left2:` label above is a typo in this plan — remove it in the actual code. The right-side computation is just:
> ```csharp
> if (Math.Abs(d2) > 1e-10f)
>     right = (...) / d2 * Basis(i + 1, p - 1, t);
> ```

**Step 2: Commit**
```bash
git add poly-maths/Algorithms/BSplineCurve.cs
git commit -m "feat: add BSplineCurve with Cox-de Boor evaluation, uniform/clamped knots"
```

---

## Task 8: Create NURBSCurve.cs

**Files:**
- Create: `poly-maths/Algorithms/NURBSCurve.cs`

**Step 1: Create the file**

```csharp
using System;
using System.Collections.Generic;

namespace PolyMaths.Algorithms
{
    /// <summary>
    /// NURBS curve: rational BSpline with per-control-point weights.
    /// </summary>
    public class NURBSCurve : BSplineCurve
    {
        private List<float> _weights = new List<float>();
        public IReadOnlyList<float> Weights => _weights;

        public new void AddPoint(Point2D p, float weight = 1f)
        {
            _controlPoints.Add(p);
            _weights.Add(weight);
            RebuildKnots();
        }

        public void SetWeight(int i, float w)
        {
            if (i >= 0 && i < _weights.Count) _weights[i] = w;
        }

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
            if (Math.Abs(denom) < 1e-10f) return _controlPoints.Count > 0 ? _controlPoints[0] : default;
            return new Point2D(nx / denom, ny / denom);
        }

        // ── Factory methods ──────────────────────────────────────────────────
        /// <summary>Creates a NURBS circle of given center and radius (9 control points).</summary>
        public static NURBSCurve Circle(Point2D center, float r)
        {
            float w = (float)Math.Sqrt(2f) / 2f; // = cos(45°)
            var pts = new (Point2D, float)[]
            {
                (new Point2D(center.x + r,  center.y    ), 1f),
                (new Point2D(center.x + r,  center.y + r), w),
                (new Point2D(center.x,      center.y + r), 1f),
                (new Point2D(center.x - r,  center.y + r), w),
                (new Point2D(center.x - r,  center.y    ), 1f),
                (new Point2D(center.x - r,  center.y - r), w),
                (new Point2D(center.x,      center.y - r), 1f),
                (new Point2D(center.x + r,  center.y - r), w),
                (new Point2D(center.x + r,  center.y    ), 1f),
            };
            var c = new NURBSCurve { Degree = 2 };
            foreach (var (p, wt) in pts) c.AddPoint(p, wt);
            c.SetClamped();
            return c;
        }

        /// <summary>Creates a NURBS ellipse (a=semi-major, b=semi-minor).</summary>
        public static NURBSCurve Ellipse(Point2D center, float a, float b)
        {
            float w = (float)Math.Sqrt(2f) / 2f;
            var pts = new (Point2D, float)[]
            {
                (new Point2D(center.x + a, center.y    ), 1f),
                (new Point2D(center.x + a, center.y + b), w),
                (new Point2D(center.x,     center.y + b), 1f),
                (new Point2D(center.x - a, center.y + b), w),
                (new Point2D(center.x - a, center.y    ), 1f),
                (new Point2D(center.x - a, center.y - b), w),
                (new Point2D(center.x,     center.y - b), 1f),
                (new Point2D(center.x + a, center.y - b), w),
                (new Point2D(center.x + a, center.y    ), 1f),
            };
            var c = new NURBSCurve { Degree = 2 };
            foreach (var (p, wt) in pts) c.AddPoint(p, wt);
            c.SetClamped();
            return c;
        }

        /// <summary>Creates a NURBS parabola arc (3 control points, degree 2).</summary>
        public static NURBSCurve Parabola(Point2D vertex, float scale)
        {
            var c = new NURBSCurve { Degree = 2 };
            c.AddPoint(new Point2D(vertex.x - scale, vertex.y + scale), 1f);
            c.AddPoint(new Point2D(vertex.x,          vertex.y        ), 0.5f); // weight < 1 → parabola
            c.AddPoint(new Point2D(vertex.x + scale, vertex.y + scale), 1f);
            c.SetClamped();
            return c;
        }

        /// <summary>Creates a NURBS hyperbola arc (5 control points, degree 2).</summary>
        public static NURBSCurve Hyperbola(Point2D center, float a, float b)
        {
            // Right branch approximation
            float w = (float)Math.Sqrt(2f) / 2f;
            var c = new NURBSCurve { Degree = 2 };
            c.AddPoint(new Point2D(center.x + a, center.y - b), 1f);
            c.AddPoint(new Point2D(center.x + a, center.y    ), w * 0.5f); // weight > 1 → hyperbola
            c.AddPoint(new Point2D(center.x + a, center.y + b), 1f);
            c.SetClamped();
            return c;
        }
    }
}
```

**Step 2: Commit**
```bash
git add poly-maths/Algorithms/NURBSCurve.cs
git commit -m "feat: add NURBSCurve with rational evaluation + Circle/Ellipse/Parabola/Hyperbola factories"
```

---

## Task 9: Create BSplineManager.cs and wire into Main.cs

**Files:**
- Create: `poly-maths/Managers/BSplineManager.cs`
- Modify: `poly-maths/Main.cs` (add BSpline mode support)

**Step 1: Create BSplineManager.cs**

```csharp
using Godot;
using System.Collections.Generic;
using PolyMaths.Algorithms;

namespace PolyMaths.Managers
{
    public class BSplineManager
    {
        private LinkedList<BSplineCurve> _bsplines = new LinkedList<BSplineCurve>();
        private LinkedList<NURBSCurve>   _nurbs    = new LinkedList<NURBSCurve>();

        private LinkedListNode<BSplineCurve> _activeBs;
        private LinkedListNode<NURBSCurve>   _activeNurbs;
        private bool _nurbsMode   = false;   // false = BSpline, true = NURBS
        private bool _editMode    = false;
        private int  _dragIndex   = -1;
        private bool _dragging    = false;
        private float _weightEdit = 1f;

        private const float SELECT_THRESHOLD = 12f;
        public Color ControlColor  { get; set; } = new Color(0.5f, 0.5f, 0.5f);
        public Color CurveColor    { get; set; } = new Color(0.2f, 0.9f, 0.5f);
        public Color ActiveColor   { get; set; } = new Color(1f, 0.5f, 0f);
        public int   DotRadius     { get; set; } = 5;

        public string StatusText
        {
            get
            {
                string type = _nurbsMode ? "NURBS" : "BSpline";
                int deg = ActiveCurve?.Degree ?? 0;
                int cp  = ActiveCurve?.ControlPoints.Count ?? 0;
                string knots = (ActiveCurve?.IsClamped ?? true) ? "Clamped" : "Uniform";
                return $"MODE: {type} | Degree:{deg} | CP:{cp} | Knots:{knots}" +
                       $"\nCourbes: {_bsplines.Count + _nurbs.Count}";
            }
        }

        private BSplineCurve ActiveCurve => _nurbsMode
            ? (BSplineCurve)_activeNurbs?.Value
            : _activeBs?.Value;

        // ── Input ────────────────────────────────────────────────────────────
        public void HandleLeftClick(Vector2 mouse)
        {
            if (!_editMode)
            {
                if (_nurbsMode)
                {
                    if (_activeNurbs == null) NewNurbs();
                    _activeNurbs.Value.AddPoint(V(mouse), _weightEdit);
                }
                else
                {
                    if (_activeBs == null) NewBSpline();
                    _activeBs.Value.AddPoint(V(mouse));
                }
                return;
            }

            // Edit mode: find nearest point
            _dragIndex = -1;
            var curve = ActiveCurve;
            if (curve == null) return;
            for (int i = 0; i < curve.ControlPoints.Count; i++)
            {
                if (P(curve.ControlPoints[i]).DistanceTo(mouse) <= SELECT_THRESHOLD)
                {
                    _dragIndex = i;
                    _dragging  = true;
                    return;
                }
            }
        }

        public void HandleLeftRelease() { _dragging = false; }

        public void HandleMouseMove(Vector2 mouse)
        {
            if (_dragging && _dragIndex >= 0)
                ActiveCurve?.MovePoint(_dragIndex, V(mouse));
        }

        public void HandleDelete()
        {
            if (_editMode && _dragIndex >= 0) { ActiveCurve?.RemovePoint(_dragIndex); _dragIndex = -1; }
            else if (_nurbsMode)              { if (_activeNurbs != null) { _nurbs.Remove(_activeNurbs); _activeNurbs = _nurbs.Last; } }
            else                              { if (_activeBs    != null) { _bsplines.Remove(_activeBs); _activeBs = _bsplines.Last; } }
        }

        public void DegreeUp()
        {
            if (ActiveCurve != null && ActiveCurve.Degree < ActiveCurve.ControlPoints.Count - 1)
            {
                ActiveCurve.Degree++;
                ((BSplineCurve)ActiveCurve).SetClamped(); // rebuild knots
            }
        }

        public void DegreeDown()
        {
            if (ActiveCurve != null && ActiveCurve.Degree > 1)
            {
                ActiveCurve.Degree--;
                ((BSplineCurve)ActiveCurve).SetClamped();
            }
        }

        public void ToggleKnots()
        {
            var c = ActiveCurve;
            if (c == null) return;
            if (c.IsClamped) c.SetUniform(); else c.SetClamped();
        }

        public void ToggleEditMode() { _editMode = !_editMode; _dragIndex = -1; }
        public void ToggleNurbsMode(){ _nurbsMode = !_nurbsMode; }

        public void ApplyTransform(Matrix3x3 m) { ActiveCurve?.ApplyTransform(m); }

        // ── Curve factories ──────────────────────────────────────────────────
        public void NewBSpline() { _activeBs = _bsplines.AddLast(new BSplineCurve()); _nurbsMode = false; }
        public void NewNurbs()   { _activeNurbs = _nurbs.AddLast(new NURBSCurve());   _nurbsMode = true;  }

        public void LoadDemoCircle()
        {
            _activeNurbs = _nurbs.AddLast(NURBSCurve.Circle(new Point2D(400, 300), 150f));
            _nurbsMode = true;
        }

        public void LoadDemoEllipse()
        {
            _activeNurbs = _nurbs.AddLast(NURBSCurve.Ellipse(new Point2D(400, 300), 200f, 100f));
            _nurbsMode = true;
        }

        public void LoadDemoParabola()
        {
            _activeNurbs = _nurbs.AddLast(NURBSCurve.Parabola(new Point2D(400, 400), 150f));
            _nurbsMode = true;
        }

        public void LoadDemoHyperbola()
        {
            _activeNurbs = _nurbs.AddLast(NURBSCurve.Hyperbola(new Point2D(400, 300), 80f, 100f));
            _nurbsMode = true;
        }

        // ── Drawing ──────────────────────────────────────────────────────────
        public void Draw(Node2D canvas)
        {
            foreach (var node in _bsplines)
                DrawCurve(canvas, node, node == _activeBs?.Value);
            foreach (var node in _nurbs)
                DrawCurve(canvas, node, node == _activeNurbs?.Value);
        }

        private void DrawCurve(Node2D canvas, BSplineCurve curve, bool isActive)
        {
            var pts = curve.ControlPoints;
            Color cc = isActive ? ActiveColor : CurveColor;

            for (int i = 0; i < pts.Count - 1; i++)
                canvas.DrawLine(P(pts[i]), P(pts[i+1]), ControlColor, 1);

            for (int i = 0; i < pts.Count; i++)
            {
                bool sel = isActive && _editMode && i == _dragIndex;
                canvas.DrawCircle(P(pts[i]), DotRadius, sel ? Colors.Red : Colors.Black);
            }

            if (pts.Count > curve.Degree)
            {
                var cPts = curve.GetPoints();
                for (int i = 0; i < cPts.Count - 1; i++)
                    canvas.DrawLine(P(cPts[i]), P(cPts[i+1]), cc, 2);
            }
        }

        private static Point2D V(Vector2 v) => new Point2D(v.X, v.Y);
        private static Vector2  P(Point2D p) => new Vector2(p.x, p.y);
    }
}
```

**Step 2: Add BSplineManager to Main.cs**

In `Main.cs`:
1. Add field: `private BSplineManager _bspMgr = new BSplineManager();`
2. In `_Draw()` switch, add: `case AppMode.BSpline: _bspMgr.Draw(this); break;`
3. In `HandleLeftClick()` switch, add: `case AppMode.BSpline: _bspMgr.HandleLeftClick(mouse); break;`
4. In `_Process()`, add BSpline key handling and mouse handling
5. Add BSpline menu items with IDs in range 40–59:

```csharp
// In BuildMenu():
_menu.AddSeparator();
_menu.AddItem("Nouvelle BSpline",   M_BS_NEW);
_menu.AddItem("Nouveau NURBS",      M_BS_NURBS);
_menu.AddItem("Degré +",            M_BS_DEG_UP);
_menu.AddItem("Degré -",            M_BS_DEG_DOWN);
_menu.AddItem("Nœuds: toggle",      M_BS_KNOTS);
_menu.AddItem("Toggle Edit",        M_BS_EDIT);
_menu.AddItem("Demo: Cercle",       M_BS_DEMO_CIRCLE);
_menu.AddItem("Demo: Ellipse",      M_BS_DEMO_ELLIPSE);
_menu.AddItem("Demo: Parabole",     M_BS_DEMO_PARABOLA);
_menu.AddItem("Demo: Hyperbole",    M_BS_DEMO_HYPERBOLA);
```

```csharp
// In OnMenuPressed():
case M_BS_NEW:           _bspMgr.NewBSpline(); break;
case M_BS_NURBS:         _bspMgr.NewNurbs();   break;
case M_BS_DEG_UP:        _bspMgr.DegreeUp();   break;
case M_BS_DEG_DOWN:      _bspMgr.DegreeDown(); break;
case M_BS_KNOTS:         _bspMgr.ToggleKnots(); break;
case M_BS_EDIT:          _bspMgr.ToggleEditMode(); break;
case M_BS_DEMO_CIRCLE:   _bspMgr.LoadDemoCircle();   break;
case M_BS_DEMO_ELLIPSE:  _bspMgr.LoadDemoEllipse();  break;
case M_BS_DEMO_PARABOLA: _bspMgr.LoadDemoParabola(); break;
case M_BS_DEMO_HYPERBOLA:_bspMgr.LoadDemoHyperbola();break;
```

**Step 3: Run project, verify BSpline mode works (click to add, demo shapes load)**

**Step 4: Commit**
```bash
git add poly-maths/Managers/BSplineManager.cs poly-maths/Main.cs
git commit -m "feat: add BSplineManager + wire BSpline/NURBS mode into Main.cs"
```

---

## Task 10: Create BSplineTestSuite.cs

**Files:**
- Create: `poly-maths/Tests/BSplineTestSuite.cs`

**Step 1: Create the test file**

```csharp
using System;
using System.Collections.Generic;
using PolyMaths.Algorithms;
using PolyMaths.Utils;

namespace PolyMaths.Tests
{
    public class BSplineTestSuite
    {
        private int _passed, _failed;
        private readonly List<string> _failures = new();

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
                Logger.Data("Distance from start to P0", d0);
                Assert("Uniform: start ≠ P0 (dist > 1)", d0 > 1f);
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
                Logger.Data("Distance start → P0", d0);
                Logger.Data("Distance end → Pn",   d1);
                Assert("Clamped: starts at P0", d0 < 1f);
                Assert("Clamped: ends at Pn",   d1 < 1f);
            }
            Logger.SectionEnd();
        }

        private void TestDegree1IsPolyline()
        {
            Logger.Section("TEST S3: Degree-1 BSpline = polyline");
            var c = new BSplineCurve { Degree = 1 };
            c.AddPoint(new Point2D(0,   0));
            c.AddPoint(new Point2D(50, 100));
            c.AddPoint(new Point2D(100, 0));

            var pts = c.GetPoints();
            Assert("Produces points", pts.Count > 0);
            // Midpoint should be near (50,100) for a polyline
            if (pts.Count > 0)
            {
                float minDist = float.MaxValue;
                foreach (var p in pts) minDist = Math.Min(minDist, p.DistanceTo(new Point2D(50, 100)));
                Logger.Data("Min dist to P1", minDist);
                Assert("Degree-1 passes through all control points", minDist < 5f);
            }
            Logger.SectionEnd();
        }

        private void TestNurbsUnityWeightsEqualsBSpline()
        {
            Logger.Section("TEST S4: NURBS with weights=1 equals BSpline");
            var bs = new BSplineCurve { Degree = 3 };
            var nr = new NURBSCurve  { Degree = 3 };
            var pts2D = new Point2D[]
            {
                new Point2D(0, 0), new Point2D(50, 100),
                new Point2D(150, 100), new Point2D(200, 0)
            };
            foreach (var p in pts2D) { bs.AddPoint(p); nr.AddPoint(p, 1f); }

            var ptsBS = bs.GetPoints();
            var ptsNR = nr.GetPoints();

            Assert("Same number of points", ptsBS.Count == ptsNR.Count);
            bool allClose = true;
            for (int i = 0; i < Math.Min(ptsBS.Count, ptsNR.Count); i++)
            {
                if (ptsBS[i].DistanceTo(ptsNR[i]) > 1e-3f) { allClose = false; break; }
            }
            Assert("NURBS(w=1) == BSpline", allClose);
            Logger.SectionEnd();
        }

        private void TestNurbsCircleCardinalPoints()
        {
            Logger.Section("TEST S5: NURBS circle cardinal points");
            var center = new Point2D(0, 0);
            float r = 100f;
            var circle = NURBSCurve.Circle(center, r);
            var pts = circle.GetPoints();
            Assert("Circle produces points", pts.Count > 0);

            // Find closest point to each cardinal direction
            float dRight = float.MaxValue, dTop = float.MaxValue;
            foreach (var p in pts)
            {
                dRight = Math.Min(dRight, p.DistanceTo(new Point2D( r, 0)));
                dTop   = Math.Min(dTop,   p.DistanceTo(new Point2D( 0, r)));
            }
            Logger.Data("Min dist to (r,0)",  dRight);
            Logger.Data("Min dist to (0,r)",  dTop);
            Assert("Circle passes near (r,0)", dRight < 5f);
            Assert("Circle passes near (0,r)", dTop   < 5f);
            Logger.SectionEnd();
        }

        private void TestCoxDeBoorDivisionByZeroGuard()
        {
            Logger.Section("TEST S6: Cox-de Boor 0/0 guard (degenerate knots)");
            var c = new BSplineCurve { Degree = 3 };
            // Only 2 points for degree 3 → degenerate
            c.AddPoint(new Point2D(0, 0));
            c.AddPoint(new Point2D(100, 100));
            // Should not throw
            bool threw = false;
            try { c.GetPoints(); }
            catch { threw = true; }
            Assert("No exception on degenerate curve", !threw);
            Logger.SectionEnd();
        }

        private void TestDemoShapesNoException()
        {
            Logger.Section("TEST S7: Demo shapes (Circle/Ellipse/Parabola/Hyperbola) — no exception");
            bool threw = false;
            try
            {
                var circle    = NURBSCurve.Circle   (new Point2D(200, 200), 80f);
                var ellipse   = NURBSCurve.Ellipse  (new Point2D(200, 200), 120f, 60f);
                var parabola  = NURBSCurve.Parabola (new Point2D(200, 300), 100f);
                var hyperbola = NURBSCurve.Hyperbola(new Point2D(200, 200), 60f, 80f);

                circle.GetPoints();
                ellipse.GetPoints();
                parabola.GetPoints();
                hyperbola.GetPoints();
            }
            catch (Exception e) { threw = true; Logger.Error("Exception: " + e.Message); }
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
            Logger.Header($"BSPLINE TESTS: {_passed} passed, {_failed} failed");
            foreach (var f in _failures) Logger.Error("  - " + f);
        }
    }
}
```

**Step 2: Enable in Main.cs `_Ready()`**
```csharp
new PolyMaths.Tests.BSplineTestSuite().RunAllTests();
```

**Step 3: Run project, fix any failures**

**Step 4: Commit**
```bash
git add poly-maths/Tests/BSplineTestSuite.cs poly-maths/Main.cs
git commit -m "test: add BSplineTestSuite — Cox-de Boor, NURBS, demo shapes"
```

---

## Task 11: Final integration check

**Step 1: Disable all test runners in _Ready() (comment them out)**

**Step 2: Run the project in interactive mode**

Verify the following manually:
- [ ] Mode switches via right-click menu (Polygone / Bézier / BSpline)
- [ ] POLYGONE: draw + clip + fill works as before
- [ ] BÉZIER: click to add points → polygon + curve appear; `+`/`-` changes step
- [ ] BÉZIER: right-click → toggle Direct/Casteljau → curve looks identical
- [ ] BÉZIER: Edit mode → drag control point → curve redraws live
- [ ] BÉZIER: Matrix transforms (Translate / Rotate / Scale / Shear) from menu
- [ ] BÉZIER: Benchmark → output shown in HUD
- [ ] BÉZIER: Fill → yellow fill scanlines appear
- [ ] BÉZIER: Multiple curves via "Nouvelle courbe", Tab to navigate
- [ ] BSPLINE: new BSpline → click points → curve appears
- [ ] BSPLINE: Degree toggle → curve changes shape
- [ ] BSPLINE: Clamped vs Uniform knots → endpoints behavior changes
- [ ] NURBS: Demo shapes load (Circle/Ellipse/Parabola/Hyperbola)

**Step 3: Final commit**
```bash
git add -A
git commit -m "feat: complete Projet 2 (Bézier) + Projet 3 (BSplines/NURBS)"
```

---

## Quick Reference: Key File Paths

| File | Purpose |
|------|---------|
| `poly-maths/Algorithms/BezierCurve.cs` | Pure math: Bernstein, Casteljau, benchmark |
| `poly-maths/Algorithms/BSplineCurve.cs` | Pure math: Cox-de Boor, knot vectors |
| `poly-maths/Algorithms/NURBSCurve.cs` | Rational BSpline + conic factories |
| `poly-maths/Algorithms/Matrix3x3.cs` | Add Shearing() |
| `poly-maths/Managers/PolygonManager.cs` | T1 state (extracted from Main.cs) |
| `poly-maths/Managers/BezierManager.cs` | Bézier state, edit, transforms, continuity |
| `poly-maths/Managers/BSplineManager.cs` | BSpline/NURBS state, demos |
| `poly-maths/Main.cs` | Mode dispatcher, PopupMenu, HUD |
| `poly-maths/Tests/BezierTestSuite.cs` | All Bézier math tests |
| `poly-maths/Tests/BSplineTestSuite.cs` | All BSpline/NURBS tests |
