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
        private bool _nurbsMode = false;
        private bool _editMode  = false;
        private int  _dragIndex = -1;
        private bool _dragging  = false;
        private float _weightEdit = 1f;

        private const float SELECT_THRESHOLD = 12f;
        public Color ControlColor { get; set; } = new Color(0.5f, 0.5f, 0.5f);
        public Color CurveColor   { get; set; } = new Color(0.2f, 0.9f, 0.5f);
        public Color ActiveColor  { get; set; } = new Color(1f, 0.5f, 0f);
        public int   DotRadius    { get; set; } = 5;

        public string StatusText
        {
            get
            {
                string type = _nurbsMode ? "NURBS" : "BSpline";
                int deg = ActiveCurve?.Degree ?? 0;
                int cp  = ActiveCurve?.ControlPoints.Count ?? 0;
                string knots = (ActiveCurve?.IsClamped ?? true) ? "Serré" : "Uniforme";
                string mode  = _editMode ? "ÉDITION" : "AJOUT";
                return string.Format(
                    "MODE: {0} | Degré:{1} | CP:{2} | Nœuds:{3} | {4}\nCourbes: {5}\n{6}",
                    type, deg, cp, knots, mode,
                    _bsplines.Count + _nurbs.Count,
                    _editMode ? "ClicG=sélect  Glisser=déplacer  Suppr=retirer" : "ClicG=ajouter point  ClicD=menu  [↑↓←→] Translater  [R] Rotation  [S] Échelle  [H] Cisaillement");
            }
        }

        private BSplineCurve ActiveCurve => _nurbsMode
            ? (BSplineCurve)_activeNurbs?.Value
            : _activeBs?.Value;

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

            _dragIndex = -1;
            _dragging  = false;
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
            if (_editMode && _dragIndex >= 0)
            {
                ActiveCurve?.RemovePoint(_dragIndex);
                _dragIndex = -1;
                _dragging  = false;
            }
            else if (_nurbsMode)
            {
                if (_activeNurbs != null)
                {
                    var next = _activeNurbs.Next ?? _activeNurbs.Previous;
                    _nurbs.Remove(_activeNurbs);
                    _activeNurbs = next;
                }
            }
            else
            {
                if (_activeBs != null)
                {
                    var next = _activeBs.Next ?? _activeBs.Previous;
                    _bsplines.Remove(_activeBs);
                    _activeBs = next;
                }
            }
        }

        public void DegreeUp()
        {
            var c = ActiveCurve;
            if (c != null && c.Degree < c.ControlPoints.Count - 1)
            {
                c.Degree++;
                if (c.IsClamped) c.SetClamped(); else c.SetUniform();
            }
        }

        public void DegreeDown()
        {
            var c = ActiveCurve;
            if (c != null && c.Degree > 1)
            {
                c.Degree--;
                if (c.IsClamped) c.SetClamped(); else c.SetUniform();
            }
        }

        public void StepUp()
        {
            var c = ActiveCurve;
            if (c != null) c.Step++;
        }

        public void StepDown()
        {
            var c = ActiveCurve;
            if (c != null && c.Step > 2) c.Step--;
        }

        public void ToggleKnots()
        {
            var c = ActiveCurve;
            if (c == null) return;
            if (c.IsClamped) c.SetUniform(); else c.SetClamped();
        }

        public void ToggleEditMode() { _editMode = !_editMode; _dragIndex = -1; _dragging = false; }
        public void ToggleNurbsMode() { _nurbsMode = !_nurbsMode; }

        public void ApplyTransform(Matrix3x3 m) { ActiveCurve?.ApplyTransform(m); }

        public void NewBSpline()
        {
            _activeBs = _bsplines.AddLast(new BSplineCurve());
            _nurbsMode = false;
        }

        public void NewNurbs()
        {
            _activeNurbs = _nurbs.AddLast(new NURBSCurve());
            _nurbsMode = true;
        }

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

        public void Draw(Node2D canvas)
        {
            var bsNode = _bsplines.First;
            while (bsNode != null)
            {
                DrawCurve(canvas, bsNode.Value, bsNode == _activeBs && !_nurbsMode);
                bsNode = bsNode.Next;
            }

            var nrNode = _nurbs.First;
            while (nrNode != null)
            {
                DrawCurve(canvas, nrNode.Value, nrNode == _activeNurbs && _nurbsMode);
                nrNode = nrNode.Next;
            }
        }

        private void DrawCurve(Node2D canvas, BSplineCurve curve, bool isActive)
        {
            var pts = curve.ControlPoints;
            Color cc = isActive ? ActiveColor : CurveColor;

            for (int i = 0; i < pts.Count - 1; i++)
                canvas.DrawLine(P(pts[i]), P(pts[i + 1]), ControlColor, 1);

            for (int i = 0; i < pts.Count; i++)
            {
                bool sel = isActive && _editMode && i == _dragIndex;
                canvas.DrawCircle(P(pts[i]), DotRadius, sel ? Colors.Red : Colors.Black);
            }

            if (pts.Count > curve.Degree)
            {
                var cPts = curve.GetPoints();
                for (int i = 0; i < cPts.Count - 1; i++)
                    canvas.DrawLine(P(cPts[i]), P(cPts[i + 1]), cc, 2);
            }
        }

        private static Point2D V(Vector2 v) => new Point2D(v.X, v.Y);
        private static Vector2  P(Point2D p) => new Vector2(p.x, p.y);
    }
}
