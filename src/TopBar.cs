using Godot;

namespace MathsPower;

// Barre supérieure : titre + onglets Partie 1 / Partie 2 + bouton de thème.
// Coordinateur du thème : bascule ThemeState et propage apply_theme.
[GlobalClass]
public partial class TopBar : Control
{
    [Export] public NodePath Cours1LayerPath { get; set; } = new();
    [Export] public NodePath Cours2LayerPath { get; set; } = new();
    [Export] public NodePath Cours1SurfacePath { get; set; } = new();
    [Export] public NodePath Cours2SurfacePath { get; set; } = new();
    [Export] public NodePath UiPanelPath { get; set; } = new();
    [Export] public NodePath UiPanel2Path { get; set; } = new();

    private Button? _cours1Button;
    private Button? _cours2Button;
    private Button? _themeButton;
    private PanelContainer? _rootPanel;
    private Label? _titleLabel;

    public override void _Ready()
    {
        BuildUi();
        ApplyThemeInternal();
        SwitchTab(0);
    }

    private void BuildUi()
    {
        _rootPanel = new PanelContainer();
        _rootPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_rootPanel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 8);
        _rootPanel.AddChild(margin);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(hbox);

        _titleLabel = new Label { Text = "⏣  Surfaces paramétrées" };
        hbox.AddChild(_titleLabel);

        var spacer1 = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        hbox.AddChild(spacer1);

        _cours1Button = new Button { Text = "Partie 1 — Primitives", ToggleMode = true, ButtonPressed = true };
        _cours1Button.Pressed += () => SwitchTab(0);
        hbox.AddChild(_cours1Button);

        _cours2Button = new Button { Text = "Partie 2 — Bézier", ToggleMode = true };
        _cours2Button.Pressed += () => SwitchTab(1);
        hbox.AddChild(_cours2Button);

        var spacer2 = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        hbox.AddChild(spacer2);

        _themeButton = new Button
        {
            Text = ThemeState.IsDark ? "🌙" : "☀",
            CustomMinimumSize = new Vector2(40, 0),
            TooltipText = "Basculer thème sombre / clair",
        };
        _themeButton.Pressed += OnThemePressed;
        hbox.AddChild(_themeButton);
    }

    private void OnThemePressed()
    {
        bool isDark = ThemeState.Toggle();
        if (_themeButton != null)
            _themeButton.Text = isDark ? "🌙" : "☀";
        ApplyThemeInternal();
        if (!UiPanelPath.IsEmpty)
            GetNodeOrNull<UiPanel>(UiPanelPath)?.ApplyTheme();
        if (!UiPanel2Path.IsEmpty)
            GetNodeOrNull<UiPanelCours2>(UiPanel2Path)?.ApplyTheme();
    }

    private void SwitchTab(int tab)
    {
        if (_cours1Button != null) _cours1Button.SetPressedNoSignal(tab == 0);
        if (_cours2Button != null) _cours2Button.SetPressedNoSignal(tab == 1);

        SetControlVisible(Cours1LayerPath, tab == 0);
        SetControlVisible(Cours2LayerPath, tab == 1);
        SetNode3DVisible(Cours1SurfacePath, tab == 0);
        SetNode3DVisible(Cours2SurfacePath, tab == 1);
    }

    private void ApplyThemeInternal()
    {
        Palette palette = Palette.Current();
        if (_rootPanel != null)
            _rootPanel.AddThemeStyleboxOverride("panel", UiTheme.PanelStyle(palette, 0));
        if (_titleLabel != null)
            _titleLabel.AddThemeColorOverride("font_color", palette.TextStrong);
        if (_rootPanel != null) UiTheme.ApplyControls(_rootPanel, palette);
    }

    private void SetControlVisible(NodePath path, bool visible)
    {
        if (path.IsEmpty) return;
        var node = GetNodeOrNull<Control>(path);
        if (node != null) node.Visible = visible;
    }

    private void SetNode3DVisible(NodePath path, bool visible)
    {
        if (path.IsEmpty) return;
        var node = GetNodeOrNull<Node3D>(path);
        if (node != null) node.Visible = visible;
    }
}
