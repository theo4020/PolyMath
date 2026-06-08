using Godot;

namespace MathsPower;

// Encapsule le ShaderMaterial procédural de surface. Partagé par les nœuds
// SurfaceCours1 / SurfaceCours2 : le mesh est reconstruit à chaque frame de
// paramètre, mais le matériau (et ses uniformes) persiste.
public sealed class SurfaceShaderController
{
    public ShaderMaterial Material { get; }

    public SurfaceShaderController()
    {
        var shader = GD.Load<Shader>("res://shaders/surface.gdshader");
        Material = new ShaderMaterial { Shader = shader };
        SetMode(0);
        SetShowGrid(false);
        SetRoughness(0.55f);
        SetMetallic(0.0f);
        SetLit(true);
    }

    public void SetMode(int mode) => Material.SetShaderParameter("render_mode_id", mode);
    public void SetShowGrid(bool show) => Material.SetShaderParameter("show_grid", show);
    public void SetRoughness(float r) => Material.SetShaderParameter("roughness_val", r);
    public void SetMetallic(float m) => Material.SetShaderParameter("metallic_val", m);
    public void SetLit(bool lit) => Material.SetShaderParameter("lit", lit);
}
