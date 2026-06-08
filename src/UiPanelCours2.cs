using System;
using System.Collections.Generic;
using Godot;

namespace MathsPower;

// Panneau latéral de la Partie 2 (surfaces de Bézier).
[GlobalClass]
public partial class UiPanelCours2 : Control
{
    [Export] public NodePath SurfacePath { get; set; } = new();
    [Export] public NodePath CameraPath { get; set; } = new();

    private PanelContainer? _rootPanel;
    private readonly List<Label> _sectionLabels = new();
    private readonly List<Label> _textLabels = new();
    private readonly List<Label> _valueLabels = new();
    private readonly List<ColorRect> _sectionBars = new();

    private HBoxContainer? _freqURow, _freqVRow, _rugositeRow;
    private CheckBox? _gridCheck, _netCheck;
    private Button? _reseedButton;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        BuildUi();
        ApplyModeVisibility(GetSurface()?.CurrentMode ?? NetMode.Bosse);
        ApplyThemeInternal();
    }

    private void BuildUi()
    {
        _rootPanel = new PanelContainer();
        _rootPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_rootPanel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 14);
        _rootPanel.AddChild(margin);

        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        margin.AddChild(scroll);

        var vbox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        vbox.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(vbox);

        // Algorithme (menu déroulant) — méthodes de PolyMaths.Algorithms.
        PushSectionLabel(vbox, "Algorithme");
        var algoOpt = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var name in new[] { "Produit tensoriel direct", "Double De Casteljau" })
            algoOpt.AddItem(name);
        algoOpt.ItemSelected += (long idx) => { var s = GetSurface(); if (s != null) s.Algo = (BezierAlgo)(int)idx; };
        vbox.AddChild(algoOpt);
        var algoNote = new Label
        {
            Text = "Même surface dans les deux cas — l'overlay vert montre la construction (courbes de lignes c_i(v)).",
            AutowrapMode = TextServer.AutowrapMode.Word,
        };
        algoNote.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(algoNote);
        _textLabels.Add(algoNote);

        PushSeparator(vbox);

        // Forme du polyèdre (menu déroulant).
        PushSectionLabel(vbox, "Forme du polyèdre");
        var modeOpt = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var name in new[] { "Bosse", "Selle", "Vague", "Plat", "Aléatoire" })
            modeOpt.AddItem(name);
        modeOpt.ItemSelected += (long idx) =>
        {
            var s = GetSurface();
            if (s != null) s.Mode = (NetMode)(int)idx;
            ApplyModeVisibility((NetMode)(int)idx);
        };
        vbox.AddChild(modeOpt);
        _reseedButton = new Button { Text = "Re-tirer le bruit", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _reseedButton.Pressed += () => GetSurface()?.Reseed();
        vbox.AddChild(_reseedButton);

        PushSeparator(vbox);

        // Degrés et résolution.
        PushSectionLabel(vbox, "Degrés et résolution");
        AddSliderRow(vbox, "m", 1, 8, 1, 3, "Degré en u", v => { var s = GetSurface(); if (s != null) s.M = (int)v; }, out _);
        AddSliderRow(vbox, "n", 1, 8, 1, 3, "Degré en v", v => { var s = GetSurface(); if (s != null) s.N = (int)v; }, out _);
        AddSliderRow(vbox, "résolution", 6, 100, 1, 30, "Échantillons par axe", v => { var s = GetSurface(); if (s != null) s.Res = (int)v; }, out _);

        PushSeparator(vbox);

        // Forme.
        PushSectionLabel(vbox, "Paramètres de forme");
        AddSliderRow(vbox, "étalement", 1.0, 6.0, 0.1, 3.0, "Largeur du polyèdre", v => { var s = GetSurface(); if (s != null) s.Spread = (float)v; }, out _);
        AddSliderRow(vbox, "amplitude", 0.0, 3.0, 0.05, 1.5, "Hauteur du relief", v => { var s = GetSurface(); if (s != null) s.Amplitude = (float)v; }, out _);
        AddSliderRow(vbox, "fréq u", 0.5, 4.0, 0.1, 1.0, "Fréquence en u (mode Vague)", v => { var s = GetSurface(); if (s != null) s.FreqU = (float)v; }, out _freqURow);
        AddSliderRow(vbox, "fréq v", 0.5, 4.0, 0.1, 1.0, "Fréquence en v (mode Vague)", v => { var s = GetSurface(); if (s != null) s.FreqV = (float)v; }, out _freqVRow);

        PushSeparator(vbox);

        // Évaluation B(u₀, v₀) — overlay visualisant l'algorithme.
        PushSectionLabel(vbox, "Évaluation B(u₀, v₀)");
        AddCheckbox(vbox, "Overlay d'évaluation", false, p => GetSurface()?.SetShowEvalOverlay(p));
        AddSliderRow(vbox, "u₀", 0.0, 1.0, 0.01, 0.5, "Position du marqueur en u", v => GetSurface()?.SetU0((float)v), out _);
        AddSliderRow(vbox, "v₀", 0.0, 1.0, 0.01, 0.5, "Position du marqueur en v", v => GetSurface()?.SetV0((float)v), out _);

        PushSeparator(vbox);

        // Rendu.
        PushSectionLabel(vbox, "Rendu");
        var renderOpt = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var name in new[] { "Viridis lisse", "Damier UV", "Topographique", "Brillant", "Iridescent" })
            renderOpt.AddItem(name);
        renderOpt.ItemSelected += (long idx) =>
        {
            GetSurface()?.SetRenderMode((int)idx);
            if (_rugositeRow != null) _rugositeRow.Visible = idx == 3;
            if (_gridCheck != null) _gridCheck.Visible = idx != 1;
        };
        vbox.AddChild(renderOpt);
        AddCheckbox(vbox, "Éclairage", true, p => GetSurface()?.SetLit(p));
        _gridCheck = AddCheckbox(vbox, "Grille de paramétrage (u,v)", false, p => GetSurface()?.SetShowGrid(p));
        AddSliderRow(vbox, "rugosité", 0.0, 1.0, 0.02, 0.55, "Rugosité du matériau brillant",
            v => GetSurface()?.SetGlossRoughness((float)v), out _rugositeRow);
        _rugositeRow.Visible = false;

        PushSeparator(vbox);

        // Affichage.
        PushSectionLabel(vbox, "Affichage");
        _netCheck = AddCheckbox(vbox, "Polyèdre de contrôle", true, p => { var s = GetSurface(); if (s != null) s.ShowControlNet = p; });
        AddCheckbox(vbox, "Subdivision en 4", false, p =>
        {
            var s = GetSurface();
            if (s != null) s.ShowSubdivision = p;
            // La subdivision remplace l'affichage du polyèdre simple.
            if (_netCheck != null) _netCheck.Visible = !p;
        });
        AddCheckbox(vbox, "Wireframe surface", true, p => { var s = GetSurface(); if (s != null) s.ShowWireframe = p; });

        var presets = new HBoxContainer();
        presets.AddThemeConstantOverride("separation", 6);
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
            presets.AddChild(btn);
        }
        vbox.AddChild(presets);
    }

    private void ApplyModeVisibility(NetMode mode)
    {
        bool isVague = mode == NetMode.Vague;
        bool isRandom = mode == NetMode.Aleatoire;
        if (_freqURow != null) _freqURow.Visible = isVague;
        if (_freqVRow != null) _freqVRow.Visible = isVague;
        if (_reseedButton != null) _reseedButton.Visible = isRandom;
    }

    // Helpers UI
    private void AddSliderRow(VBoxContainer parent, string labelText, double min, double max,
        double step, double initial, string tooltip, Action<double> onChanged, out HBoxContainer row)
    {
        row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        var name = new Label { Text = labelText, CustomMinimumSize = new Vector2(72, 0), TooltipText = tooltip };
        row.AddChild(name);
        _textLabels.Add(name);

        var slider = new HSlider { MinValue = min, MaxValue = max, Step = step,
            TooltipText = tooltip, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        slider.SetValueNoSignal(initial);
        var valueLabel = new Label
        {
            Text = step >= 1.0 ? ((int)initial).ToString() : initial.ToString("0.00"),
            CustomMinimumSize = new Vector2(44, 0),
        };
        bool isInt = step >= 1.0;
        slider.ValueChanged += (double v) =>
        {
            valueLabel.Text = isInt ? ((int)v).ToString() : v.ToString("0.00");
            onChanged(v);
        };
        row.AddChild(slider);
        row.AddChild(valueLabel);
        _valueLabels.Add(valueLabel);

        parent.AddChild(row);
    }

    private CheckBox AddCheckbox(VBoxContainer parent, string label, bool initial, Action<bool> onToggled)
    {
        var cb = new CheckBox { Text = label, ButtonPressed = initial };
        cb.Toggled += (bool p) => onToggled(p);
        parent.AddChild(cb);
        return cb;
    }

    private void PushSectionLabel(VBoxContainer parent, string text)
        => UiKit.SectionHeader(parent, text, _sectionLabels, _sectionBars);

    private static void PushSeparator(VBoxContainer parent) => parent.AddChild(new HSeparator());

    // Thème
    public void ApplyTheme()
    {
        ApplyThemeInternal();
        GetSurface()?.ApplyTheme();
    }

    private void ApplyThemeInternal()
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

    private SurfaceCours2? GetSurface() => SurfacePath.IsEmpty ? null : GetNodeOrNull<SurfaceCours2>(SurfacePath);
    private OrbitCamera? GetCamera() => CameraPath.IsEmpty ? null : GetNodeOrNull<OrbitCamera>(CameraPath);
}
