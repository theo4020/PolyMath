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
        BuildMenu();
        BuildHud();
        // Uncomment to run tests:
        // new PolyMaths.Tests.PolygonTestSuite().RunAllTests();
        // new PolyMaths.Tests.BezierTestSuite().RunAllTests();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("Quitter"))
            GetTree().Quit();

        // Keyboard step control (Bézier)
        if (_mode == AppMode.Bezier)
        {
            if (Input.IsKeyJustPressed(Key.Plus)  || Input.IsKeyJustPressed(Key.KpAdd))      _bezMgr.StepUp();
            if (Input.IsKeyJustPressed(Key.Minus) || Input.IsKeyJustPressed(Key.KpSubtract)) _bezMgr.StepDown();
            if (Input.IsKeyJustPressed(Key.Delete))  _bezMgr.HandleDelete();
            if (Input.IsKeyJustPressed(Key.Tab))     _bezMgr.SelectNext();
        }

        // Mouse drag (must poll every frame)
        if (_mode == AppMode.Bezier && Input.IsMouseButtonPressed(MouseButton.Left))
            _bezMgr.HandleMouseMove(GetViewport().GetMousePosition());
        if (_mode == AppMode.BSpline && Input.IsMouseButtonPressed(MouseButton.Left))
            _bspMgr.HandleMouseMove(GetViewport().GetMousePosition());

        // BSpline key handling
        if (_mode == AppMode.BSpline)
        {
            if (Input.IsKeyJustPressed(Key.Plus)  || Input.IsKeyJustPressed(Key.KpAdd))      _bspMgr.StepUp();
            if (Input.IsKeyJustPressed(Key.Minus) || Input.IsKeyJustPressed(Key.KpSubtract)) _bspMgr.StepDown();
            if (Input.IsKeyJustPressed(Key.Delete))  _bspMgr.HandleDelete();
        }

        // Clicks
        if (Input.IsActionJustPressed("ClicGauche"))  HandleLeftClick();
        if (Input.IsActionJustReleased("ClicGauche")) _bezMgr.HandleLeftRelease();
        if (Input.IsActionJustReleased("ClicGauche") && _mode == AppMode.BSpline) _bspMgr.HandleLeftRelease();
        if (Input.IsActionJustPressed("ClicDroit"))   HandleRightClick();

        UpdateHud();
        QueueRedraw();
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
        _menu.AddSeparator();
        _menu.AddItem("Toggle Edit / Append",      M_BEZ_TOGGLE_MODE);
        _menu.AddItem("Toggle Direct / Casteljau", M_BEZ_TOGGLE_METHOD);
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
        _menu.AddItem("Rotation 15 deg",      M_BEZ_ROTATE);
        _menu.AddItem("Scale x1.1",           M_BEZ_SCALE);
        _menu.AddItem("Cisaillement shx=0.2", M_BEZ_SHEAR);
        _menu.AddSeparator();
        // BSpline items
        _menu.AddItem("Nouvelle BSpline",     M_BS_NEW);
        _menu.AddItem("Nouveau NURBS",        M_BS_NURBS);
        _menu.AddSeparator();
        _menu.AddItem("Degré +",              M_BS_DEG_UP);
        _menu.AddItem("Degré -",              M_BS_DEG_DOWN);
        _menu.AddItem("Nœuds: toggle",        M_BS_KNOTS);
        _menu.AddItem("Toggle Edit BSpline",  M_BS_EDIT);
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
            case M_BEZ_DELETE:        _bezMgr.DeleteActiveCurve(); break;
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
