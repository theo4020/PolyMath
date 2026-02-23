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
                              "Clipping done. Right-click menu → Reset to restart.";

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
            for (int i = 0; i < v.Count - 1; i++) c.DrawLine(P(v[i]), P(v[i + 1]), col, 2);
            if (close && v.Count >= 2) c.DrawLine(P(v[v.Count - 1]), P(v[0]), col, 2);
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
