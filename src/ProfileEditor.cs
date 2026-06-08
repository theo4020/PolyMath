using System.Collections.Generic;
using Godot;

namespace MathsPower;

// Éditeur 2D de la courbe de contrôle (profil ou âme). Le type de courbe
// (Bézier / B-spline / NURBS / Polygone) est choisi dans le panneau latéral.
//   - clic gauche vide  : ajoute un point (à la fin ou inséré sur un segment)
//   - clic gauche point : déplace le point
//   - clic droit point  : supprime le point
//   - clic-milieu       : déplace la vue (pan)
//   - molette sur point : règle son poids NURBS (Maj = pas fin, double-clic = 1)
// CurveKind ∈ {"profile", "ame"} sélectionne la courbe éditée.
[GlobalClass]
public partial class ProfileEditor : Control
{
    [Export] public NodePath SurfacePath { get; set; } = new();
    [Export] public string CurveKind { get; set; } = "profile";
    [Export] public Vector2 BoundsMin { get; set; } = new(-1.8f, -1.8f);
    [Export] public Vector2 BoundsMax { get; set; } = new(1.8f, 1.8f);

    private List<Vector2> _controlPoints = new();
    private List<float> _weights = new(); 
    private bool _closed;
    private int _draggingIdx = -1;
    private int _hoveredIdx = -1;
    private const float PickRadiusPx = 10.0f;
    private const float WeightMin = 0.1f;
    private const float WeightMax = 8.0f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SyncFromSurface();
        QueueRedraw();
    }

    private ProfileCurveKind _lastKind = ProfileCurveKind.Bezier;
    private int _lastDegree = 3;

    public override void _Process(double delta)
    {
        bool dirty = _draggingIdx < 0 && SyncFromSurface();
        var surface = GetSurface();
        var kind = surface?.ProfileCurveKindValue ?? ProfileCurveKind.Bezier;
        int degree = surface?.SplineDegreeValue ?? 3;
        if (kind != _lastKind) { _lastKind = kind; dirty = true; }
        if (degree != _lastDegree) { _lastDegree = degree; dirty = true; }
        if (dirty) QueueRedraw();
    }

    private bool NurbsActive() =>
        (GetSurface()?.ProfileCurveKindValue ?? ProfileCurveKind.Bezier) == ProfileCurveKind.NURBS;

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
            HandleButton(mb);
        else if (@event is InputEventMouseMotion motion)
            HandleMotion(motion);
    }

    public void RefreshTheme() => QueueRedraw();

    // Conversions monde ↔ pixel
    private Vector2 ToScreen(Vector2 p)
    {
        Vector2 size = Size;
        float tx = (p.X - BoundsMin.X) / (BoundsMax.X - BoundsMin.X);
        float ty = (p.Y - BoundsMin.Y) / (BoundsMax.Y - BoundsMin.Y);
        return new Vector2(tx * size.X, size.Y - ty * size.Y);
    }

    private Vector2 ToWorld(Vector2 pos)
    {
        Vector2 size = Size;
        float tx = pos.X / size.X;
        float ty = (size.Y - pos.Y) / size.Y;
        return new Vector2(
            BoundsMin.X + tx * (BoundsMax.X - BoundsMin.X),
            BoundsMin.Y + ty * (BoundsMax.Y - BoundsMin.Y));
    }

    private int PickPoint(Vector2 pos)
    {
        int best = -1;
        float bestD = float.PositiveInfinity;
        for (int i = 0; i < _controlPoints.Count; i++)
        {
            float d = ToScreen(_controlPoints[i]).DistanceTo(pos);
            if (d < bestD) { bestD = d; best = i; }
        }
        return bestD < PickRadiusPx ? best : -1;
    }

    private int PickSegment(Vector2 pos)
    {
        int n = _controlPoints.Count;
        if (n < 2) return -1;
        int last = _closed ? n : n - 1;
        float threshold = PickRadiusPx * 0.7f;
        int best = -1;
        float bestD = float.PositiveInfinity;
        for (int i = 0; i < last; i++)
        {
            Vector2 a = ToScreen(_controlPoints[i]);
            Vector2 b = ToScreen(_controlPoints[(i + 1) % n]);
            float d = DistanceToSegment(pos, a, b);
            if (d < bestD) { bestD = d; best = i; }
        }
        return bestD < threshold ? best : -1;
    }

    // Interactions
    private void HandleButton(InputEventMouseButton mb)
    {
        Vector2 pos = mb.Position;

        // Molette sur un point → ajuste son poids (NURBS seulement). Maj = pas fin.
        // L'événement est toujours consommé pour ne pas atteindre la caméra
        // (sinon la molette zoome la vue 3D en même temps).
        if (mb.Pressed && (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown))
        {
            if (NurbsActive())
            {
                int idx = PickPoint(pos);
                if (idx >= 0 && idx < _weights.Count)
                {
                    float step = mb.ShiftPressed ? 1.05f : 1.18f;
                    float factor = mb.ButtonIndex == MouseButton.WheelUp ? step : 1.0f / step;
                    _weights[idx] = Mathf.Clamp(_weights[idx] * factor, WeightMin, WeightMax);
                    PushToSurface();
                    QueueRedraw();
                }
            }
            AcceptEvent();
            return;
        }

        if (mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                // Double-clic sur un point → poids neutre (NURBS).
                if (mb.DoubleClick && NurbsActive())
                {
                    int reset = PickPoint(pos);
                    if (reset >= 0 && reset < _weights.Count)
                    {
                        _weights[reset] = 1.0f;
                        PushToSurface();
                        QueueRedraw();
                        AcceptEvent();
                        return;
                    }
                }

                int pt = PickPoint(pos);
                if (pt >= 0)
                {
                    _draggingIdx = pt;
                }
                else
                {
                    int seg = PickSegment(pos);
                    Vector2 world = ToWorld(pos);
                    if (seg >= 0)
                    {
                        _controlPoints.Insert(seg + 1, world);
                        _weights.Insert(seg + 1, 1.0f);
                        _draggingIdx = seg + 1;
                    }
                    else
                    {
                        _controlPoints.Add(world);
                        _weights.Add(1.0f);
                        _draggingIdx = _controlPoints.Count - 1;
                    }
                    PushToSurface();
                }
                QueueRedraw();
            }
            else
            {
                _draggingIdx = -1;
            }
        }
        else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
        {
            int idx = PickPoint(pos);
            if (idx >= 0)
            {
                _controlPoints.RemoveAt(idx);
                if (idx < _weights.Count) _weights.RemoveAt(idx);
                if (_draggingIdx == idx) _draggingIdx = -1;
                else if (_draggingIdx > idx) _draggingIdx--;
                if (_hoveredIdx == idx) _hoveredIdx = -1;
                else if (_hoveredIdx > idx) _hoveredIdx--;
                PushToSurface();
                QueueRedraw();
            }
        }
    }

    private void HandleMotion(InputEventMouseMotion motion)
    {
        Vector2 pos = motion.Position;

        if (Input.IsMouseButtonPressed(MouseButton.Middle))
        {
            Vector2 delta = motion.Relative;
            Vector2 size = Size;
            float spanX = BoundsMax.X - BoundsMin.X;
            float spanY = BoundsMax.Y - BoundsMin.Y;
            float worldDx = -delta.X * spanX / size.X;
            float worldDy = delta.Y * spanY / size.Y;
            BoundsMin += new Vector2(worldDx, worldDy);
            BoundsMax += new Vector2(worldDx, worldDy);
            MouseDefaultCursorShape = CursorShape.Drag;
            QueueRedraw();
            return;
        }

        if (_draggingIdx >= 0)
        {
            _controlPoints[_draggingIdx] = ToWorld(pos);
            PushToSurface();
            QueueRedraw();
            return;
        }

        int newHover = PickPoint(pos);
        if (newHover != _hoveredIdx)
        {
            _hoveredIdx = newHover;
            MouseDefaultCursorShape = newHover >= 0 ? CursorShape.PointingHand : CursorShape.Cross;
            QueueRedraw();
        }
    }

    // Synchronisation avec la surface
    private SurfaceCours1? GetSurface() =>
        SurfacePath.IsEmpty ? null : GetNodeOrNull<SurfaceCours1>(SurfacePath);

    private bool IsProfile() => CurveKind != "ame";

    private bool SyncFromSurface()
    {
        var surface = GetSurface();
        if (surface == null) return false;
        Vector2[] newPts = IsProfile() ? surface.ProfileControlPoints : surface.AmeControlPoints;
        float[] newWeights = IsProfile() ? surface.ProfileWeights : surface.AmeWeights;
        bool newClosed = IsProfile() ? surface.ProfileClosed : surface.AmeClosed;

        bool changed = newClosed != _closed
            || newPts.Length != _controlPoints.Count
            || newWeights.Length != _weights.Count;
        if (!changed)
            for (int i = 0; i < newPts.Length; i++)
                if (newPts[i] != _controlPoints[i]) { changed = true; break; }
        if (!changed)
            for (int i = 0; i < newWeights.Length; i++)
                if (!Mathf.IsEqualApprox(newWeights[i], _weights[i])) { changed = true; break; }

        if (changed)
        {
            _controlPoints = new List<Vector2>(newPts);
            _weights = new List<float>(newWeights);
            _closed = newClosed;
        }
        return changed;
    }

    private void PushToSurface()
    {
        var surface = GetSurface();
        if (surface == null) return;
        // Poids toujours alignés sur les points avant l'envoi.
        while (_weights.Count < _controlPoints.Count) _weights.Add(1.0f);
        if (_weights.Count > _controlPoints.Count)
            _weights.RemoveRange(_controlPoints.Count, _weights.Count - _controlPoints.Count);

        Vector2[] pts = _controlPoints.ToArray();
        float[] weights = _weights.ToArray();
        if (IsProfile()) surface.SetProfilePoints(pts, weights, _closed);
        else surface.SetAmePoints(pts, weights, _closed);
    }

    // Rendu
    public override void _Draw()
    {
        Palette palette = Palette.Current();
        Vector2 size = Size;
        var rect = new Rect2(Vector2.Zero, size);

        DrawRect(rect, palette.BgCanvas, filled: true);
        DrawRect(rect, palette.Border, filled: false, width: 1.0f);

        // Grille.
        for (int k = 1; k < 10; k++)
        {
            float f = k / 10.0f;
            DrawLine(new Vector2(f * size.X, 0), new Vector2(f * size.X, size.Y), palette.CanvasGrid);
            DrawLine(new Vector2(0, f * size.Y), new Vector2(size.X, f * size.Y), palette.CanvasGrid);
        }

        // Axes.
        Vector2 origin = ToScreen(Vector2.Zero);
        if (BoundsMin.X <= 0 && 0 <= BoundsMax.X)
            DrawLine(new Vector2(origin.X, 0), new Vector2(origin.X, size.Y), palette.CanvasAxis);
        if (BoundsMin.Y <= 0 && 0 <= BoundsMax.Y)
            DrawLine(new Vector2(0, origin.Y), new Vector2(size.X, origin.Y), palette.CanvasAxis);

        // Polygone de contrôle + courbe.
        if (_controlPoints.Count >= 2)
        {
            var screen = new List<Vector2>();
            foreach (var p in _controlPoints)
                screen.Add(ToScreen(p));
            for (int i = 0; i + 1 < screen.Count; i++)
                DrawLine(screen[i], screen[i + 1], palette.ControlPolygon);
            if (_closed)
                DrawLine(screen[^1], screen[0], palette.ControlPolygon);

            // Trace la courbe selon le type sélectionné sur la surface.
            var surface = GetSurface();
            var kind = surface?.ProfileCurveKindValue ?? ProfileCurveKind.Bezier;
            int degree = surface?.SplineDegreeValue ?? 3;
            var samples = ProfileSampler.Sample(_controlPoints, _closed, kind, 90, degree, _weights);
            for (int i = 0; i + 1 < samples.Count; i++)
                DrawLine(ToScreen(samples[i]), ToScreen(samples[i + 1]), palette.Curve, 2.2f);
        }

        // Sommets (+ poids quand la NURBS est active).
        bool nurbs = NurbsActive();
        Font? font = GetThemeDefaultFont();
        for (int i = 0; i < _controlPoints.Count; i++)
        {
            Vector2 sp = ToScreen(_controlPoints[i]);
            bool isDrag = _draggingIdx == i;
            bool isHover = _hoveredIdx == i;
            float r = isDrag ? 7.0f : isHover ? 6.5f : 5.5f;
            Color color = isDrag || isHover ? palette.ControlPointActive : palette.ControlPoint;

            if (nurbs && i < _weights.Count)
            {
                float w = _weights[i];
                float ringR = Mathf.Clamp(r + 3.0f + (w - 1.0f) * 3.5f, r + 1.5f, r + 22.0f);
                Color ringColor = isDrag || isHover ? palette.ControlPointActive : palette.ControlPolygon;
                DrawArc(sp, ringR, 0.0f, Mathf.Tau, 28, ringColor, 1.6f);
                if (font != null && (isDrag || isHover || Mathf.Abs(w - 1.0f) > 0.01f))
                    DrawString(font, sp + new Vector2(ringR + 3.0f, 4.0f), w.ToString("0.0"),
                        HorizontalAlignment.Left, -1, 10, palette.Text);
            }

            DrawCircle(sp, r, color);
        }

        // Hint.
        if (font != null)
        {
            string hint = nurbs
                ? "molette sur un point = poids  ·  drag = déplacer  ·  clic droit = supprimer"
                : "clic = ajouter  ·  drag = déplacer  ·  clic droit = supprimer  ·  clic-milieu = pan";
            DrawString(font, new Vector2(8, size.Y - 8), hint,
                HorizontalAlignment.Center, size.X - 16, 10, palette.TextDim);
        }
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.X * ab.X + ab.Y * ab.Y;
        if (lenSq < 1e-6f)
            return p.DistanceTo(a);
        Vector2 ap = p - a;
        float t = Mathf.Clamp((ap.X * ab.X + ap.Y * ab.Y) / lenSq, 0.0f, 1.0f);
        Vector2 proj = new(a.X + ab.X * t, a.Y + ab.Y * t);
        return p.DistanceTo(proj);
    }
}
