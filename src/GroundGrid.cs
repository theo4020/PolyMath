using System.Collections.Generic;
using Godot;

namespace MathsPower;

// Grille de sol (plan y = 0 Godot) + axes monde colorés x/y/z.
[GlobalClass]
public partial class GroundGrid : MeshInstance3D
{
    private float _extent = 3.0f;
    private float _step = 0.5f;

    [Export] public float Extent { get => _extent; set { _extent = Mathf.Max(0.1f, value); Rebuild(); } }
    [Export] public float Step { get => _step; set { _step = Mathf.Max(0.05f, value); Rebuild(); } }

    public override void _Ready() => Rebuild();

    public void ApplyTheme() => Rebuild();

    private void Rebuild()
    {
        Color gridColor = Palette.Current().GroundGridLines;
        var axisX = new Color(0.94f, 0.38f, 0.38f);
        var axisY = new Color(0.47f, 0.86f, 0.47f);
        var axisZ = new Color(0.47f, 0.63f, 1.0f);

        var verts = new List<Vector3>();
        var colors = new List<Color>();

        float halfStep = _step * 0.5f;
        for (float t = -_extent; t <= _extent + 1e-6f; t += _step)
        {
            if (Mathf.Abs(t) > halfStep)
            {
                verts.Add(new Vector3(t, 0, -_extent));
                verts.Add(new Vector3(t, 0, _extent));
                colors.Add(gridColor); colors.Add(gridColor);
                verts.Add(new Vector3(-_extent, 0, t));
                verts.Add(new Vector3(_extent, 0, t));
                colors.Add(gridColor); colors.Add(gridColor);
            }
        }

        // Axe X (polycopié X = Godot X) — rouge.
        verts.Add(new Vector3(-_extent, 0, 0)); verts.Add(new Vector3(_extent, 0, 0));
        colors.Add(axisX); colors.Add(axisX);
        // Axe Y (polycopié Y = Godot Z) — vert.
        verts.Add(new Vector3(0, 0, -_extent)); verts.Add(new Vector3(0, 0, _extent));
        colors.Add(axisY); colors.Add(axisY);
        // Axe Z (polycopié Z = Godot Y) — bleu, vertical.
        verts.Add(new Vector3(0, -_extent * 0.25f, 0)); verts.Add(new Vector3(0, _extent, 0));
        colors.Add(axisZ); colors.Add(axisZ);

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true,
            VertexColorIsSrgb = true,
        };
        mesh.SurfaceSetMaterial(0, material);
        Mesh = mesh;
    }
}
