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

        // T2 – Edit mode (drag polygon vertices)
        private bool _editMode  = false;
        private int  _dragIndex = -1;       // index in _subject.Vertices being dragged
        private bool _dragging  = false;
        private const float EDIT_THRESHOLD = 12f;

        // Pre-computed LCA fill segments for each polygon
        private List<(Point2D, Point2D)> _subjectFill = new List<(Point2D, Point2D)>();
        private List<(Point2D, Point2D)> _windowFill  = new List<(Point2D, Point2D)>();
        private List<(Point2D, Point2D)> _resultFill  = new List<(Point2D, Point2D)>();

        public Color SubjectColor { get; set; } = new Color(0.2f, 0.6f, 1f);
        public Color WindowColor  { get; set; } = new Color(1f, 0.6f, 0.1f);
        public Color ResultColor  { get; set; } = new Color(0.2f, 0.9f, 0.3f);
        public int   DotRadius    { get; set; } = 5;

        public string StatusText =>
            _editMode   ? "POLYGONE ÉDITION – Glisser=déplacer sommet | RightClick=menu" :
            !_subjectClosed ? "Click: ajouter sommet polygone | RightClick: fermer" :
            !_windowClosed  ? "Click: ajouter sommet fenêtre (convexe) | RightClick: fermer" :
                              "Clipping terminé. Menu→Reset pour recommencer.";

        // ── Input ────────────────────────────────────────────────────────────
        public bool HandleLeftClick(Vector2 mouse)
        {
            // In edit mode, start drag if near a subject vertex
            if (_editMode)
            {
                _dragIndex = -1;
                _dragging  = false;
                for (int i = 0; i < _subject.Vertices.Count; i++)
                {
                    if (new Vector2(_subject.Vertices[i].x, _subject.Vertices[i].y).DistanceTo(mouse) <= EDIT_THRESHOLD)
                    {
                        _dragIndex = i;
                        _dragging  = true;
                        return true;
                    }
                }
                return false;
            }

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

        // T2 – drag support
        public void HandleMouseMove(Vector2 mouse)
        {
            if (_editMode && _dragging && _dragIndex >= 0 && _dragIndex < _subject.Vertices.Count)
            {
                _subject.Vertices[_dragIndex] = V(mouse);
                // Recompute fill if subject is already closed
                if (_subjectClosed) ComputeFill(_subject, _subjectFill);
                // Rerun clipping if result was ready
                if (_resultReady) RunClipping();
            }
        }

        public void HandleLeftRelease() { _dragging = false; }

        public bool HandleRightClick()
        {
            if (!_subjectClosed && _subject.Vertices.Count >= 3)
            {
                _subjectClosed = true;
                ComputeFill(_subject, _subjectFill);
                return true;
            }
            if (!_windowClosed && _window.Vertices.Count >= 3)
            {
                _windowClosed = true;
                ComputeFill(_window, _windowFill);
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
            _subjectFill.Clear();
            _windowFill.Clear();
            _resultFill.Clear();
            _dragging = false; _dragIndex = -1;
        }

        public void ToggleEditMode() { _editMode = !_editMode; _dragging = false; _dragIndex = -1; }  // T2

        // ── Drawing ──────────────────────────────────────────────────────────
        public void Draw(Node2D canvas)
        {
            // Fill via LCA scanlines (drawn first, behind outlines)
            if (_resultReady && !_result.IsEmpty)
            {
                var fc = new Color(ResultColor.R, ResultColor.G, ResultColor.B, 0.55f);
                foreach (var (a, b) in _resultFill)
                    canvas.DrawLine(P(a), P(b), fc, 1);
            }
            else
            {
                if (_subjectClosed)
                {
                    var fc = new Color(SubjectColor.R, SubjectColor.G, SubjectColor.B, 0.3f);
                    foreach (var (a, b) in _subjectFill)
                        canvas.DrawLine(P(a), P(b), fc, 1);
                }
                if (_windowClosed)
                {
                    var fc = new Color(WindowColor.R, WindowColor.G, WindowColor.B, 0.3f);
                    foreach (var (a, b) in _windowFill)
                        canvas.DrawLine(P(a), P(b), fc, 1);
                }
            }

            // Outlines
            DrawOutline(canvas, _subject, SubjectColor, _subjectClosed);
            DrawOutline(canvas, _window,  WindowColor,  _windowClosed);
            if (_resultReady && !_result.IsEmpty)
                DrawOutline(canvas, _result, ResultColor, true);

            // Dots
            if (!_resultReady) { DrawDots(canvas, _subject); DrawDots(canvas, _window); }
            DrawDots(canvas, _result);
        }

        // ── Private helpers ──────────────────────────────────────────────────
        private void RunClipping()
        {
            _result = new SutherlandHodgman().ClipPolygon(_subject, _window);
            _resultReady = true;
            ComputeFill(_result, _resultFill);
        }

        private static void ComputeFill(Polygon poly, List<(Point2D, Point2D)> list)
        {
            list.Clear();
            if (poly == null || poly.Vertices.Count < 3) return;
            var segs = new LCAFill().FillPolygon(poly);
            foreach (var s in segs) list.Add((s.Item1, s.Item2));
        }

        private static bool IsConvexPartial(List<Point2D> pts)
            => pts.Count < 4 || new Polygon(pts).IsConvex();

        private void DrawOutline(Node2D c, Polygon p, Color col, bool close)
        {
            var v = p.Vertices;
            for (int i = 0; i < v.Count - 1; i++) c.DrawLine(P(v[i]), P(v[i + 1]), col, 2);
            if (close && v.Count >= 2) c.DrawLine(P(v[v.Count - 1]), P(v[0]), col, 2);
        }

        private void DrawDots(Node2D c, Polygon p)
        {
            var verts = p.Vertices;
            for (int i = 0; i < verts.Count; i++)
            {
                bool sel = _editMode && p == _subject && i == _dragIndex;
                c.DrawCircle(P(verts[i]), DotRadius, sel ? Colors.Red : Colors.Black);
            }
        }

        private static Point2D V(Vector2 v) => new Point2D(v.X, v.Y);
        private static Vector2  P(Point2D p) => new Vector2(p.x, p.y);

    }
}
