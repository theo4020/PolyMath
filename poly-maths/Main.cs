using System;
using Godot;
using PolyMaths.Managers;
using PolyMaths.Algorithms;

public partial class Main : Node2D
{
    // ── Mode ──────────────────────────────────────────────────────────────
    private enum AppMode { Polygon, Bezier, BSpline }
    private AppMode _mode = AppMode.Polygon;

    // ── Managers ─────────────────────────────────────────────────────────
    private PolygonManager  _polyMgr = new PolygonManager();
    private BezierManager   _bezMgr  = new BezierManager();
    private BSplineManager  _bspMgr  = new BSplineManager();

    // ── Inspector exports (visibles dans l'éditeur Godot) ────────────────
    // Couleurs – Polygone
    [Export] public Color PolySubjectColor { get; set; } = new Color(0.2f, 0.6f, 1f);
    [Export] public Color PolyWindowColor  { get; set; } = new Color(1f, 0.6f, 0.1f);
    [Export] public Color PolyResultColor  { get; set; } = new Color(0.2f, 0.9f, 0.3f);
    // Couleurs – Bézier
    [Export] public Color BezControlColor  { get; set; } = new Color(0.5f, 0.5f, 0.5f);
    [Export] public Color BezCurveColor    { get; set; } = new Color(0.2f, 0.6f, 1f);
    [Export] public Color BezActiveColor   { get; set; } = new Color(1f, 0.3f, 0.3f);
    // Couleurs – BSpline / NURBS
    [Export] public Color BsControlColor   { get; set; } = new Color(0.5f, 0.5f, 0.5f);
    [Export] public Color BsCurveColor     { get; set; } = new Color(0.2f, 0.9f, 0.5f);
    [Export] public Color BsActiveColor    { get; set; } = new Color(1f, 0.5f, 0f);
    // Géométrie commune
    [Export] public int   DotRadius        { get; set; } = 5;

    // ── Menu ─────────────────────────────────────────────────────────────
    private PopupMenu _menu;
    private Label     _hud;

    // ── Menu item IDs ────────────────────────────────────────────────────
    // Mode
    private const int M_MODE_POLYGON = 0;
    private const int M_MODE_BEZIER  = 1;
    private const int M_MODE_BSPLINE = 2;
    // Polygon
    private const int M_POLY_RESET   = 10;
    // Bezier
    private const int M_BEZ_NEW          = 20;
    private const int M_BEZ_DELETE       = 21;
    private const int M_BEZ_TOGGLE_MODE  = 22;
    private const int M_BEZ_TOGGLE_METHOD= 23;
    private const int M_BEZ_STEP_UP      = 24;
    private const int M_BEZ_STEP_DOWN    = 25;
    private const int M_BEZ_JOIN_C0      = 26;
    private const int M_BEZ_JOIN_C1      = 27;
    private const int M_BEZ_JOIN_C2      = 28;
    private const int M_BEZ_FILL         = 29;
    private const int M_BEZ_BENCH        = 30;
    private const int M_BEZ_MARK         = 35;
    private const int M_BEZ_DELETE_MARKED= 36;
    private const int M_BEZ_DELETE_ALL   = 37;
    private const int M_BEZ_TRANSLATE    = 31;
    private const int M_BEZ_ROTATE       = 32;
    private const int M_BEZ_SCALE        = 33;
    private const int M_BEZ_SHEAR        = 34;
    // BSpline
    private const int M_BS_NEW            = 40;
    private const int M_BS_NURBS          = 41;
    private const int M_BS_DEG_UP         = 42;
    private const int M_BS_DEG_DOWN       = 43;
    private const int M_BS_KNOTS          = 44;
    private const int M_BS_EDIT           = 45;
    private const int M_BS_STEP_UP        = 46;
    private const int M_BS_STEP_DOWN      = 47;
    private const int M_BS_TRANSLATE      = 48;
    private const int M_BS_ROTATE         = 49;
    private const int M_BS_SCALE          = 50;
    private const int M_BS_SHEAR          = 51;
    private const int M_BS_DEMO_CIRCLE    = 52;
    private const int M_BS_DEMO_ELLIPSE   = 53;
    private const int M_BS_DEMO_PARABOLA  = 54;
    private const int M_BS_DEMO_HYPERBOLA = 55;

    // ── Lifecycle ────────────────────────────────────────────────────────
    public override void _Ready()
    {
        ApplyColors();
        BuildMenu();
        BuildHud();
        BuildSidebar();
        // Décommenter pour lancer les tests en console :
        // new PolyMaths.Tests.PolygonTestSuite().RunAllTests();
        // new PolyMaths.Tests.BezierTestSuite().RunAllTests();
        // new PolyMaths.Tests.BSplineTestSuite().RunAllTests();
    }

    private void ApplyColors()
    {
        _polyMgr.SubjectColor       = PolySubjectColor;
        _polyMgr.WindowColor        = PolyWindowColor;
        _polyMgr.ResultColor        = PolyResultColor;
        _polyMgr.DotRadius          = DotRadius;

        _bezMgr.ControlPolygonColor = BezControlColor;
        _bezMgr.CurveColor          = BezCurveColor;
        _bezMgr.ActiveCurveColor    = BezActiveColor;
        _bezMgr.DotRadius           = DotRadius;

        _bspMgr.ControlColor        = BsControlColor;
        _bspMgr.CurveColor          = BsCurveColor;
        _bspMgr.ActiveColor         = BsActiveColor;
        _bspMgr.DotRadius           = DotRadius;
    }

    private void ResetAll()
    {
        _polyMgr = new PolygonManager();
        _bezMgr  = new BezierManager();
        _bspMgr  = new BSplineManager();
        ApplyColors();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("Quitter"))
            GetTree().Quit();

        // Mouse drag (sondé chaque frame)
        if (_mode == AppMode.Bezier  && Input.IsMouseButtonPressed(MouseButton.Left))
            _bezMgr.HandleMouseMove(GetViewport().GetMousePosition());
        if (_mode == AppMode.BSpline && Input.IsMouseButtonPressed(MouseButton.Left))
            _bspMgr.HandleMouseMove(GetViewport().GetMousePosition());

        UpdateHud();
        QueueRedraw();
    }

    // _UnhandledInput garantit que les clics sur les boutons UI ne traversent pas jusqu'au canvas
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ClicGauche"))  HandleLeftClick();
        if (@event.IsActionReleased("ClicGauche")) { _bezMgr.HandleLeftRelease(); _bspMgr.HandleLeftRelease(); }
        if (@event.IsActionPressed("ClicDroit"))   HandleRightClick();
    }

    // Input.IsKeyJustPressed n'existe pas en C# Godot 4 — on utilise _Input à la place
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey k) return;
        if (!k.Pressed || k.Echo) return;   // uniquement la première pression, pas le repeat

        bool shift = k.ShiftPressed;
        var  key   = k.Keycode;

        // ── Bézier ──────────────────────────────────────────────────────────
        if (_mode == AppMode.Bezier)
        {
            if (key == Key.Plus  || key == Key.KpAdd)      _bezMgr.StepUp();
            if (key == Key.Minus || key == Key.KpSubtract) _bezMgr.StepDown();
            if (key == Key.Delete)                         _bezMgr.HandleDelete();
            if (key == Key.Tab)                            _bezMgr.SelectNext();
            if (key == Key.Space)                          _bezMgr.ToggleActiveInSelection();
        }

        // ── BSpline ─────────────────────────────────────────────────────────
        if (_mode == AppMode.BSpline)
        {
            if (key == Key.Plus  || key == Key.KpAdd)      _bspMgr.StepUp();
            if (key == Key.Minus || key == Key.KpSubtract) _bspMgr.StepDown();
            if (key == Key.Delete)                         _bspMgr.HandleDelete();
        }

        // ── Transformées matricielles (Bézier & BSpline) ────────────────────
        // Flèches → Translation ±10 px
        // R / Maj+R → Rotation ±15°  |  S / Maj+S → Échelle ×1.1/×0.9  |  H / Maj+H → Cisaillement ±0.1
        if (_mode == AppMode.Bezier || _mode == AppMode.BSpline)
        {
            switch (key)
            {
                case Key.Right: ApplyTransform(Matrix3x3.Translation( 10,   0)); break;
                case Key.Left:  ApplyTransform(Matrix3x3.Translation(-10,   0)); break;
                case Key.Down:  ApplyTransform(Matrix3x3.Translation(  0,  10)); break;
                case Key.Up:    ApplyTransform(Matrix3x3.Translation(  0, -10)); break;
                case Key.R:
                    ApplyTransform(shift ? Matrix3x3.Rotation(-Mathf.Pi/12f) : Matrix3x3.Rotation(Mathf.Pi/12f));
                    break;
                case Key.S:
                    ApplyTransform(shift ? Matrix3x3.Scaling(1f/1.1f,1f/1.1f) : Matrix3x3.Scaling(1.1f,1.1f));
                    break;
                case Key.H:
                    ApplyTransform(shift ? Matrix3x3.Shearing(-0.1f,0f) : Matrix3x3.Shearing(0.1f,0f));
                    break;
            }
        }
    }

    private void HandleLeftClick()
    {
        var mouse = GetViewport().GetMousePosition();
        switch (_mode)
        {
            case AppMode.Polygon: _polyMgr.HandleLeftClick(mouse); break;
            case AppMode.Bezier:  _bezMgr.HandleLeftClick(mouse);  break;
            case AppMode.BSpline: _bspMgr.HandleLeftClick(mouse);  break;
        }
    }

    /// <summary>Dispatche une transformée matricielle vers le manager actif.</summary>
    private void ApplyTransform(Matrix3x3 m)
    {
        if (_mode == AppMode.Bezier)  _bezMgr.ApplyTransform(m);
        if (_mode == AppMode.BSpline) _bspMgr.ApplyTransform(m);
    }

    private void HandleRightClick()
    {
        if (_mode == AppMode.Polygon)
        {
            _polyMgr.HandleRightClick();
            return;
        }
        // Bezier / BSpline: show popup menu
        _menu.Position = (Vector2I)GetViewport().GetMousePosition();
        _menu.Popup();
    }

    // ── Drawing ──────────────────────────────────────────────────────────
    public override void _Draw()
    {
        switch (_mode)
        {
            case AppMode.Polygon: _polyMgr.Draw(this); break;
            case AppMode.Bezier:  _bezMgr.Draw(this);  break;
            case AppMode.BSpline: _bspMgr.Draw(this);  break;
        }
    }

    // ── Menu construction ─────────────────────────────────────────────────
    private void BuildMenu()
    {
        var canvasLayer = new CanvasLayer();
        AddChild(canvasLayer);

        _menu = new PopupMenu();
        canvasLayer.AddChild(_menu);
        _menu.IdPressed += OnMenuPressed;

        // Mode switcher
        _menu.AddItem("Mode: Polygone",  M_MODE_POLYGON);
        _menu.AddItem("Mode: Bezier",    M_MODE_BEZIER);
        _menu.AddItem("Mode: BSpline",   M_MODE_BSPLINE);
        _menu.AddSeparator();
        // Polygon items
        _menu.AddItem("Reset polygone",  M_POLY_RESET);
        _menu.AddSeparator();
        // Bezier items
        _menu.AddItem("Nouvelle courbe",          M_BEZ_NEW);
        _menu.AddItem("Supprimer courbe active",   M_BEZ_DELETE);
        _menu.AddItem("Marquer / Démarquer",       M_BEZ_MARK);
        _menu.AddItem("Supprimer marquées",        M_BEZ_DELETE_MARKED);
        _menu.AddItem("Supprimer toutes",          M_BEZ_DELETE_ALL);
        _menu.AddSeparator();
        _menu.AddItem("Basculer: Édition / Ajout",    M_BEZ_TOGGLE_MODE);
        _menu.AddItem("Basculer: Direct / Casteljau", M_BEZ_TOGGLE_METHOD);
        _menu.AddSeparator();
        _menu.AddItem("Pas +",  M_BEZ_STEP_UP);
        _menu.AddItem("Pas -",  M_BEZ_STEP_DOWN);
        _menu.AddSeparator();
        _menu.AddItem("Raccord C0", M_BEZ_JOIN_C0);
        _menu.AddItem("Raccord C1", M_BEZ_JOIN_C1);
        _menu.AddItem("Raccord C2", M_BEZ_JOIN_C2);
        _menu.AddSeparator();
        _menu.AddItem("Remplir courbe",  M_BEZ_FILL);
        _menu.AddItem("Benchmark",       M_BEZ_BENCH);
        _menu.AddSeparator();
        _menu.AddItem("Translater (+10,+10)", M_BEZ_TRANSLATE);
        _menu.AddItem("Rotation 15°",          M_BEZ_ROTATE);
        _menu.AddItem("Échelle x1.1",         M_BEZ_SCALE);
        _menu.AddItem("Cisaillement shx=0.2", M_BEZ_SHEAR);
        _menu.AddSeparator();
        // BSpline items
        _menu.AddItem("Nouvelle BSpline",     M_BS_NEW);
        _menu.AddItem("Nouveau NURBS",        M_BS_NURBS);
        _menu.AddSeparator();
        _menu.AddItem("Degré +",              M_BS_DEG_UP);
        _menu.AddItem("Degré -",              M_BS_DEG_DOWN);
        _menu.AddItem("Nœuds: basculer",       M_BS_KNOTS);
        _menu.AddItem("Basculer: Édition",    M_BS_EDIT);
        _menu.AddSeparator();
        _menu.AddItem("Pas + (BS)",           M_BS_STEP_UP);
        _menu.AddItem("Pas - (BS)",           M_BS_STEP_DOWN);
        _menu.AddSeparator();
        _menu.AddItem("Translater BS (+10,+10)", M_BS_TRANSLATE);
        _menu.AddItem("Rotation BS 15°",         M_BS_ROTATE);
        _menu.AddItem("Scale BS x1.1",           M_BS_SCALE);
        _menu.AddItem("Cisaillement BS shx=0.2", M_BS_SHEAR);
        _menu.AddSeparator();
        _menu.AddItem("Demo: Cercle",         M_BS_DEMO_CIRCLE);
        _menu.AddItem("Demo: Ellipse",        M_BS_DEMO_ELLIPSE);
        _menu.AddItem("Demo: Parabole",       M_BS_DEMO_PARABOLA);
        _menu.AddItem("Demo: Hyperbole",      M_BS_DEMO_HYPERBOLA);
    }

    private void OnMenuPressed(long id)
    {
        switch ((int)id)
        {
            // Mode
            case M_MODE_POLYGON: _mode = AppMode.Polygon; break;
            case M_MODE_BEZIER:  _mode = AppMode.Bezier;  break;
            case M_MODE_BSPLINE: _mode = AppMode.BSpline; break;
            // Polygon
            case M_POLY_RESET: _polyMgr.Reset(); break;
            // Bezier
            case M_BEZ_NEW:           _bezMgr.NewCurve();        break;
            case M_BEZ_DELETE:        _bezMgr.DeleteActiveCurve();      break;
            case M_BEZ_MARK:          _bezMgr.ToggleActiveInSelection(); break;
            case M_BEZ_DELETE_MARKED: _bezMgr.DeleteMarked();            break;
            case M_BEZ_DELETE_ALL:    _bezMgr.DeleteAll();               break;
            case M_BEZ_TOGGLE_MODE:   _bezMgr.ToggleEditMode();  break;
            case M_BEZ_TOGGLE_METHOD: _bezMgr.ToggleMethod();    break;
            case M_BEZ_STEP_UP:       _bezMgr.StepUp();          break;
            case M_BEZ_STEP_DOWN:     _bezMgr.StepDown();        break;
            case M_BEZ_JOIN_C0:       _bezMgr.JoinLastTwo(Continuity.C0); break;
            case M_BEZ_JOIN_C1:       _bezMgr.JoinLastTwo(Continuity.C1); break;
            case M_BEZ_JOIN_C2:       _bezMgr.JoinLastTwo(Continuity.C2); break;
            case M_BEZ_FILL:          _bezMgr.FillActiveCurve(); break;
            case M_BEZ_BENCH:         _bezMgr.RunBenchmark();    break;
            case M_BEZ_TRANSLATE:
                _bezMgr.ApplyTransform(Matrix3x3.Translation(10, 10)); break;
            case M_BEZ_ROTATE:
                _bezMgr.ApplyTransform(Matrix3x3.Rotation(Mathf.Pi / 12f)); break;
            case M_BEZ_SCALE:
                _bezMgr.ApplyTransform(Matrix3x3.Scaling(1.1f, 1.1f)); break;
            case M_BEZ_SHEAR:
                _bezMgr.ApplyTransform(Matrix3x3.Shearing(0.2f, 0f)); break;
            // BSpline
            case M_BS_NEW:            _bspMgr.NewBSpline();      break;
            case M_BS_NURBS:          _bspMgr.NewNurbs();        break;
            case M_BS_DEG_UP:         _bspMgr.DegreeUp();        break;
            case M_BS_DEG_DOWN:       _bspMgr.DegreeDown();      break;
            case M_BS_KNOTS:          _bspMgr.ToggleKnots();     break;
            case M_BS_EDIT:           _bspMgr.ToggleEditMode();  break;
            case M_BS_STEP_UP:        _bspMgr.StepUp();          break;
            case M_BS_STEP_DOWN:      _bspMgr.StepDown();        break;
            case M_BS_TRANSLATE:
                _bspMgr.ApplyTransform(Matrix3x3.Translation(10, 10)); break;
            case M_BS_ROTATE:
                _bspMgr.ApplyTransform(Matrix3x3.Rotation(Mathf.Pi / 12f)); break;
            case M_BS_SCALE:
                _bspMgr.ApplyTransform(Matrix3x3.Scaling(1.1f, 1.1f)); break;
            case M_BS_SHEAR:
                _bspMgr.ApplyTransform(Matrix3x3.Shearing(0.2f, 0f)); break;
            case M_BS_DEMO_CIRCLE:    _bspMgr.LoadDemoCircle();    break;
            case M_BS_DEMO_ELLIPSE:   _bspMgr.LoadDemoEllipse();   break;
            case M_BS_DEMO_PARABOLA:  _bspMgr.LoadDemoParabola();  break;
            case M_BS_DEMO_HYPERBOLA: _bspMgr.LoadDemoHyperbola(); break;
        }
        QueueRedraw();
    }

    // ── HUD ──────────────────────────────────────────────────────────────
    private void BuildHud()
    {
        var canvasLayer = new CanvasLayer();
        AddChild(canvasLayer);
        _hud = new Label();
        _hud.Position = new Vector2(10, 10);
        _hud.AddThemeFontSizeOverride("font_size", 14);
        canvasLayer.AddChild(_hud);
    }

    // ── Sidebar ───────────────────────────────────────────────────────────
    private void BuildSidebar()
    {
        var layer = new CanvasLayer();
        layer.Layer = 10;
        AddChild(layer);

        var panel = new Panel();
        panel.Position = new Vector2(1775, 0);
        panel.Size     = new Vector2(145, 1080);
        layer.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.Position = new Vector2(4, 6);
        vbox.Size     = new Vector2(137, 1068);
        panel.AddChild(vbox);

        // ── Reset global ────────────────────────────────────────────────
        SideBtn(vbox, "⟳ Reset tout",  () => { ResetAll(); _mode = AppMode.Polygon; });
        vbox.AddChild(new HSeparator());

        // ── Mode ────────────────────────────────────────────────────────
        SideLabel(vbox, "── MODE ──");
        SideBtn(vbox, "Polygone",       () => _mode = AppMode.Polygon);
        SideBtn(vbox, "Bézier",         () => _mode = AppMode.Bezier);
        SideBtn(vbox, "BSpline / NURBS",() => _mode = AppMode.BSpline);
        vbox.AddChild(new HSeparator());

        // ── Bézier ──────────────────────────────────────────────────────
        SideLabel(vbox, "── BÉZIER ──");
        SideBtn(vbox, "+ Courbe",       () => { _mode = AppMode.Bezier;  _bezMgr.NewCurve(); });
        SideBtn(vbox, "Édition / Ajout",() => _bezMgr.ToggleEditMode());
        SideBtn(vbox, "Marquer [Espace]",      () => _bezMgr.ToggleActiveInSelection());
        SideBtn(vbox, "Suppr. active",         () => _bezMgr.DeleteActiveCurve());
        SideBtn(vbox, "Suppr. marquées",       () => _bezMgr.DeleteMarked());
        SideBtn(vbox, "Suppr. toutes",         () => _bezMgr.DeleteAll());
        SideBtn(vbox, "Méthode",        () => _bezMgr.ToggleMethod());
        SideBtn(vbox, "Raccord C0",     () => _bezMgr.JoinLastTwo(Continuity.C0));
        SideBtn(vbox, "Raccord C1",     () => _bezMgr.JoinLastTwo(Continuity.C1));
        SideBtn(vbox, "Raccord C2",     () => _bezMgr.JoinLastTwo(Continuity.C2));
        SideBtn(vbox, "Remplir",        () => _bezMgr.FillActiveCurve());
        vbox.AddChild(new HSeparator());

        // ── BSpline ─────────────────────────────────────────────────────
        SideLabel(vbox, "── BSPLINE ──");
        SideBtn(vbox, "+ BSpline",      () => { _mode = AppMode.BSpline; _bspMgr.NewBSpline(); });
        SideBtn(vbox, "+ NURBS",        () => { _mode = AppMode.BSpline; _bspMgr.NewNurbs(); });
        SideBtn(vbox, "Édition / Ajout",() => _bspMgr.ToggleEditMode());
        SideBtn(vbox, "Degré +",        () => _bspMgr.DegreeUp());
        SideBtn(vbox, "Degré -",        () => _bspMgr.DegreeDown());
        SideBtn(vbox, "Nœuds",          () => _bspMgr.ToggleKnots());
        SideBtn(vbox, "Cercle",         () => { _mode = AppMode.BSpline; _bspMgr.LoadDemoCircle(); });
        SideBtn(vbox, "Ellipse",        () => { _mode = AppMode.BSpline; _bspMgr.LoadDemoEllipse(); });
        SideBtn(vbox, "Parabole",       () => { _mode = AppMode.BSpline; _bspMgr.LoadDemoParabola(); });
        SideBtn(vbox, "Hyperbole",      () => { _mode = AppMode.BSpline; _bspMgr.LoadDemoHyperbola(); });
        vbox.AddChild(new HSeparator());

        // ── Polygone ────────────────────────────────────────────────────
        SideLabel(vbox, "── POLYGONE ──");
        SideBtn(vbox, "Reset",          () => _polyMgr.Reset());
        vbox.AddChild(new HSeparator());

        // ── Hint ────────────────────────────────────────────────────────
        var hint = new Label();
        hint.Text = "Clic droit\n= menu complet";
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(hint);
    }

    private static void SideLabel(VBoxContainer parent, string text)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.AddThemeFontSizeOverride("font_size", 11);
        parent.AddChild(lbl);
    }

    private static void SideBtn(VBoxContainer parent, string text, Action action)
    {
        var btn = new Button();
        btn.Text = text;
        btn.AddThemeFontSizeOverride("font_size", 12);
        btn.Pressed += action;
        parent.AddChild(btn);
    }

    private void UpdateHud()
    {
        _hud.Text = _mode switch
        {
            AppMode.Polygon => string.Format("MODE: POLYGONE\n{0}", _polyMgr.StatusText),
            AppMode.Bezier  => _bezMgr.StatusText,
            AppMode.BSpline => _bspMgr.StatusText,
            _               => ""
        };
    }
}
