using Godot;
using System;
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
        private bool _showPascal    = true;
        private bool _showCasteljau = false;
        public Color PascalColor     { get; set; } = new Color(0.2f, 0.6f, 1f);    // blue
        public Color CasteljauColor  { get; set; } = new Color(1f, 0.35f, 0.1f);   // orange-red
        private string _benchText    = "";

        // ── Multi-selection ──────────────────────────────────────────────────
        private HashSet<BezierCurve> _multiSelected = new HashSet<BezierCurve>();
        public int MultiSelectedCount => _multiSelected.Count;

        // ── Colors ───────────────────────────────────────────────────────────
        public Color ControlPolygonColor { get; set; } = new Color(0.5f, 0.5f, 0.5f);
        public Color CurveColor          { get; set; } = new Color(0.2f, 0.6f, 1f);
        public Color ActiveCurveColor    { get; set; } = new Color(1f, 0.3f, 0.3f);
        public Color MarkedCurveColor    { get; set; } = new Color(1f, 0.7f, 0f);   // orange
        public Color SelectedPointColor  { get; set; } = Colors.Red;
        public Color FillColor           { get; set; } = new Color(1f, 1f, 0f, 0.4f);
        public int   DotRadius           { get; set; } = 5;
        private const float SELECT_THRESHOLD = 12f;

        // ── Status ───────────────────────────────────────────────────────────
        public string StatusText
        {
            get
            {
                string algos = (_showPascal    ? "Pascal "   : "")
                             + (_showCasteljau ? "Casteljau" : "");
                if (algos == "") algos = "(aucun)";
                string mode   = _editMode ? "ÉDITION" : "AJOUT";
                int step = _activeNode?.Value.Step ?? 0;
                int curveCount = _curves.Count;
                string mark = _multiSelected.Count > 0
                    ? string.Format("  [Marquées:{0}]", _multiSelected.Count) : "";
                return string.Format(
                    "BEZIER | Mode:{0} | Algos:{1} | Pas:{2} | Courbes:{3}{4}\n{5}\n{6}",
                    mode, algos, step, curveCount, mark,
                    _editMode ? "ClicG=sélect/insérer  DblClic=suppr pt  Glisser=déplacer  Suppr=retirer  [Espace]=marquer"
                              : "ClicG=ajouter  ClicD=menu  [↑↓←→] Trans  [R] Rot  [S] Éch  [H] Cis  [Espace]=marquer",
                    _benchText);
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

            // Edit mode — priority 1: select nearest existing control point
            _dragIndex = -1;
            _dragging  = false;
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

            // Edit mode — priority 2: click on a control-polygon edge → insert point there
            node = _curves.First;
            while (node != null)
            {
                var pts = node.Value.ControlPoints;
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    if (DistancePointToSegment(mouse, P(pts[i]), P(pts[i + 1])) <= SELECT_THRESHOLD)
                    {
                        _activeNode = node;
                        node.Value.InsertPoint(i + 1, V(mouse));
                        _dragIndex = i + 1;
                        _dragging  = true;
                        return;
                    }
                }
                node = node.Next;
            }
        }

        /// <summary>Double-click in edit mode: delete the point under the cursor.</summary>
        public void HandleDoubleClick(Vector2 mouse)
        {
            if (!_editMode) return;
            var node = _curves.First;
            while (node != null)
            {
                for (int i = 0; i < node.Value.ControlPoints.Count; i++)
                {
                    if (P(node.Value.ControlPoints[i]).DistanceTo(mouse) <= SELECT_THRESHOLD)
                    {
                        _activeNode = node;
                        node.Value.RemovePoint(i);
                        _dragIndex = -1;
                        _dragging  = false;
                        return;
                    }
                }
                node = node.Next;
            }
        }

        public void HandleLeftRelease() { _dragging = false; }

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
                _dragging  = false;
            }
            else
            {
                DeleteActiveCurve();
            }
        }

        public void StepUp()
        {
            if (_activeNode != null) _activeNode.Value.Step++;
        }

        public void StepDown()
        {
            if (_activeNode != null && _activeNode.Value.Step > 2)
                _activeNode.Value.Step--;
        }

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
            _multiSelected.Remove(_activeNode.Value);
            var next = _activeNode.Next ?? _activeNode.Previous;
            _curves.Remove(_activeNode);
            _activeNode = next;
        }

        /// <summary>Toggle the active curve in/out of the multi-selection set.</summary>
        public void ToggleActiveInSelection()
        {
            if (_activeNode == null) return;
            var curve = _activeNode.Value;
            if (!_multiSelected.Add(curve))   // Add() returns false when already present
                _multiSelected.Remove(curve);
        }

        /// <summary>Delete all marked curves. Falls back to deleting the active curve if none are marked.</summary>
        public void DeleteMarked()
        {
            if (_multiSelected.Count == 0) { DeleteActiveCurve(); return; }

            // Find a replacement active node that is not in the marked set
            LinkedListNode<BezierCurve> newActive = null;
            for (var n = _curves.First; n != null; n = n.Next)
                if (!_multiSelected.Contains(n.Value)) { newActive = n; break; }

            // Remove all marked nodes
            var node = _curves.First;
            while (node != null)
            {
                var next = node.Next;
                if (_multiSelected.Contains(node.Value)) _curves.Remove(node);
                node = next;
            }
            _multiSelected.Clear();
            _activeNode = newActive;
        }

        /// <summary>Delete every curve.</summary>
        public void DeleteAll()
        {
            _curves.Clear();
            _activeNode = null;
            _multiSelected.Clear();
        }

        /// <summary>
        /// Creates a new Bézier curve with 50 control points in a 3-cycle sine wave.
        /// Demonstrates Runge's oscillation phenomenon on high-degree polynomials.
        /// </summary>
        public void LoadDemoSine50()
        {
            var curve = new BezierCurve();
            for (int i = 0; i < 50; i++)
            {
                float t = i / 49f;
                float x = 80f  + i * 33f;
                float y = 420f + 170f * (float)Math.Sin(t * 6 * Math.PI);
                curve.AddPoint(new Point2D(x, y));
            }
            _activeNode = _curves.AddLast(curve);
            _editMode   = false;
        }

        public void SelectNext()
        {
            if (_activeNode?.Next != null) _activeNode = _activeNode.Next;
        }

        public void SelectPrev()
        {
            if (_activeNode?.Previous != null) _activeNode = _activeNode.Previous;
        }

        public void ToggleShowPascal()    { _showPascal    = !_showPascal;    _benchText = ""; }
        public void ToggleShowCasteljau() { _showCasteljau = !_showCasteljau; _benchText = ""; }
        public void ToggleEditMode() { _editMode = !_editMode; _dragIndex = -1; _dragging = false; }

        // ── Transforms ───────────────────────────────────────────────────────
        public void ApplyTransform(Matrix3x3 m)
        {
            _activeNode?.Value.ApplyTransform(m);
        }

        public void Translate(float dx, float dy)
            => _activeNode?.Value.ApplyTransform(Matrix3x3.Translation(dx, dy));

        public void Rotate(float angle)
        {
            if (_activeNode == null) return;
            var p = GetPivot();
            var m = Matrix3x3.Translation(p.x, p.y)
                  * Matrix3x3.Rotation(angle)
                  * Matrix3x3.Translation(-p.x, -p.y);
            _activeNode.Value.ApplyTransform(m);
        }

        public void Scale(float factor)
        {
            if (_activeNode == null) return;
            var p = GetPivot();
            var m = Matrix3x3.Translation(p.x, p.y)
                  * Matrix3x3.Scaling(factor, factor)
                  * Matrix3x3.Translation(-p.x, -p.y);
            _activeNode.Value.ApplyTransform(m);
        }

        public void ShearH(float delta) => _activeNode?.Value.ApplyTransform(Matrix3x3.Shearing(delta, 0f));
        public void ShearV(float delta) => _activeNode?.Value.ApplyTransform(Matrix3x3.Shearing(0f, delta));

        // ── Continuity joining ───────────────────────────────────────────────
        /// <summary>
        /// Adjusts the first control points of the last curve to achieve C0/C1/C2
        /// continuity with the second-to-last curve.
        /// </summary>
        public void JoinLastTwo(Continuity cont)
        {
            if (_curves.Count < 2) return;
            var nodeB = _curves.Last;
            var nodeA = nodeB.Previous;
            Join(nodeA.Value, nodeB.Value, cont);
        }

        public static void Join(BezierCurve a, BezierCurve b, Continuity cont)
        {
            if (a == null || b == null) return;
            if (a.ControlPoints.Count < 2 || b.ControlPoints.Count < 2) return;

            int n     = a.Degree;
            var Pn    = a.ControlPoints[n];
            var Pn_1  = a.ControlPoints[n - 1];

            // C0: B.P0 = A.Pn
            b.MovePoint(0, Pn);
            if (cont == Continuity.C0) return;

            // C1: B.P1 = 2*Pn - P(n-1)
            if (b.ControlPoints.Count < 2) return;
            var c1P1 = Pn * 2f - Pn_1;
            b.MovePoint(1, c1P1);
            if (cont == Continuity.C1) return;

            // C2: B.P2 based on second derivative continuity
            if (b.ControlPoints.Count < 3 || n < 2) return;
            var Pn_2 = a.ControlPoints[n - 2];
            // Formula: B.P2 = 2*c1P1 - ((n-1)/n * Pn + 1/n * Pn_1)
            var c2P2 = c1P1 * 2f - (Pn * ((float)(n - 1) / n) + Pn_1 * (1f / n));
            b.MovePoint(2, c2P2);
        }

        // ── Benchmark ────────────────────────────────────────────────────────
        public void RunBenchmark()
        {
            if (_activeNode == null) return;
            var (d, k) = _activeNode.Value.BenchmarkBoth(10000);
            _benchText = string.Format("Benchmark: Direct={0}ms  Casteljau={1}ms", d, k);
        }

        // ── Fill ─────────────────────────────────────────────────────────────
        /// <summary>Toggle scanline fill on the active curve.</summary>
        public void FillActiveCurve()
        {
            if (_activeNode == null) return;
            _activeNode.Value.FillEnabled = !_activeNode.Value.FillEnabled;
        }

        /// <summary>Toggle fill on every marked curve (or active if none marked).</summary>
        public void FillMarked()
        {
            if (_multiSelected.Count == 0) { FillActiveCurve(); return; }
            // Use the first marked curve's current state to decide direction (toggle all to opposite)
            bool target = false;
            foreach (var c in _multiSelected) { target = !c.FillEnabled; break; }
            foreach (var c in _multiSelected) c.FillEnabled = target;
        }

        /// <summary>Toggle fill on every curve.</summary>
        public void FillAll()
        {
            // If any curve has fill off, turn all on; otherwise turn all off
            bool anyOff = false;
            for (var n = _curves.First; n != null; n = n.Next)
                if (!n.Value.FillEnabled) { anyOff = true; break; }
            for (var n = _curves.First; n != null; n = n.Next)
                n.Value.FillEnabled = anyOff;
        }

        // ── Drawing ──────────────────────────────────────────────────────────
        public void Draw(Node2D canvas)
        {
            var node = _curves.First;
            while (node != null)
            {
                DrawCurve(canvas, node.Value, node == _activeNode, _multiSelected.Contains(node.Value));
                node = node.Next;
            }
        }

        private void DrawCurve(Node2D canvas, BezierCurve curve, bool isActive, bool isMarked)
        {
            var pts = curve.ControlPoints;

            // Pascal and/or Casteljau evaluation
            var pascalPts    = (pts.Count >= 2 && _showPascal)    ? curve.GetPoints(false) : null;
            var casteljauPts = (pts.Count >= 2 && _showCasteljau) ? curve.GetPoints(true)  : null;
            // curvePts used for fill: prefer Pascal; fallback to Casteljau
            var curvePts = pascalPts ?? casteljauPts;

            // Scanline fill — recomputed each frame so it tracks control-point drags
            if (curve.FillEnabled && curvePts != null && curvePts.Count >= 3)
            {
                var segs = new LCAFill().FillPolygon(new Polygon(curvePts));
                foreach (var s in segs)
                    canvas.DrawLine(P(s.Item1), P(s.Item2), FillColor, 1);
            }

            // Control polygon (thin gray lines)
            for (int i = 0; i < pts.Count - 1; i++)
                canvas.DrawLine(P(pts[i]), P(pts[i + 1]), ControlPolygonColor, 1);

            // Control point dots
            for (int i = 0; i < pts.Count; i++)
            {
                bool sel = isActive && _editMode && i == _dragIndex;
                canvas.DrawCircle(P(pts[i]), DotRadius, sel ? SelectedPointColor : Colors.Black);
            }

            // Pivot indicator (white ring) in edit mode
            if (isActive && _editMode)
            {
                var pivot = GetPivot();
                canvas.DrawCircle(P(pivot), DotRadius + 4, Colors.White);
                canvas.DrawCircle(P(pivot), DotRadius + 2, Colors.Black);
            }

            // Pascal curve
            if (pascalPts != null)
                for (int i = 0; i < pascalPts.Count - 1; i++)
                    canvas.DrawLine(P(pascalPts[i]), P(pascalPts[i + 1]), PascalColor, 2);

            // Casteljau curve
            if (casteljauPts != null)
                for (int i = 0; i < casteljauPts.Count - 1; i++)
                    canvas.DrawLine(P(casteljauPts[i]), P(casteljauPts[i + 1]), CasteljauColor, 2);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static Point2D V(Vector2 v) => new Point2D(v.X, v.Y);
        private static Vector2  P(Point2D p) => new Vector2(p.x, p.y);

        /// <summary>Shortest distance from point <paramref name="p"/> to segment [a,b].</summary>
        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.LengthSquared();
            if (len2 < 1e-6f) return p.DistanceTo(a);
            float t = Mathf.Clamp((p - a).Dot(ab) / len2, 0f, 1f);
            return p.DistanceTo(a + ab * t);
        }

        /// <summary>
        /// Returns the current rotation/scale pivot: the selected control point if one
        /// is active in edit mode, or the centroid of all control points otherwise.
        /// </summary>
        public Point2D GetPivot()
        {
            if (_activeNode == null) return default;
            var pts = _activeNode.Value.ControlPoints;
            if (pts.Count == 0) return default;

            if (_editMode && _dragIndex >= 0 && _dragIndex < pts.Count)
                return pts[_dragIndex];

            float cx = 0, cy = 0;
            foreach (var p in pts) { cx += p.x; cy += p.y; }
            return new Point2D(cx / pts.Count, cy / pts.Count);
        }
    }
}
