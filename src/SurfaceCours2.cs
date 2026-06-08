using System.Collections.Generic;
using Godot;
using EnzoSurface = PolyMaths.Algorithms.BezierSurface;
using EnzoCurve = PolyMaths.Algorithms.BezierCurve;
using P3 = PolyMaths.Algorithms.Point3D;

namespace MathsPower;

public enum BezierAlgo { Direct = 0, DoubleCasteljau = 1 }
public enum NetMode { Bosse = 0, Selle = 1, Vague = 2, Plat = 3, Aleatoire = 4 }

public static class BezierAlgoExt
{
    public static string DisplayName(this BezierAlgo a) => a switch
    {
        BezierAlgo.Direct => "Produit tensoriel direct",
        BezierAlgo.DoubleCasteljau => "Double De Casteljau",
        _ => "?",
    };
}

// Cours 2 — Surface de Bézier. Le calcul utilise les algorithmes du module
// PolyMaths.Algorithms (BezierSurface.EvaluateDirect / EvaluateDoubleCasteljau
// / Subdivide), fournis par l'équipe.
[GlobalClass]
public partial class SurfaceCours2 : MeshInstance3D
{
    private bool _initialized;

    private BezierAlgo _algo = BezierAlgo.Direct;
    private NetMode _mode = NetMode.Bosse;
    private int _m = 3;
    private int _n = 3;
    private int _res = 30;
    private float _spread = 3.0f;
    private float _amplitude = 1.5f;
    private float _freqU = 1.0f;
    private float _freqV = 1.0f;
    private ulong _seed;
    private bool _showControlNet = true;
    private bool _showSubdivision;
    private bool _showWireframe = true;

    [Export] public BezierAlgo Algo { get => _algo; set { _algo = value; Rebuild(); } }
    [Export] public NetMode Mode { get => _mode; set { _mode = value; Regenerate(); } }
    [Export] public int M { get => _m; set { _m = Mathf.Clamp(value, 1, 8); Regenerate(); } }
    [Export] public int N { get => _n; set { _n = Mathf.Clamp(value, 1, 8); Regenerate(); } }
    [Export] public int Res { get => _res; set { _res = Mathf.Clamp(value, 6, 100); Rebuild(); } }
    [Export] public float Spread { get => _spread; set { _spread = Mathf.Max(1.0f, value); Regenerate(); } }
    [Export] public float Amplitude { get => _amplitude; set { _amplitude = value; Regenerate(); } }
    [Export] public float FreqU { get => _freqU; set { _freqU = value; Regenerate(); } }
    [Export] public float FreqV { get => _freqV; set { _freqV = value; Regenerate(); } }
    [Export] public bool ShowControlNet { get => _showControlNet; set { _showControlNet = value; Rebuild(); } }
    [Export] public bool ShowSubdivision { get => _showSubdivision; set { _showSubdivision = value; Rebuild(); } }
    [Export] public bool ShowWireframe { get => _showWireframe; set { _showWireframe = value; Rebuild(); } }

    private ControlNet _net = new(3, 3);

    public BezierAlgo CurrentAlgo => _algo;
    public NetMode CurrentMode => _mode;
    public int MValue => _m;
    public int NValue => _n;
    public int ResValue => _res;
    public float SpreadValue => _spread;
    public float AmplitudeValue => _amplitude;
    public bool ControlNetValue => _showControlNet;
    public bool SubdivisionValue => _showSubdivision;
    public bool WireframeValue => _showWireframe;

    private MeshInstance3D? _netNode;
    private MeshInstance3D? _subdivNode;
    private MeshInstance3D? _evalNode;
    private MeshInstance3D? _markerNode;
    private SurfaceShaderController? _shader;

    private float _u0 = 0.5f;
    private float _v0 = 0.5f;
    // Les courbes de construction sont masquées par défaut (surface lisible) ;
    // le marqueur B(u₀,v₀), lui, reste toujours visible pour que u₀/v₀ aient un
    // effet immédiat.
    private bool _showEval;

    public float U0Value => _u0;
    public float V0Value => _v0;
    public bool EvalOverlayValue => _showEval;

    public override void _Ready()
    {
        _shader = new SurfaceShaderController();
        EnsureChildren();
        _initialized = true;
        Regenerate();
    }

    public void SetRenderMode(int mode) => _shader?.SetMode(mode);
    public void SetShowGrid(bool show) => _shader?.SetShowGrid(show);
    public void SetGlossRoughness(float r) => _shader?.SetRoughness(r);
    public void SetLit(bool lit) => _shader?.SetLit(lit);

    public void SetU0(float u) { _u0 = Mathf.Clamp(u, 0f, 1f); UpdateEvalOverlay(); }
    public void SetV0(float v) { _v0 = Mathf.Clamp(v, 0f, 1f); UpdateEvalOverlay(); }
    public void SetShowEvalOverlay(bool show) { _showEval = show; UpdateEvalOverlay(); }

    public void ApplyTheme() => Rebuild();

    public void Reseed() { _seed++; Regenerate(); }

    private void EnsureChildren()
    {
        if (_netNode == null) { _netNode = new MeshInstance3D { Name = "ControlNet3D" }; AddChild(_netNode, false, InternalMode.Front); }
        if (_subdivNode == null) { _subdivNode = new MeshInstance3D { Name = "Subdiv3D" }; AddChild(_subdivNode, false, InternalMode.Front); }
        if (_evalNode == null) { _evalNode = new MeshInstance3D { Name = "EvalOverlay3D" }; AddChild(_evalNode, false, InternalMode.Front); }
        if (_markerNode == null) { _markerNode = new MeshInstance3D { Name = "EvalMarker3D" }; AddChild(_markerNode, false, InternalMode.Front); }
    }

    // Régénère le polyèdre selon le mode puis reconstruit la surface.
    private void Regenerate()
    {
        if (!_initialized) return;

        var net = new ControlNet(_m, _n);
        ulong rngState = _seed * 2654435761UL + 1UL;
        float Rand()
        {
            rngState = rngState * 6364136223846793005UL + 1442695040888963407UL;
            return (float)((rngState >> 32) / (double)(1UL << 32)) * 2.0f - 1.0f;
        }

        float half = _spread * 0.5f;
        for (int i = 0; i <= _m; i++)
        {
            for (int j = 0; j <= _n; j++)
            {
                float x = -half + _spread * i / _m;
                float y = -half + _spread * j / _n;
                float u = i / (float)_m;
                float v = j / (float)_n;
                float z = _mode switch
                {
                    NetMode.Plat => 0.0f,
                    NetMode.Bosse => _amplitude * Mathf.Exp(-((x * x + y * y) / (half * half + 1e-6f))),
                    NetMode.Selle => _amplitude * (x * x - y * y) / Mathf.Max(_spread, 1e-6f),
                    // Vraie ondulation : somme de deux sinus perpendiculaires
                    // (motif « boîte d'œufs » / vagues), période réglée par freq.
                    NetMode.Vague => _amplitude * 0.5f *
                        (Mathf.Sin(Mathf.Tau * _freqU * u) + Mathf.Sin(Mathf.Tau * _freqV * v)),
                    NetMode.Aleatoire => _amplitude * 0.5f * Rand(),
                    _ => 0.0f,
                };
                net.Set(i, j, new Vector3(x, y, z));
            }
        }
        _net = net;
        Rebuild();
    }

    private void Rebuild()
    {
        if (!_initialized) return;

        // Surface via les algorithmes de PolyMaths.Algorithms
        var surf = new EnzoSurface(_net.ToPoint3DArray());
        float denom = Mathf.Max(_res - 1, 1);
        var grid = new SurfaceGrid(_res - 1, _res - 1);
        for (int k = 0; k < _res; k++)
        {
            float u = k / denom;
            for (int l = 0; l < _res; l++)
            {
                float v = l / denom;
                P3 p = _algo == BezierAlgo.Direct
                    ? surf.EvaluateDirect(u, v)
                    : surf.EvaluateDoubleCasteljau(u, v);
                grid.Set(k, l, new Vector3(p.x, p.y, p.z));
            }
        }
        grid.ComputeNormals(false);
        Mesh = MeshBuilder.SurfaceGridToMesh(grid, _showWireframe, _shader?.Material);

        // Polyèdre de contrôle
        if (_netNode != null)
        {
            if (_showControlNet && !_showSubdivision)
            {
                var mesh = new ArrayMesh();
                AppendNetLines(mesh, _net, new Color(1.0f, 0.72f, 0.25f));
                _netNode.Mesh = mesh;
                _netNode.Visible = true;
            }
            else { _netNode.Visible = false; }
        }

        // Subdivision en 4 (Subdivide() de l'équipe)
        if (_subdivNode != null)
        {
            if (_showSubdivision)
            {
                P3[][,] quads = surf.Subdivide(); // {NW, NE, SW, SE}
                Color[] cols =
                {
                    new(1.0f, 0.45f, 0.35f),
                    new(0.45f, 0.85f, 0.45f),
                    new(0.45f, 0.65f, 1.0f),
                    new(0.95f, 0.85f, 0.35f),
                };
                var combined = new ArrayMesh();
                for (int q = 0; q < quads.Length; q++)
                    AppendNetLinesArray(combined, quads[q], cols[q]);
                _subdivNode.Mesh = combined;
                _subdivNode.Visible = true;
            }
            else { _subdivNode.Visible = false; }
        }

        UpdateEvalOverlay();
    }

    // Visualise l'évaluation B(u₀,v₀). Le marqueur rouge est toujours affiché
    // (u₀/v₀ ont donc un effet immédiat) ; les courbes de construction (les
    // c_i(v), le polygone intermédiaire et la courbe finale) ne s'affichent que
    // si l'overlay est activé.
    private void UpdateEvalOverlay()
    {
        if (_evalNode == null || _markerNode == null) return;

        EnzoCurve RowCurve(int row)
        {
            var curve = new EnzoCurve();
            for (int j = 0; j <= _net.N; j++)
            {
                var p = _net.Get(row, j);
                curve.AddPoint3D(new P3(p.X, p.Y, p.Z));
            }
            return curve;
        }

        // Points intermédiaires c_i(v₀) → courbe finale B(u,v₀), sur laquelle
        // glisse le marqueur quand u₀ change.
        var intermediatePoints = new List<P3>(_net.M + 1);
        for (int i = 0; i <= _net.M; i++)
            intermediatePoints.Add(RowCurve(i).EvaluateCasteljau3D(_v0));
        var finalCurve = new EnzoCurve();
        foreach (var p in intermediatePoints) finalCurve.AddPoint3D(p);

        // Marqueur B(u₀,v₀) — toujours visible.
        P3 markerPoint = finalCurve.EvaluateCasteljau3D(_u0);
        _markerNode.Mesh = MeshBuilder.MarkerSphere(0.06f, new Color(0.95f, 0.25f, 0.2f));
        _markerNode.Position = ToGodot(markerPoint);
        _markerNode.Visible = true;

        if (!_showEval)
        {
            _evalNode.Visible = false;
            return;
        }

        var rowColor = new Color(0.30f, 0.80f, 0.40f);
        var polygonColor = new Color(1.0f, 0.65f, 0.20f);
        var finalColor = new Color(0.55f, 1.0f, 0.55f);
        var mesh = new ArrayMesh();
        const int samples = 48;

        // Les m+1 courbes c_i(v).
        for (int i = 0; i <= _net.M; i++)
        {
            var curve = RowCurve(i);
            var pts = new Vector3[samples];
            for (int s = 0; s < samples; s++)
                pts[s] = ToGodot(curve.EvaluateCasteljau3D(s / (float)(samples - 1)));
            MeshBuilder.AppendTube(mesh, pts, 0.010f, rowColor, 5);
        }

        // Polygone reliant les points intermédiaires c_i(v₀).
        var polygon = new Vector3[intermediatePoints.Count];
        for (int i = 0; i < intermediatePoints.Count; i++)
            polygon[i] = ToGodot(intermediatePoints[i]);
        MeshBuilder.AppendTube(mesh, polygon, 0.012f, polygonColor, 5);

        // Courbe finale B(u,v₀).
        var finalPts = new Vector3[samples];
        for (int s = 0; s < samples; s++)
            finalPts[s] = ToGodot(finalCurve.EvaluateCasteljau3D(s / (float)(samples - 1)));
        MeshBuilder.AppendTube(mesh, finalPts, 0.018f, finalColor, 6);

        _evalNode.Mesh = mesh;
        _evalNode.Visible = mesh.GetSurfaceCount() > 0;
    }

    // Conversion repère math (z vers le haut) → Godot (y vers le haut).
    private static Vector3 ToGodot(P3 p) => new(p.x, p.z, -p.y);

    // Trace les arêtes d'un polyèdre de contrôle (lignes horizontales + verticales).
    private static void AppendNetLines(ArrayMesh mesh, ControlNet net, Color color)
    {
        var verts = new List<Vector3>();
        Vector3 ToScene(int i, int j) { var p = net.Get(i, j); return new Vector3(p.X, p.Z, -p.Y); }
        for (int j = 0; j <= net.N; j++)
            for (int i = 0; i < net.M; i++) { verts.Add(ToScene(i, j)); verts.Add(ToScene(i + 1, j)); }
        for (int i = 0; i <= net.M; i++)
            for (int j = 0; j < net.N; j++) { verts.Add(ToScene(i, j)); verts.Add(ToScene(i, j + 1)); }
        EmitLines(mesh, verts, color);
    }

    private static void AppendNetLinesArray(ArrayMesh mesh, P3[,] net, Color color)
    {
        int rows = net.GetLength(0), cols = net.GetLength(1);
        var verts = new List<Vector3>();
        Vector3 ToScene(int i, int j) { var p = net[i, j]; return new Vector3(p.x, p.z, -p.y); }
        for (int j = 0; j < cols; j++)
            for (int i = 0; i < rows - 1; i++) { verts.Add(ToScene(i, j)); verts.Add(ToScene(i + 1, j)); }
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols - 1; j++) { verts.Add(ToScene(i, j)); verts.Add(ToScene(i, j + 1)); }
        EmitLines(mesh, verts, color);
    }

    private static void EmitLines(ArrayMesh mesh, List<Vector3> verts, Color color)
    {
        var colors = new Color[verts.Count];
        for (int k = 0; k < colors.Length; k++) colors[k] = color;
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = colors;
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
        int idx = mesh.GetSurfaceCount() - 1;
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true,
            NoDepthTest = true,
            AlbedoColor = color,
        };
        mesh.SurfaceSetMaterial(idx, material);
    }
}
