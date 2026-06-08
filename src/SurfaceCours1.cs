using System.Collections.Generic;
using Godot;

namespace MathsPower;

public enum Primitive
{
    Sphere = 0,
    Cylinder = 1,
    Cone = 2,
    ExtrusionSimple = 3,
    Revolution = 4,
    ExtrusionGeneralisee = 5,
}

public enum SourceCurveAxis { ColumnI0, RowJ0 }

public static class PrimitiveExt
{
    public static string DisplayName(this Primitive p) => p switch
    {
        Primitive.Sphere => "Sphère",
        Primitive.Cylinder => "Cylindre",
        Primitive.Cone => "Cône",
        Primitive.ExtrusionSimple => "Extrusion simple",
        Primitive.Revolution => "Révolution",
        Primitive.ExtrusionGeneralisee => "Extrusion généralisée",
        _ => "?",
    };

    public static bool UsesProfile(this Primitive p) =>
        p is Primitive.ExtrusionSimple or Primitive.Revolution or Primitive.ExtrusionGeneralisee;

    public static SourceCurveAxis SourceAxis(this Primitive p) =>
        p is Primitive.Sphere or Primitive.Cylinder or Primitive.Cone
            ? SourceCurveAxis.ColumnI0
            : SourceCurveAxis.RowJ0;
}

// MeshInstance3D paramétré par une Primitive. Reconstruit le mesh à chaque
// changement de paramètre.
[GlobalClass]
public partial class SurfaceCours1 : MeshInstance3D
{
    private bool _initialized;

    private Primitive _primitive = Primitive.Sphere;
    private int _subdivisionsU = 60;
    private int _subdivisionsV = 30;
    private float _radius = 1.0f;
    private float _height = 2.0f;
    private float _pCoef = 0.5f;
    private float _ameZAmp = 0.0f;
    private float _ameZFreq = 1.0f;
    private bool _showWireframe = true;

    [Export] public Primitive Prim { get => _primitive; set { _primitive = value; Rebuild(); } }
    [Export] public int SubdivisionsU { get => _subdivisionsU; set { _subdivisionsU = Mathf.Clamp(value, 4, 256); Rebuild(); } }
    [Export] public int SubdivisionsV { get => _subdivisionsV; set { _subdivisionsV = Mathf.Clamp(value, 3, 256); Rebuild(); } }
    [Export] public float Radius { get => _radius; set { _radius = Mathf.Max(0.01f, value); Rebuild(); } }
    [Export] public float Height { get => _height; set { _height = Mathf.Max(0.01f, value); Rebuild(); } }
    [Export] public float PCoef { get => _pCoef; set { _pCoef = Mathf.Max(0.001f, value); Rebuild(); } }
    [Export] public float AmeZAmp { get => _ameZAmp; set { _ameZAmp = value; Rebuild(); } }
    [Export] public float AmeZFreq { get => _ameZFreq; set { _ameZFreq = value; Rebuild(); } }
    [Export] public bool ShowWireframe { get => _showWireframe; set { _showWireframe = value; Rebuild(); } }

    // Points de contrôle (non exportés). Chaque point porte un poids, utilisé
    // uniquement par la NURBS (1 = point ordinaire ; > 1 attire la courbe).
    private List<Vector2> _profileControlPoints = new();
    private List<float> _profileWeights = new();
    private bool _profileClosed;
    private List<Vector2> _ameControlPoints = new();
    private List<float> _ameWeights = new();
    private bool _ameClosed;

    public Vector2[] ProfileControlPoints => _profileControlPoints.ToArray();
    public Vector2[] AmeControlPoints => _ameControlPoints.ToArray();
    public float[] ProfileWeights => _profileWeights.ToArray();
    public float[] AmeWeights => _ameWeights.ToArray();
    public bool ProfileClosed => _profileClosed;
    public bool AmeClosed => _ameClosed;

    public Primitive CurrentPrimitive => _primitive;
    public int SubU => _subdivisionsU;
    public int SubV => _subdivisionsV;
    public float RadiusValue => _radius;
    public float HeightValue => _height;
    public float PCoefValue => _pCoef;
    public float AmeZAmpValue => _ameZAmp;
    public float AmeZFreqValue => _ameZFreq;
    public bool WireframeValue => _showWireframe;
    public ProfileCurveKind ProfileCurveKindValue => _profileCurveKind;
    public int SplineDegreeValue => _splineDegree;

    private ProfileCurveKind _profileCurveKind = ProfileCurveKind.Bezier;
    private int _splineDegree = 3;

    public void SetProfileCurveKind(int kind)
    {
        _profileCurveKind = (ProfileCurveKind)kind;
        Rebuild();
    }

    public void SetSplineDegree(int degree)
    {
        _splineDegree = degree;
        Rebuild();
    }

    private MeshInstance3D? _profileCurveNode;
    private MeshInstance3D? _ameCurveNode;
    private SurfaceShaderController? _shader;

    public override void _Ready()
    {
        _shader = new SurfaceShaderController();
        EnsureCurveChildren();
        _initialized = true;
        Rebuild();
    }

    public void SetRenderMode(int mode) => _shader?.SetMode(mode);
    public void SetShowGrid(bool show) => _shader?.SetShowGrid(show);
    public void SetGlossRoughness(float r) => _shader?.SetRoughness(r);
    public void SetLit(bool lit) => _shader?.SetLit(lit);

    // Setters appelés par l'éditeur 2D : points + poids associés.
    public void SetProfilePoints(Vector2[] pts, float[] weights, bool closed)
    {
        _profileControlPoints = new List<Vector2>(pts);
        _profileWeights = new List<float>(weights);
        _profileClosed = closed;
        Rebuild();
    }

    public void SetAmePoints(Vector2[] pts, float[] weights, bool closed)
    {
        _ameControlPoints = new List<Vector2>(pts);
        _ameWeights = new List<float>(weights);
        _ameClosed = closed;
        Rebuild();
    }

    public void ApplyTheme() => Rebuild();

    // Presets — repartent toujours sur des poids neutres (1).
    public void PresetProfileCircle()
    {
        var pts = new List<Vector2>();
        for (int k = 0; k < 12; k++)
        {
            float t = Mathf.Tau * k / 12.0f;
            pts.Add(new Vector2(Mathf.Cos(t) * 0.8f, Mathf.Sin(t) * 0.8f));
        }
        ApplyProfile(pts, closed: true);
    }

    public void PresetProfileVase() => ApplyProfile(new List<Vector2>
    {
        new(0.2f, 0.0f), new(0.8f, 0.2f), new(0.3f, 0.5f),
        new(0.6f, 0.9f), new(0.4f, 1.4f), new(0.0f, 1.7f),
    }, closed: false);

    public void PresetProfileStar() => ApplyProfile(new List<Vector2>
    {
        new(1.0f, 0.0f), new(0.35f, 0.25f), new(0.3f, 0.9f), new(-0.05f, 0.35f),
        new(-0.85f, 0.6f), new(-0.4f, 0.0f), new(-0.85f, -0.6f), new(-0.05f, -0.35f),
        new(0.3f, -0.9f), new(0.35f, -0.25f),
    }, closed: true);

    public void PresetProfileClear() => ApplyProfile(new List<Vector2>(), closed: false);

    public void PresetAmeHelice()
    {
        var pts = new List<Vector2>();
        for (int k = 0; k < 30; k++)
        {
            float t = Mathf.Tau * 2.0f * k / 29.0f;
            pts.Add(new Vector2(1.5f * Mathf.Cos(t), 1.5f * Mathf.Sin(t)));
        }
        ApplyAme(pts, closed: false);
    }

    public void PresetAmeStraight()
    {
        var pts = new List<Vector2>();
        for (int k = 0; k < 15; k++)
            pts.Add(new Vector2(3.0f * k / 14.0f - 1.5f, 0.0f));
        ApplyAme(pts, closed: false);
    }

    public void PresetAmeClear() => ApplyAme(new List<Vector2>(), closed: false);

    private void ApplyProfile(List<Vector2> pts, bool closed)
    {
        _profileControlPoints = pts;
        _profileWeights = NeutralWeights(pts.Count);
        _profileClosed = closed;
        Rebuild();
    }

    private void ApplyAme(List<Vector2> pts, bool closed)
    {
        _ameControlPoints = pts;
        _ameWeights = NeutralWeights(pts.Count);
        _ameClosed = closed;
        Rebuild();
    }

    private static List<float> NeutralWeights(int count)
    {
        var weights = new List<float>(count);
        for (int i = 0; i < count; i++) weights.Add(1.0f);
        return weights;
    }

    // Reconstruction
    private void EnsureCurveChildren()
    {
        if (_profileCurveNode == null)
        {
            _profileCurveNode = new MeshInstance3D { Name = "ProfileCurve3D" };
            AddChild(_profileCurveNode, false, InternalMode.Front);
        }
        if (_ameCurveNode == null)
        {
            _ameCurveNode = new MeshInstance3D { Name = "AmeCurve3D" };
            AddChild(_ameCurveNode, false, InternalMode.Front);
        }
    }

    private void Rebuild()
    {
        if (!_initialized)
            return;

        AlignWeights(_profileControlPoints, _profileWeights);
        AlignWeights(_ameControlPoints, _ameWeights);

        int m = _subdivisionsU;
        int p = _subdivisionsV;

        SurfaceGrid grid = _primitive switch
        {
            Primitive.Sphere => Surfaces.Sphere(_radius, m, p),
            Primitive.Cylinder => Surfaces.Cylinder(_radius, _height, m, p),
            Primitive.Cone => Surfaces.Cone(_radius, _height, m, p),
            Primitive.ExtrusionSimple => Surfaces.ExtrusionSimple(SampledProfile(m + 1), _height, _pCoef, p),
            Primitive.Revolution => Surfaces.Revolution(SampledProfile(m + 1), p),
            Primitive.ExtrusionGeneralisee => BuildGeneralisee(m, p),
            _ => Surfaces.Sphere(_radius, m, p),
        };

        Mesh = MeshBuilder.SurfaceGridToMesh(grid, _showWireframe, _shader?.Material);
        UpdateCurveOverlays(grid);
    }

    private SurfaceGrid BuildGeneralisee(int m, int p)
    {
        var forme = SampledProfile(m + 1);
        var ame = SampledAme(p + 1);
        float[]? z = null;
        if (Mathf.Abs(_ameZAmp) > 1e-6f)
        {
            z = new float[ame.Count];
            for (int k = 0; k < ame.Count; k++)
            {
                float s = (float)k / (ame.Count - 1);
                z[k] = _ameZAmp * Mathf.Sin(_ameZFreq * Mathf.Pi * s);
            }
        }
        return Surfaces.ExtrusionGeneralisee(forme, ame, z);
    }

    private void UpdateCurveOverlays(SurfaceGrid grid)
    {
        bool showAme = _primitive == Primitive.ExtrusionGeneralisee;
        int np = grid.P + 1;

        // Courbe source (orange).
        if (_profileCurveNode != null)
        {
            var pts = new List<Vector3>();
            if (_primitive.SourceAxis() == SourceCurveAxis.RowJ0)
            {
                for (int i = 0; i <= grid.M; i++)
                {
                    var q = grid.Data[i * np];
                    pts.Add(new Vector3(q.X, q.Z, -q.Y));
                }
            }
            else
            {
                for (int j = 0; j <= grid.P; j++)
                {
                    var q = grid.Data[j];
                    pts.Add(new Vector3(q.X, q.Z, -q.Y));
                }
            }
            if (pts.Count >= 2)
            {
                _profileCurveNode.Mesh = MeshBuilder.BuildTubeMesh(
                    pts.ToArray(), 0.022f, new Color(1.0f, 0.55f, 0.15f), 6);
                _profileCurveNode.Visible = true;
            }
            else
            {
                _profileCurveNode.Visible = false;
            }
        }

        // Âme (magenta).
        if (_ameCurveNode != null)
        {
            if (showAme)
            {
                int n = _subdivisionsV + 1;
                var ameXy = SampledAme(n);
                float[]? zLift = null;
                if (Mathf.Abs(_ameZAmp) > 1e-6f)
                {
                    zLift = new float[n];
                    for (int k = 0; k < n; k++)
                    {
                        float s = (float)k / (n - 1);
                        zLift[k] = _ameZAmp * Mathf.Sin(_ameZFreq * Mathf.Pi * s);
                    }
                }
                var pts = new List<Vector3>();
                for (int k = 0; k < ameXy.Count; k++)
                {
                    float z = zLift != null ? zLift[k] : 0.0f;
                    pts.Add(new Vector3(ameXy[k].X, z, -ameXy[k].Y));
                }
                if (pts.Count >= 2)
                    _ameCurveNode.Mesh = MeshBuilder.BuildTubeMesh(
                        pts.ToArray(), 0.022f, new Color(0.95f, 0.4f, 0.85f), 6);
                _ameCurveNode.Visible = true;
            }
            else
            {
                _ameCurveNode.Visible = false;
            }
        }
    }

    // Garde les poids alignés sur les points (nouveaux points → poids 1).
    private static void AlignWeights(List<Vector2> pts, List<float> weights)
    {
        while (weights.Count < pts.Count) weights.Add(1.0f);
        if (weights.Count > pts.Count) weights.RemoveRange(pts.Count, weights.Count - pts.Count);
    }

    // Profils par défaut
    private List<Vector2> SampledProfile(int n)
    {
        if (_profileControlPoints.Count >= 2)
            return ProfileSampler.Sample(_profileControlPoints, _profileClosed, _profileCurveKind, n, _splineDegree, _profileWeights);
        return _primitive switch
        {
            Primitive.Revolution => DefaultVase(n),
            Primitive.ExtrusionGeneralisee => DefaultCircleSmall(n),
            _ => DefaultCircle(n),
        };
    }

    private List<Vector2> SampledAme(int n)
    {
        if (_ameControlPoints.Count >= 2)
            return ProfileSampler.Sample(_ameControlPoints, _ameClosed, _profileCurveKind, n, _splineDegree, _ameWeights);
        return DefaultAme(n);
    }

    private static List<Vector2> DefaultCircle(int n)
    {
        var ctrl = new List<Vector2>();
        for (int k = 0; k < 12; k++)
        {
            float t = Mathf.Tau * k / 12.0f;
            ctrl.Add(new Vector2(Mathf.Cos(t) * 0.8f, Mathf.Sin(t) * 0.8f));
        }
        return Bezier.BezierCurve2D(ctrl, n, true);
    }

    private static List<Vector2> DefaultCircleSmall(int n)
    {
        var ctrl = new List<Vector2>();
        for (int k = 0; k < 12; k++)
        {
            float t = Mathf.Tau * k / 12.0f;
            ctrl.Add(new Vector2(Mathf.Cos(t) * 0.3f, Mathf.Sin(t) * 0.3f));
        }
        return Bezier.BezierCurve2D(ctrl, n, true);
    }

    private static List<Vector2> DefaultVase(int n)
    {
        var ctrl = new List<Vector2>
        {
            new(0.2f, 0.0f), new(0.8f, 0.2f), new(0.3f, 0.5f),
            new(0.6f, 0.9f), new(0.4f, 1.4f), new(0.0f, 1.7f),
        };
        return Bezier.BezierCurve2D(ctrl, n, false);
    }

    private static List<Vector2> DefaultAme(int n)
    {
        var ctrl = new List<Vector2>();
        for (int k = 0; k < 30; k++)
        {
            float t = Mathf.Tau * 2.0f * k / 29.0f;
            ctrl.Add(new Vector2(1.5f * Mathf.Cos(t), 1.5f * Mathf.Sin(t)));
        }
        return Bezier.BezierCurve2D(ctrl, n, false);
    }
}
