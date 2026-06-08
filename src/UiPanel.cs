using System;
using System.Collections.Generic;
using Godot;

namespace MathsPower;

// Panneau latéral gauche : liste de primitives, sliders, presets, vue.
[GlobalClass]
public partial class UiPanel : Control
{
    [Export] public NodePath SurfacePath { get; set; } = new();
    [Export] public NodePath CameraPath { get; set; } = new();
    [Export] public NodePath GroundGridPath { get; set; } = new();
    [Export] public NodePath ProfileEditorPath { get; set; } = new();
    [Export] public NodePath AmeEditorPath { get; set; } = new();
    [Export] public NodePath WorldEnvPath { get; set; } = new();

    private PanelContainer? _rootPanel;

    private OptionButton? _primitiveOpt;
    private CheckBox? _wireframeCheckbox;
    private CheckBox? _groundCheckbox;

    private HSlider? _subUSlider, _subVSlider, _radiusSlider, _heightSlider, _pCoefSlider, _ameAmpSlider, _ameFreqSlider;
    private Label? _subULabel, _subVLabel, _radiusLabel, _heightLabel, _pCoefLabel, _ameAmpLabel, _ameFreqLabel;

    private HBoxContainer? _radiusRow, _heightRow, _pCoefRow, _ameAmpRow, _ameFreqRow;
    private HBoxContainer? _degreeRow;
    private Label? _nurbsHint;
    private HBoxContainer? _paramsSectionRow;
    private VBoxContainer? _profilePresetsBlock, _amePresetsBlock, _curveKindBlock;

    private HBoxContainer? _rugositeRow;
    private CheckBox? _gridCheck;

    private Label? _titleLabel;

    private readonly List<Label> _sectionLabels = new();
    private readonly List<Label> _textLabels = new();
    private readonly List<Label> _valueLabels = new();
    private readonly List<ColorRect> _sectionBars = new();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        BuildUi();
        SyncFromTarget();
        var prim = GetTarget()?.CurrentPrimitive ?? Primitive.Sphere;
        ApplyPrimitiveVisibility(prim);
        UpdateTitle();
        ApplyThemeInternal();
    }

    // Construction
    private void BuildUi()
    {
        _rootPanel = new PanelContainer();
        _rootPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_rootPanel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 14);
        _rootPanel.AddChild(margin);

        // ScrollContainer : le contenu défile verticalement si le panneau
        // dépasse la hauteur de la fenêtre.
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        margin.AddChild(scroll);

        var vbox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        vbox.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(vbox);

        // Primitive (menu déroulant).
        PushSectionLabel(vbox, "Primitive");
        _primitiveOpt = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var name in new[] { "Sphère", "Cylindre", "Cône", "Extrusion simple", "Révolution", "Extrusion généralisée" })
            _primitiveOpt.AddItem(name);
        _primitiveOpt.ItemSelected += (long idx) => OnPrimitivePressed((Primitive)(int)idx);
        vbox.AddChild(_primitiveOpt);

        PushSeparator(vbox);

        // Maillage.
        PushSectionLabel(vbox, "Maillage");
        (_subUSlider, _subULabel, _) = AddSliderRow(vbox, "m", 4, 200, 1, 60,
            "Subdivisions sur le 1ᵉʳ paramètre", v => OnSubU(v));
        (_subVSlider, _subVLabel, _) = AddSliderRow(vbox, "p", 3, 200, 1, 30,
            "Subdivisions sur le 2ᵉ paramètre", v => OnSubV(v));

        PushSeparator(vbox);

        // Paramètres.
        _paramsSectionRow = UiKit.SectionHeader(vbox, "Paramètres", _sectionLabels, _sectionBars);

        (_radiusSlider, _radiusLabel, _radiusRow) = AddSliderRow(vbox, "R", 0.1, 3.0, 0.05, 1.0,
            "Rayon", v => OnRadius(v));
        (_heightSlider, _heightLabel, _heightRow) = AddSliderRow(vbox, "H", 0.2, 5.0, 0.05, 2.0,
            "Hauteur", v => OnHeight(v));
        (_pCoefSlider, _pCoefLabel, _pCoefRow) = AddSliderRow(vbox, "coef p", 0.05, 3.0, 0.05, 0.5,
            "Rapport rayon-sommet / rayon-base", v => OnPCoef(v));
        (_ameAmpSlider, _ameAmpLabel, _ameAmpRow) = AddSliderRow(vbox, "âme z amp", 0.0, 2.0, 0.01, 0.0,
            "Amplitude du relief vertical de l'âme", v => OnAmeAmp(v));
        (_ameFreqSlider, _ameFreqLabel, _ameFreqRow) = AddSliderRow(vbox, "âme z freq", 0.0, 5.0, 0.05, 1.0,
            "Fréquence du relief", v => OnAmeFreq(v));

        PushSeparator(vbox);

        // Type de courbe du profil (Bézier / B-spline / NURBS / Polygone).
        _curveKindBlock = new VBoxContainer();
        _curveKindBlock.AddThemeConstantOverride("separation", 6);
        UiKit.SectionHeader(_curveKindBlock, "Type de courbe", _sectionLabels, _sectionBars);
        var curveOpt = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var name in new[] { "Bézier", "B-spline", "NURBS", "Polygone" })
            curveOpt.AddItem(name);
        curveOpt.ItemSelected += (long idx) =>
        {
            GetTarget()?.SetProfileCurveKind((int)idx);
            // Le degré concerne B-spline (1) et NURBS (2) ; le poids, la NURBS seule.
            if (_degreeRow != null) _degreeRow.Visible = idx is 1 or 2;
            if (_nurbsHint != null) _nurbsHint.Visible = idx == 2;
        };
        _curveKindBlock.AddChild(curveOpt);
        (_, _, _degreeRow) = AddSliderRow(_curveKindBlock, "degré", 1, 7, 1, 3,
            "Degré de la B-spline / NURBS", v => GetTarget()?.SetSplineDegree((int)v));
        _degreeRow.Visible = false;
        _nurbsHint = new Label
        {
            Text = "Poids : molette sur un point (Maj = pas fin) · double-clic = remettre à 1.",
            AutowrapMode = TextServer.AutowrapMode.Word,
            Visible = false,
        };
        _nurbsHint.AddThemeFontSizeOverride("font_size", 11);
        _curveKindBlock.AddChild(_nurbsHint);
        _textLabels.Add(_nurbsHint);
        vbox.AddChild(_curveKindBlock);

        // Presets.
        _profilePresetsBlock = BuildPresetBlock(vbox, "Presets de profil", new (string, Action, string)[]
        {
            ("Cercle", () => GetTarget()?.PresetProfileCircle(), "Cercle fermé"),
            ("Vase", () => GetTarget()?.PresetProfileVase(), "Profil ouvert"),
            ("Étoile", () => GetTarget()?.PresetProfileStar(), "Étoile fermée"),
            ("Effacer", () => GetTarget()?.PresetProfileClear(), "Repasser au défaut"),
        });
        _amePresetsBlock = BuildPresetBlock(vbox, "Presets d'âme", new (string, Action, string)[]
        {
            ("Hélice", () => GetTarget()?.PresetAmeHelice(), "Trajectoire spirale"),
            ("Droite", () => GetTarget()?.PresetAmeStraight(), "Trajectoire rectiligne"),
            ("Effacer", () => GetTarget()?.PresetAmeClear(), "Repasser à l'hélice"),
        });

        PushSeparator(vbox);

        // Rendu (texture procédurale + matériau).
        PushSectionLabel(vbox, "Rendu");
        var renderOpt = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var name in new[] { "Viridis lisse", "Damier UV", "Topographique", "Brillant", "Iridescent" })
            renderOpt.AddItem(name);
        renderOpt.ItemSelected += (long idx) =>
        {
            GetTarget()?.SetRenderMode((int)idx);
            // Contextuel : rugosité seulement en Brillant, grille inutile en Damier.
            if (_rugositeRow != null) _rugositeRow.Visible = idx == 3;
            if (_gridCheck != null) _gridCheck.Visible = idx != 1;
        };
        vbox.AddChild(renderOpt);

        var lightingCheck = new CheckBox { Text = "Éclairage", ButtonPressed = true, TooltipText = "Surface éclairée (sinon aplat de couleur, sans éclairage)" };
        lightingCheck.Toggled += (bool p) => GetTarget()?.SetLit(p);
        vbox.AddChild(lightingCheck);

        _gridCheck = new CheckBox { Text = "Grille de paramétrage (u,v)", TooltipText = "Superpose les isolignes du paramétrage" };
        _gridCheck.Toggled += (bool p) => GetTarget()?.SetShowGrid(p);
        vbox.AddChild(_gridCheck);

        (_, _, _rugositeRow) = AddSliderRow(vbox, "rugosité", 0.0, 1.0, 0.02, 0.55,
            "Rugosité du matériau brillant", v => GetTarget()?.SetGlossRoughness((float)v));
        _rugositeRow.Visible = false; // mode Viridis au départ

        PushSeparator(vbox);

        // Vue.
        PushSectionLabel(vbox, "Vue");
        _wireframeCheckbox = new CheckBox { Text = "Wireframe", ButtonPressed = true, TooltipText = "Afficher les arêtes du maillage" };
        _wireframeCheckbox.Toggled += OnWireframeToggled;
        vbox.AddChild(_wireframeCheckbox);

        _groundCheckbox = new CheckBox { Text = "Grille de sol", ButtonPressed = true, TooltipText = "Plan z = 0 + axes monde" };
        _groundCheckbox.Toggled += OnGroundToggled;
        vbox.AddChild(_groundCheckbox);

        var presets = new HBoxContainer();
        presets.AddThemeConstantOverride("separation", 6);
        foreach (var (label, action, tip) in new (string, Action, string)[]
        {
            ("Face", () => GetCamera()?.ViewFace(), "Vue de face"),
            ("Dessus", () => GetCamera()?.ViewTop(), "Vue de dessus"),
            ("3/4", () => GetCamera()?.ViewThreeQuarters(), "Vue trois-quarts"),
        })
        {
            var btn = new Button { Text = label, TooltipText = tip, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            Action captured = action;
            btn.Pressed += () => captured();
            presets.AddChild(btn);
        }
        vbox.AddChild(presets);

        // Cartouche de titre (hors panneau). Ajout différé : le parent est encore
        // en train d'instancier ses enfants pendant _Ready.
        _titleLabel = new Label { Text = "", Position = new Vector2(300, 18) };
        GetParent()?.CallDeferred(Node.MethodName.AddChild, _titleLabel);
    }

    private (HSlider, Label, HBoxContainer) AddSliderRow(
        VBoxContainer parent, string labelText, double min, double max, double step,
        double initial, string tooltip, Action<double> onChanged)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        var name = new Label { Text = labelText, CustomMinimumSize = new Vector2(64, 0), TooltipText = tooltip };
        row.AddChild(name);
        _textLabels.Add(name);

        var slider = new HSlider
        {
            MinValue = min, MaxValue = max, Step = step,
            TooltipText = tooltip, SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        slider.SetValueNoSignal(initial);
        slider.ValueChanged += (double v) => onChanged(v);
        row.AddChild(slider);

        var value = new Label
        {
            Text = step >= 1.0 ? ((int)initial).ToString() : initial.ToString("0.00"),
            CustomMinimumSize = new Vector2(48, 0),
        };
        row.AddChild(value);
        _valueLabels.Add(value);

        parent.AddChild(row);
        return (slider, value, row);
    }

    private VBoxContainer BuildPresetBlock(VBoxContainer parent, string title, (string, Action, string)[] buttons)
    {
        var block = new VBoxContainer();
        block.AddThemeConstantOverride("separation", 6);

        UiKit.SectionHeader(block, title, _sectionLabels, _sectionBars);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        foreach (var (text, action, tip) in buttons)
        {
            var btn = new Button { Text = text, TooltipText = tip, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            Action captured = action;
            btn.Pressed += () => captured();
            row.AddChild(btn);
        }
        block.AddChild(row);
        parent.AddChild(block);
        return block;
    }

    private void PushSectionLabel(VBoxContainer parent, string text)
        => UiKit.SectionHeader(parent, text, _sectionLabels, _sectionBars);

    private static void PushSeparator(VBoxContainer parent) => parent.AddChild(new HSeparator());

    // Synchro + visibilité
    private void SyncFromTarget()
    {
        var t = GetTarget();
        if (t == null) return;
        var current = t.CurrentPrimitive;
        if (_primitiveOpt != null) _primitiveOpt.Selected = (int)current;
        SetSlider(_subUSlider, _subULabel, t.SubU);
        SetSlider(_subVSlider, _subVLabel, t.SubV);
        SetSlider(_radiusSlider, _radiusLabel, t.RadiusValue);
        SetSlider(_heightSlider, _heightLabel, t.HeightValue);
        SetSlider(_pCoefSlider, _pCoefLabel, t.PCoefValue);
        SetSlider(_ameAmpSlider, _ameAmpLabel, t.AmeZAmpValue);
        SetSlider(_ameFreqSlider, _ameFreqLabel, t.AmeZFreqValue);
        if (_wireframeCheckbox != null) _wireframeCheckbox.ButtonPressed = t.WireframeValue;
    }

    private void ApplyPrimitiveVisibility(Primitive prim)
    {
        bool showR = prim is Primitive.Sphere or Primitive.Cylinder or Primitive.Cone;
        bool showH = prim is Primitive.Cylinder or Primitive.Cone or Primitive.ExtrusionSimple;
        bool showPCoef = prim == Primitive.ExtrusionSimple;
        bool showAme = prim == Primitive.ExtrusionGeneralisee;
        bool showProfilePresets = prim.UsesProfile();

        if (_radiusRow != null) _radiusRow.Visible = showR;
        if (_heightRow != null) _heightRow.Visible = showH;
        if (_pCoefRow != null) _pCoefRow.Visible = showPCoef;
        if (_ameAmpRow != null) _ameAmpRow.Visible = showAme;
        if (_ameFreqRow != null) _ameFreqRow.Visible = showAme;
        if (_paramsSectionRow != null) _paramsSectionRow.Visible = showR || showH || showPCoef || showAme;
        if (_profilePresetsBlock != null) _profilePresetsBlock.Visible = showProfilePresets;
        if (_amePresetsBlock != null) _amePresetsBlock.Visible = showAme;
        if (_curveKindBlock != null) _curveKindBlock.Visible = showProfilePresets;

        SetControlVisible(ProfileEditorPath, showProfilePresets);
        SetControlVisible(AmeEditorPath, showAme);
    }

    private void UpdateTitle()
    {
        var t = GetTarget();
        if (t == null) { if (_titleLabel != null) _titleLabel.Text = ""; return; }
        var prim = t.CurrentPrimitive;
        string info = prim switch
        {
            Primitive.Sphere => $"R = {t.RadiusValue:0.00}",
            Primitive.Cylinder or Primitive.Cone => $"R = {t.RadiusValue:0.00} · H = {t.HeightValue:0.00}",
            Primitive.ExtrusionSimple => $"H = {t.HeightValue:0.00} · p = {t.PCoefValue:0.00}",
            Primitive.Revolution => "profil de révolution",
            Primitive.ExtrusionGeneralisee => $"âme amp = {t.AmeZAmpValue:0.00}, freq = {t.AmeZFreqValue:0.00}",
            _ => "",
        };
        if (_titleLabel != null)
            _titleLabel.Text = $"{prim.DisplayName()}   ·   {info}   ·   m = {t.SubU}, p = {t.SubV}";
    }

    // Thème
    public void ApplyTheme()
    {
        ApplyThemeInternal();
        GetNodeOrNull<ProfileEditor>(ProfileEditorPath)?.RefreshTheme();
        GetNodeOrNull<ProfileEditor>(AmeEditorPath)?.RefreshTheme();
        GetNodeOrNull<GroundGrid>(GroundGridPath)?.ApplyTheme();
        GetNodeOrNull<SurfaceCours1>(SurfacePath)?.ApplyTheme();
        if (!WorldEnvPath.IsEmpty)
        {
            var we = GetNodeOrNull<WorldEnvironment>(WorldEnvPath);
            if (we?.Environment != null)
                we.Environment.BackgroundColor = Palette.Current().BgApp;
        }
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
        if (_titleLabel != null) _titleLabel.AddThemeColorOverride("font_color", palette.TextDim);
        if (_rootPanel != null) UiTheme.ApplyControls(_rootPanel, palette);
    }

    // Accès cibles
    private SurfaceCours1? GetTarget() => SurfacePath.IsEmpty ? null : GetNodeOrNull<SurfaceCours1>(SurfacePath);
    private OrbitCamera? GetCamera() => CameraPath.IsEmpty ? null : GetNodeOrNull<OrbitCamera>(CameraPath);
    private GroundGrid? GetGround() => GroundGridPath.IsEmpty ? null : GetNodeOrNull<GroundGrid>(GroundGridPath);

    // Callbacks
    private void OnPrimitivePressed(Primitive prim)
    {
        var t = GetTarget();
        if (t != null) t.Prim = prim;
        ApplyPrimitiveVisibility(prim);
        UpdateTitle();
    }

    private void OnSubU(double v) { UpdateLabelInt(_subULabel, v); var t = GetTarget(); if (t != null) t.SubdivisionsU = (int)v; UpdateTitle(); }
    private void OnSubV(double v) { UpdateLabelInt(_subVLabel, v); var t = GetTarget(); if (t != null) t.SubdivisionsV = (int)v; UpdateTitle(); }
    private void OnRadius(double v) { UpdateLabelFloat(_radiusLabel, v); var t = GetTarget(); if (t != null) t.Radius = (float)v; UpdateTitle(); }
    private void OnHeight(double v) { UpdateLabelFloat(_heightLabel, v); var t = GetTarget(); if (t != null) t.Height = (float)v; UpdateTitle(); }
    private void OnPCoef(double v) { UpdateLabelFloat(_pCoefLabel, v); var t = GetTarget(); if (t != null) t.PCoef = (float)v; UpdateTitle(); }
    private void OnAmeAmp(double v) { UpdateLabelFloat(_ameAmpLabel, v); var t = GetTarget(); if (t != null) t.AmeZAmp = (float)v; UpdateTitle(); }
    private void OnAmeFreq(double v) { UpdateLabelFloat(_ameFreqLabel, v); var t = GetTarget(); if (t != null) t.AmeZFreq = (float)v; UpdateTitle(); }

    private void OnWireframeToggled(bool pressed) { var t = GetTarget(); if (t != null) t.ShowWireframe = pressed; }
    private void OnGroundToggled(bool pressed) { var g = GetGround(); if (g != null) g.Visible = pressed; }

    // Helpers
    private void SetControlVisible(NodePath path, bool visible)
    {
        if (path.IsEmpty) return;
        var node = GetNodeOrNull<Control>(path);
        if (node != null) node.Visible = visible;
    }

    private static void SetSlider(HSlider? slider, Label? label, double value)
    {
        if (slider == null) return;
        double step = slider.Step;
        slider.SetValueNoSignal(value);
        if (label != null)
            label.Text = step >= 1.0 ? ((int)value).ToString() : value.ToString("0.00");
    }

    private static void UpdateLabelInt(Label? label, double v) { if (label != null) label.Text = ((int)v).ToString(); }
    private static void UpdateLabelFloat(Label? label, double v) { if (label != null) label.Text = v.ToString("0.00"); }
}
