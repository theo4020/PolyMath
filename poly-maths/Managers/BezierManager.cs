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

        // ── Fill state ───────────────────────────────────────────────────────
        private List<(Point2D, Point2D)> _fillSegs = new List<(Point2D, Point2D)>();

        // ── Colors ───────────────────────────────────────────────────────────
        public Color ControlPolygonColor { get; set; } = new Color(0.5f, 0.5f, 0.5f);
        public Color CurveColor          { get; set; } = new Color(0.2f, 0.6f, 1f);
        public Color ActiveCurveColor    { get; set; } = new Color(1f, 0.3f, 0.3f);
        public Color SelectedPointColor  { get; set; } = Colors.Red;
        public Color FillColor           { get; set; } = new Color(1f, 1f, 0f, 0.4f);
        public int   DotRadius           { get; set; } = 5;
        private const float SELECT_THRESHOLD = 12f;

        // ── Status ───────────────────────────────────────────────────────────
        public string StatusText
        {
            get
            {
                string method = _useCasteljau ? "Casteljau" : "Direct";
                string mode   = _editMode ? "ÉDITION" : "AJOUT";
                int step = _activeNode?.Value.Step ?? 0;
                int curveCount = _curves.Count;
                return string.Format(
                    "BEZIER | Mode:{0} | Méthode:{1} | Pas:{2} | Courbes:{3}\n{4}\n{5}",
                    mode, method, step, curveCount,
                    _editMode ? "ClicG=sélect  Glisser=déplacer  Suppr=retirer" : "ClicG=ajouter point  ClicD=menu  [↑↓←→] Translater  [R] Rotation  [S] Échelle  [H] Cisaillement",
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

            // Edit mode: try to select nearest vertex on any curve
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
            _fillSegs.Clear();
        }

        public void DeleteActiveCurve()
        {
            if (_activeNode == null) return;
            var next = _activeNode.Next ?? _activeNode.Previous;
            _curves.Remove(_activeNode);
            _activeNode = next;
            _fillSegs.Clear();
        }

        public void SelectNext()
        {
            if (_activeNode?.Next != null) _activeNode = _activeNode.Next;
        }

        public void SelectPrev()
        {
            if (_activeNode?.Previous != null) _activeNode = _activeNode.Previous;
        }

        public void ToggleMethod()   { _useCasteljau = !_useCasteljau; _benchText = ""; }
        public void ToggleEditMode() { _editMode = !_editMode; _dragIndex = -1; _dragging = false; }

        // ── Transforms ───────────────────────────────────────────────────────
        public void ApplyTransform(Matrix3x3 m)
        {
            _activeNode?.Value.ApplyTransform(m);
        }

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
        public void FillActiveCurve()
        {
            _fillSegs.Clear();
            if (_activeNode == null) return;
            var pts = _activeNode.Value.GetPoints(_useCasteljau);
            if (pts.Count < 3) return;

            var poly   = new Polygon(pts);
            var filler = new LCAFill();
            var segs   = filler.FillPolygon(poly);
            foreach (var s in segs)
                _fillSegs.Add((s.Item1, s.Item2));
        }

        // ── Drawing ──────────────────────────────────────────────────────────
        public void Draw(Node2D canvas)
        {
            // Draw fill scanlines first (behind curves)
            foreach (var (a, b) in _fillSegs)
                canvas.DrawLine(P(a), P(b), FillColor, 1);

            // Draw all curves
            var node = _curves.First;
            while (node != null)
            {
                DrawCurve(canvas, node.Value, node == _activeNode);
                node = node.Next;
            }
        }

        private void DrawCurve(Node2D canvas, BezierCurve curve, bool isActive)
        {
            var pts = curve.ControlPoints;
            Color cc = isActive ? ActiveCurveColor : CurveColor;

            // Control polygon (thin gray lines)
            for (int i = 0; i < pts.Count - 1; i++)
                canvas.DrawLine(P(pts[i]), P(pts[i + 1]), ControlPolygonColor, 1);

            // Control point dots
            for (int i = 0; i < pts.Count; i++)
            {
                bool sel = isActive && _editMode && i == _dragIndex;
                canvas.DrawCircle(P(pts[i]), DotRadius, sel ? SelectedPointColor : Colors.Black);
            }

            // Curve segments
            if (pts.Count >= 2)
            {
                var curvePts = curve.GetPoints(_useCasteljau);
                for (int i = 0; i < curvePts.Count - 1; i++)
                    canvas.DrawLine(P(curvePts[i]), P(curvePts[i + 1]), cc, 2);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static Point2D V(Vector2 v) => new Point2D(v.X, v.Y);
        private static Vector2  P(Point2D p) => new Vector2(p.x, p.y);
    }
}
