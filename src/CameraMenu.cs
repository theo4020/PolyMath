using System;
using System.Collections.Generic;
using Godot;

namespace MathsPower;

// Menu caméra (touche C). Mutuellement exclusif avec le menu lumière.
// Les réglages non pertinents se masquent selon le contexte.
[GlobalClass]
public partial class CameraMenu : Control
{
    [Export] public NodePath CameraPath { get; set; } = new();
    [Export] public NodePath OtherMenuPath { get; set; } = new();

    private PanelContainer? _rootPanel;
    private readonly List<Label> _sectionLabels = new();
    private readonly List<Label> _textLabels = new();
    private readonly List<Label> _valueLabels = new();
    private readonly List<ColorRect> _sectionBars = new();

    private HBoxContainer? _rotSpeedRow, _freeSpeedRow, _fovRow;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        BuildUi();
        ApplyTheme();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.C)
        {
            Visible = !Visible;
            if (Visible) { HideOther(); ApplyTheme(); }
            GetViewport().SetInputAsHandled();
        }
    }

    private void HideOther()
    {
        if (OtherMenuPath.IsEmpty) return;
        GetNodeOrNull<Control>(OtherMenuPath)?.Set("visible", false);
    }

    private void BuildUi()
    {
        _rootPanel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        _rootPanel.SetAnchorsPreset(LayoutPreset.TopWide);
        AddChild(_rootPanel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 12);
        _rootPanel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(vbox);

        UiKit.SectionHeader(vbox, "Caméra  (C)", _sectionLabels, _sectionBars);

        var modeOpt = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        modeOpt.AddItem("Orbite");
        modeOpt.AddItem("Libre (vol)");
        modeOpt.ItemSelected += (long i) =>
        {
            GetCamera()?.SetMode((int)i);
            if (_freeSpeedRow != null) _freeSpeedRow.Visible = i == 1;
        };
        vbox.AddChild(modeOpt);

        var projOpt = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        projOpt.AddItem("Orthographique");
        projOpt.AddItem("Perspective");
        projOpt.ItemSelected += (long i) =>
        {
            GetCamera()?.SetPerspective(i == 1);
            if (_fovRow != null) _fovRow.Visible = i == 1;
        };
        vbox.AddChild(projOpt);

        Sep(vbox);
        UiKit.SectionHeader(vbox, "Traveling", _sectionLabels, _sectionBars);

        var auto = new CheckBox { Text = "Rotation automatique" };
        auto.Toggled += (bool p) =>
        {
            GetCamera()?.SetAutoRotate(p);
            if (_rotSpeedRow != null) _rotSpeedRow.Visible = p;
        };
        vbox.AddChild(auto);
        (_, _, _rotSpeedRow) = Slider(vbox, "vitesse", 0.05, 2.0, 0.05, 0.4, "Vitesse de la rotation auto", v => GetCamera()?.SetAutoRotateSpeed((float)v));
        _rotSpeedRow.Visible = false;

        Sep(vbox);
        UiKit.SectionHeader(vbox, "Réglages", _sectionLabels, _sectionBars);
        (_, _, _freeSpeedRow) = Slider(vbox, "vit. vol", 0.5, 20.0, 0.5, 4.0, "Vitesse de déplacement en mode libre", v => GetCamera()?.SetMoveSpeed((float)v));
        _freeSpeedRow.Visible = false;
        (_, _, _fovRow) = Slider(vbox, "FOV", 20.0, 100.0, 1.0, 75.0, "Champ de vision (perspective)", v => GetCamera()?.SetFieldOfView((float)v));
        _fovRow.Visible = false;

        Sep(vbox);
        UiKit.SectionHeader(vbox, "Vues", _sectionLabels, _sectionBars);
        var views = new HBoxContainer();
        views.AddThemeConstantOverride("separation", 6);
        foreach (var (label, action) in new (string, Action)[]
        {
            ("Face", () => GetCamera()?.ViewFace()),
            ("Dessus", () => GetCamera()?.ViewTop()),
            ("3/4", () => GetCamera()?.ViewThreeQuarters()),
        })
        {
            var btn = new Button { Text = label, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            Action captured = action;
            btn.Pressed += () => captured();
            views.AddChild(btn);
        }
        vbox.AddChild(views);

        var reset = new Button { Text = "Réinitialiser", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        reset.Pressed += () => GetCamera()?.ResetView();
        vbox.AddChild(reset);

        var hint = new Label
        {
            Text = "Vol : ZQSD/WASD · Espace/Ctrl · Maj rapide · clic droit regarder",
            AutowrapMode = TextServer.AutowrapMode.Word,
        };
        hint.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(hint);
        _textLabels.Add(hint);
    }

    private (HSlider, Label, HBoxContainer) Slider(VBoxContainer parent, string label, double min, double max,
        double step, double initial, string tooltip, Action<double> onChanged) =>
        UiKit.SliderRow(parent, label, min, max, step, initial, tooltip, onChanged, _textLabels, _valueLabels, 70f);

    private static void Sep(VBoxContainer p) => p.AddChild(new HSeparator());

    private void ApplyTheme()
    {
        Palette palette = Palette.Current();
        if (_rootPanel != null)
            _rootPanel.AddThemeStyleboxOverride("panel", UiTheme.PanelStyle(palette, 6));
        foreach (var l in _sectionLabels) l.AddThemeColorOverride("font_color", palette.SectionLabel);
        foreach (var l in _textLabels) l.AddThemeColorOverride("font_color", palette.Text);
        foreach (var l in _valueLabels) l.AddThemeColorOverride("font_color", palette.TextDim);
        foreach (var b in _sectionBars) b.Color = palette.SectionLabel;
        if (_rootPanel != null) UiTheme.ApplyControls(_rootPanel, palette);
    }

    private OrbitCamera? GetCamera() => CameraPath.IsEmpty ? null : GetNodeOrNull<OrbitCamera>(CameraPath);
}
