using System;
using System.Collections.Generic;
using Godot;

namespace MathsPower;

// Menu lumière (touche L). Mutuellement exclusif avec le menu caméra.
[GlobalClass]
public partial class LightMenu : Control
{
    [Export] public NodePath MainLightPath { get; set; } = new();
    [Export] public NodePath FillLightPath { get; set; } = new();
    [Export] public NodePath WorldEnvPath { get; set; } = new();
    [Export] public NodePath OtherMenuPath { get; set; } = new();

    private PanelContainer? _rootPanel;
    private readonly List<Label> _sectionLabels = new();
    private readonly List<Label> _textLabels = new();
    private readonly List<Label> _valueLabels = new();
    private readonly List<ColorRect> _sectionBars = new();

    private HSlider? _mainEnergy, _fillEnergy, _ambient;
    private ColorPickerButton? _mainColor, _fillColor, _ambientColor;
    private HBoxContainer? _softnessRow;
    private float _sunAzimuth = 40.0f;
    private float _sunElevation = 50.0f;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        BuildUi();
        ApplyTheme();
        ApplyRotation();
        SetMainColor(new Color(1.0f, 0.96f, 0.90f));
        SetFillColor(new Color(0.78f, 0.84f, 1.0f));
        SetAmbientColor(new Color(0.55f, 0.6f, 0.7f));
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.L)
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

        UiKit.SectionHeader(vbox, "Lumière  (L)", _sectionLabels, _sectionBars);

        var presets = new HBoxContainer();
        presets.AddThemeConstantOverride("separation", 6);
        foreach (var (label, action) in new (string, Action)[]
        {
            ("Studio", () => Preset(1.1f, 0.45f, 0.4f, true, new Color(1f, 0.98f, 0.95f), new Color(0.7f, 0.8f, 1f))),
            ("Dramatique", () => Preset(1.7f, 0.10f, 0.15f, true, new Color(1f, 0.82f, 0.55f), new Color(0.3f, 0.35f, 0.5f))),
            ("Plat", () => Preset(0.6f, 0.6f, 0.85f, false, new Color(1f, 1f, 1f), new Color(1f, 1f, 1f))),
        })
        {
            var btn = new Button { Text = label, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            Action captured = action;
            btn.Pressed += () => captured();
            presets.AddChild(btn);
        }
        vbox.AddChild(presets);

        Sep(vbox);
        UiKit.SectionHeader(vbox, "Lumière principale", _sectionLabels, _sectionBars);

        _mainColor = ColorRow(vbox, "couleur", new Color(1.0f, 0.96f, 0.90f), c => SetMainColor(c));
        (_mainEnergy, _, _) = Slider(vbox, "énergie", 0.0, 3.0, 0.05, 1.1, "Intensité de la lumière principale", v => SetMainEnergy((float)v));
        Slider(vbox, "azimut", 0.0, 360.0, 1.0, _sunAzimuth, "Orientation horizontale du soleil", v => { _sunAzimuth = (float)v; ApplyRotation(); });
        Slider(vbox, "élévation", 5.0, 89.0, 1.0, _sunElevation, "Hauteur du soleil au-dessus de l'horizon", v => { _sunElevation = (float)v; ApplyRotation(); });

        var shadows = new CheckBox { Text = "Ombres portées", ButtonPressed = true };
        shadows.Toggled += (bool p) =>
        {
            SetShadows(p);
            if (_softnessRow != null) _softnessRow.Visible = p;
        };
        vbox.AddChild(shadows);
        (_, _, _softnessRow) = Slider(vbox, "douceur", 0.0, 4.0, 0.1, 0.5, "Taille angulaire du soleil → pénombre douce", v => SetSoftness((float)v));

        Sep(vbox);
        UiKit.SectionHeader(vbox, "Ambiance", _sectionLabels, _sectionBars);

        _fillColor = ColorRow(vbox, "rempl.", new Color(0.78f, 0.84f, 1.0f), c => SetFillColor(c));
        (_fillEnergy, _, _) = Slider(vbox, "remplissage", 0.0, 1.5, 0.05, 0.45, "Lumière de remplissage (adoucit le côté sombre)", v => SetFillEnergy((float)v));
        _ambientColor = ColorRow(vbox, "ambiante", new Color(0.55f, 0.6f, 0.7f), c => SetAmbientColor(c));
        (_ambient, _, _) = Slider(vbox, "ambiante", 0.0, 1.5, 0.05, 0.4, "Lumière ambiante globale", v => SetAmbient((float)v));

        var ssao = new CheckBox { Text = "SSAO (occlusion)", ButtonPressed = true };
        ssao.Toggled += (bool p) => SetSsao(p);
        vbox.AddChild(ssao);
    }

    // Application
    private void SetMainEnergy(float v) { var l = GetMain(); if (l != null) l.LightEnergy = v; }
    private void SetMainColor(Color c) { var l = GetMain(); if (l != null) l.LightColor = c; }
    private void SetShadows(bool p) { var l = GetMain(); if (l != null) l.ShadowEnabled = p; }
    private void SetSoftness(float deg) { var l = GetMain(); if (l != null) l.LightAngularDistance = deg; }
    private void SetFillEnergy(float v) { var l = GetFill(); if (l != null) l.LightEnergy = v; }
    private void SetFillColor(Color c) { var l = GetFill(); if (l != null) l.LightColor = c; }
    private void SetAmbient(float v) { var env = GetEnv(); if (env != null) env.AmbientLightEnergy = v; }
    private void SetAmbientColor(Color c) { var env = GetEnv(); if (env != null) env.AmbientLightColor = c; }
    private void SetSsao(bool p) { var env = GetEnv(); if (env != null) env.SsaoEnabled = p; }

    private void ApplyRotation()
    {
        var l = GetMain();
        if (l != null) l.Rotation = new Vector3(Mathf.DegToRad(-_sunElevation), Mathf.DegToRad(_sunAzimuth), 0);
    }

    private void Preset(float main, float fill, float ambient, bool ssao, Color mainCol, Color fillCol)
    {
        if (_mainEnergy != null) _mainEnergy.SetValueNoSignal(main);
        if (_fillEnergy != null) _fillEnergy.SetValueNoSignal(fill);
        if (_ambient != null) _ambient.SetValueNoSignal(ambient);
        if (_mainColor != null) _mainColor.Color = mainCol;
        if (_fillColor != null) _fillColor.Color = fillCol;
        SetMainEnergy(main); SetFillEnergy(fill); SetAmbient(ambient); SetSsao(ssao);
        SetMainColor(mainCol); SetFillColor(fillCol);
    }

    // Helpers
    private (HSlider, Label, HBoxContainer) Slider(VBoxContainer parent, string label, double min, double max,
        double step, double initial, string tooltip, Action<double> onChanged) =>
        UiKit.SliderRow(parent, label, min, max, step, initial, tooltip, onChanged, _textLabels, _valueLabels, 84f);

    private ColorPickerButton ColorRow(VBoxContainer parent, string labelText, Color initial, Action<Color> onChanged)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        var name = new Label { Text = labelText, CustomMinimumSize = new Vector2(84, 0) };
        row.AddChild(name);
        _textLabels.Add(name);
        var picker = new ColorPickerButton
        {
            Color = initial, SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 26), EditAlpha = false,
        };
        picker.ColorChanged += (Color c) => onChanged(c);
        row.AddChild(picker);
        parent.AddChild(row);
        return picker;
    }

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
        StylePopup(_mainColor, palette);
        StylePopup(_fillColor, palette);
        StylePopup(_ambientColor, palette);
    }

    private static void StylePopup(ColorPickerButton? picker, Palette palette)
    {
        var popup = picker?.GetPopup();
        if (popup == null) return;
        var bg = new StyleBoxFlat { BgColor = palette.BgPanel, BorderColor = palette.Border };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(6);
        bg.SetContentMarginAll(8);
        popup.AddThemeStyleboxOverride("panel", bg);
    }

    private DirectionalLight3D? GetMain() => MainLightPath.IsEmpty ? null : GetNodeOrNull<DirectionalLight3D>(MainLightPath);
    private DirectionalLight3D? GetFill() => FillLightPath.IsEmpty ? null : GetNodeOrNull<DirectionalLight3D>(FillLightPath);
    private Godot.Environment? GetEnv() => WorldEnvPath.IsEmpty ? null : GetNodeOrNull<WorldEnvironment>(WorldEnvPath)?.Environment;
}
