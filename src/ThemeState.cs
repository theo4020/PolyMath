using Godot;

namespace MathsPower;

// État de thème global (sombre/clair) + palette de couleurs unifiée.
// Toute l'UI consulte `Palette.Current()` au moment du rendu.
public static class ThemeState
{
    public static bool IsDark { get; private set; } = true;

    public static bool Toggle()
    {
        IsDark = !IsDark;
        return IsDark;
    }
}

public readonly struct Palette
{
    // Fonds
    public readonly Color BgApp;
    public readonly Color BgPanel;
    public readonly Color BgCanvas;
    public readonly Color Border;

    // Texte
    public readonly Color TextStrong;
    public readonly Color Text;
    public readonly Color TextDim;
    public readonly Color SectionLabel;

    // Éditeur 2D
    public readonly Color CanvasGrid;
    public readonly Color CanvasAxis;
    public readonly Color ControlPoint;
    public readonly Color ControlPointActive;
    public readonly Color ControlPolygon;
    public readonly Color Curve;

    // Scène 3D
    public readonly Color Wireframe;
    public readonly Color GroundGridLines;

    private Palette(
        Color bgApp, Color bgPanel, Color bgCanvas, Color border,
        Color textStrong, Color text, Color textDim, Color sectionLabel,
        Color canvasGrid, Color canvasAxis, Color controlPoint,
        Color controlPointActive, Color controlPolygon, Color curve,
        Color wireframe, Color groundGridLines)
    {
        BgApp = bgApp; BgPanel = bgPanel; BgCanvas = bgCanvas; Border = border;
        TextStrong = textStrong; Text = text; TextDim = textDim; SectionLabel = sectionLabel;
        CanvasGrid = canvasGrid; CanvasAxis = canvasAxis; ControlPoint = controlPoint;
        ControlPointActive = controlPointActive; ControlPolygon = controlPolygon; Curve = curve;
        Wireframe = wireframe; GroundGridLines = groundGridLines;
    }

    public static Palette Current() => ThemeState.IsDark ? Dark() : Light();

    public static Palette Dark() => new(
        bgApp: new Color(0.078f, 0.086f, 0.110f),
        bgPanel: new Color(0.110f, 0.120f, 0.150f),
        bgCanvas: new Color(0.060f, 0.070f, 0.090f),
        border: new Color(0.230f, 0.250f, 0.310f),
        textStrong: new Color(0.910f, 0.930f, 0.980f),
        text: new Color(0.770f, 0.800f, 0.860f),
        textDim: new Color(0.700f, 0.750f, 0.850f, 0.7f),
        sectionLabel: new Color(0.520f, 0.700f, 1.000f),
        canvasGrid: new Color(0.35f, 0.40f, 0.50f, 0.40f),
        canvasAxis: new Color(0.45f, 0.50f, 0.60f),
        controlPoint: new Color(1.0f, 0.78f, 0.35f),
        controlPointActive: new Color(1.0f, 0.95f, 0.55f),
        controlPolygon: new Color(1.0f, 0.78f, 0.42f, 0.55f),
        curve: new Color(0.43f, 0.78f, 1.0f),
        wireframe: new Color(0.92f, 0.94f, 1.00f, 0.55f),
        groundGridLines: new Color(0.42f, 0.46f, 0.55f, 0.80f));

    // Thème clair « studio » : gris neutres doux plutôt que du blanc pur, pour
    // un rendu pro et reposant. Hiérarchie de profondeur : viewport (le plus
    // grand aplat) le plus sombre, panneaux off-white, canevas d'édition à peine
    // plus clair. Texte ardoise foncée (pas de noir pur).
    public static Palette Light() => new(
        bgApp: new Color(0.843f, 0.860f, 0.884f),
        bgPanel: new Color(0.903f, 0.915f, 0.933f),
        bgCanvas: new Color(0.957f, 0.965f, 0.976f),
        border: new Color(0.748f, 0.775f, 0.828f),
        textStrong: new Color(0.135f, 0.165f, 0.225f),
        text: new Color(0.290f, 0.330f, 0.405f),
        textDim: new Color(0.455f, 0.505f, 0.600f, 0.9f),
        sectionLabel: new Color(0.195f, 0.375f, 0.745f),
        canvasGrid: new Color(0.620f, 0.660f, 0.735f, 0.5f),
        canvasAxis: new Color(0.490f, 0.535f, 0.630f),
        controlPoint: new Color(0.880f, 0.520f, 0.120f),
        controlPointActive: new Color(0.950f, 0.655f, 0.190f),
        controlPolygon: new Color(0.800f, 0.520f, 0.220f, 0.6f),
        curve: new Color(0.165f, 0.495f, 0.840f),
        wireframe: new Color(0.120f, 0.165f, 0.270f, 0.7f),
        groundGridLines: new Color(0.515f, 0.555f, 0.645f, 0.75f));
}
