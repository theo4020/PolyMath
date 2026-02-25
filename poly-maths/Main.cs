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

    // ── Transform steps (shared by Bezier + BSpline) ──────────────────────
    private float _transStep  = 30f;
    private float _rotStep    = Mathf.Pi / 12f;   // 15°
    private float _scaleStep  = 1.10f;
    private float _shearStep  = 0.10f;

    // Step display labels — assigned in BuildSidebar(), updated by step-adjust methods
    private Label _transStepLbl, _rotStepLbl, _scaleStepLbl, _shearStepLbl;

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
    private const int M_BEZ_FILL_MARKED  = 38;
    private const int M_BEZ_FILL_ALL     = 39;
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
    // New Bezier constants (Task 8a)
    private const int M_BEZ_PASCAL     = 56;
    private const int M_BEZ_CASTELJAU  = 57;
    private const int M_BEZ_DEMO_SINE  = 58;

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
        // Double-clic gauche en mode Bézier → supprimer le point sous le curseur
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.DoubleClick)
        {
            if (_mode == AppMode.Bezier)
                _bezMgr.HandleDoubleClick(GetViewport().GetMousePosition());
            return;   // ne pas traiter aussi comme simple clic
        }

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
            if (key == Key.P) _bezMgr.ToggleShowPascal();
            if (key == Key.C) _bezMgr.ToggleShowCasteljau();
        }

        // ── BSpline ─────────────────────────────────────────────────────────
        if (_mode == AppMode.BSpline)
        {
            if (key == Key.Plus  || key == Key.KpAdd)      _bspMgr.StepUp();
            if (key == Key.Minus || key == Key.KpSubtract) _bspMgr.StepDown();
            if (key == Key.Delete)                         _bspMgr.HandleDelete();
        }

        // ── Transformées matricielles (Bézier & BSpline) ────────────────────
        if (_mode == AppMode.Bezier || _mode == AppMode.BSpline)
        {
            float tStep = shift ? _transStep / 6f : _transStep;

            switch (key)
            {
                case Key.Right: DoTranslate( tStep,     0); break;
                case Key.Left:  DoTranslate(-tStep,     0); break;
                case Key.Down:  DoTranslate(    0,  tStep); break;
                case Key.Up:    DoTranslate(    0, -tStep); break;
                case Key.R:     DoRotate(shift ? -1f : +1f); break;
                case Key.S:     DoScale(shift ? 1f / _scaleStep : _scaleStep); break;
                case Key.H:     DoShearH(shift ? -1f : +1f); break;
                case Key.V:     DoShearV(shift ? -1f : +1f); break;
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

    // ── Do* dispatch helpers ─────────────────────────────────────────────
    private void DoTranslate(float dx, float dy)
    {
        if (_mode == AppMode.Bezier)  _bezMgr.Translate(dx, dy);
        if (_mode == AppMode.BSpline) _bspMgr.Translate(dx, dy);
    }

    private void DoRotate(float sign)
    {
        float angle = sign * _rotStep;
        if (_mode == AppMode.Bezier)  _bezMgr.Rotate(angle);
        if (_mode == AppMode.BSpline) _bspMgr.Rotate(angle);
    }

    private void DoScale(float factor)
    {
        if (_mode == AppMode.Bezier)  _bezMgr.Scale(factor);
        if (_mode == AppMode.BSpline) _bspMgr.Scale(factor);
    }

    private void DoShearH(float sign)
    {
        float d = sign * _shearStep;
        if (_mode == AppMode.Bezier)  _bezMgr.ShearH(d);
        if (_mode == AppMode.BSpline) _bspMgr.ShearH(d);
    }

    private void DoShearV(float sign)
    {
        float d = sign * _shearStep;
        if (_mode == AppMode.Bezier)  _bezMgr.ShearV(d);
        if (_mode == AppMode.BSpline) _bspMgr.ShearV(d);
    }

    // ── Step-adjust helpers ──────────────────────────────────────────────
    private void RefreshStepLabels()
    {
        if (_transStepLbl  != null) _transStepLbl.Text  = $"T:{(int)_transStep}px";
        if (_rotStepLbl    != null) _rotStepLbl.Text    = $"R:{_rotStep * 180f / Mathf.Pi:F0}°";
        if (_scaleStepLbl  != null) _scaleStepLbl.Text  = $"S:{(_scaleStep - 1f) * 100f:F0}%";
        if (_shearStepLbl  != null) _shearStepLbl.Text  = $"H:{_shearStep:F2}";
    }

    private void TransStepUp()   { _transStep  = Math.Min(_transStep  + 5f,    100f); RefreshStepLabels(); }
    private void TransStepDown() { _transStep  = Math.Max(_transStep  - 5f,      5f); RefreshStepLabels(); }
    private void RotStepUp()     { _rotStep    = Math.Min(_rotStep    + Mathf.Pi / 36f, Mathf.Pi / 2f);  RefreshStepLabels(); }
    private void RotStepDown()   { _rotStep    = Math.Max(_rotStep    - Mathf.Pi / 36f, Mathf.Pi / 36f); RefreshStepLabels(); }
    private void ScaleStepUp()   { _scaleStep  = Math.Min(_scaleStep  + 0.05f,  2.0f); RefreshStepLabels(); }
    private void ScaleStepDown() { _scaleStep  = Math.Max(_scaleStep  - 0.05f,  1.05f); RefreshStepLabels(); }
    private void ShearStepUp()   { _shearStep  = Math.Min(_shearStep  + 0.05f,  0.5f); RefreshStepLabels(); }
    private void ShearStepDown() { _shearStep  = Math.Max(_shearStep  - 0.05f,  0.05f); RefreshStepLabels(); }

    private void HandleRightClick()
    {
        if (_mode == AppMode.Polygon)
        {
            if (!_polyMgr.HandleRightClick())
            {
                _menu.Position = (Vector2I)GetViewport().GetMousePosition();
                _menu.Popup();
            }
            return;
        }
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
        _menu.AddSeparator();
        _menu.AddItem("Pas +",  M_BEZ_STEP_UP);
        _menu.AddItem("Pas -",  M_BEZ_STEP_DOWN);
        _menu.AddSeparator();
        _menu.AddItem("Raccord C0", M_BEZ_JOIN_C0);
        _menu.AddItem("Raccord C1", M_BEZ_JOIN_C1);
        _menu.AddItem("Raccord C2", M_BEZ_JOIN_C2);
        _menu.AddSeparator();
        _menu.AddItem("Remplir active",        M_BEZ_FILL);
        _menu.AddItem("Remplir marquées",      M_BEZ_FILL_MARKED);
        _menu.AddItem("Remplir toutes",        M_BEZ_FILL_ALL);
        _menu.AddSeparator();
        _menu.AddItem("Afficher Pascal",    M_BEZ_PASCAL);
        _menu.AddItem("Afficher Casteljau", M_BEZ_CASTELJAU);
        _menu.AddItem("Demo sinus 50 pts",  M_BEZ_DEMO_SINE);
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
            case M_BEZ_STEP_UP:       _bezMgr.StepUp();          break;
            case M_BEZ_STEP_DOWN:     _bezMgr.StepDown();        break;
            case M_BEZ_JOIN_C0:       _bezMgr.JoinLastTwo(Continuity.C0); break;
            case M_BEZ_JOIN_C1:       _bezMgr.JoinLastTwo(Continuity.C1); break;
            case M_BEZ_JOIN_C2:       _bezMgr.JoinLastTwo(Continuity.C2); break;
            case M_BEZ_FILL:          _bezMgr.FillActiveCurve(); break;
            case M_BEZ_FILL_MARKED:   _bezMgr.FillMarked();      break;
            case M_BEZ_FILL_ALL:      _bezMgr.FillAll();         break;
            case M_BEZ_BENCH:         _bezMgr.RunBenchmark();    break;
            case M_BEZ_PASCAL:        _bezMgr.ToggleShowPascal();    break;
            case M_BEZ_CASTELJAU:     _bezMgr.ToggleShowCasteljau(); break;
            case M_BEZ_DEMO_SINE:     _bezMgr.LoadDemoSine50();      break;
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
        SideBtn(vbox, "+ Courbe",        () => { _mode = AppMode.Bezier; _bezMgr.NewCurve(); });
        SideHBox(vbox, "Édition/Ajout",  () => _bezMgr.ToggleEditMode(),
                       "Marquer [Sp]",   () => _bezMgr.ToggleActiveInSelection());
        SideHBox(vbox, "Suppr. active",  () => _bezMgr.DeleteActiveCurve(),
                       "Suppr. toutes",  () => _bezMgr.DeleteAll());
        SideHBox(vbox, "Remplir active", () => _bezMgr.FillActiveCurve(),
                       "Remplir toutes", () => _bezMgr.FillAll());
        SideHBox(vbox, "Raccord C0",     () => _bezMgr.JoinLastTwo(Continuity.C0),
                       "C1",             () => _bezMgr.JoinLastTwo(Continuity.C1));
        SideBtn(vbox,  "Raccord C2",     () => _bezMgr.JoinLastTwo(Continuity.C2));
        SideLabel(vbox, "Algo :");
        SideHBox(vbox, "Pascal [P]",     () => _bezMgr.ToggleShowPascal(),
                       "Casteljau [C]",  () => _bezMgr.ToggleShowCasteljau());
        SideBtn(vbox, "Demo sinus 50",   () => { _mode = AppMode.Bezier; _bezMgr.LoadDemoSine50(); });
        vbox.AddChild(new HSeparator());

        // ── BSpline ─────────────────────────────────────────────────────
        SideLabel(vbox, "── BSPLINE ──");
        SideHBox(vbox, "+ BSpline", () => { _mode = AppMode.BSpline; _bspMgr.NewBSpline(); },
                       "+ NURBS",   () => { _mode = AppMode.BSpline; _bspMgr.NewNurbs();   });
        SideHBox(vbox, "Édit/Ajout",    () => _bspMgr.ToggleEditMode(),
                       "Degré +",       () => _bspMgr.DegreeUp());
        SideHBox(vbox, "Degré −",       () => _bspMgr.DegreeDown(),
                       "Nœuds",         () => _bspMgr.ToggleKnots());
        SideHBox(vbox, "Cercle",        () => { _mode = AppMode.BSpline; _bspMgr.LoadDemoCircle();    },
                       "Ellipse",       () => { _mode = AppMode.BSpline; _bspMgr.LoadDemoEllipse();   });
        SideHBox(vbox, "Parabole",      () => { _mode = AppMode.BSpline; _bspMgr.LoadDemoParabola();  },
                       "Hyperbole",     () => { _mode = AppMode.BSpline; _bspMgr.LoadDemoHyperbola(); });
        vbox.AddChild(new HSeparator());

        // ── Transformées (Bézier + BSpline) ─────────────────────────────
        SideLabel(vbox, "─ TRANSFORM. ─");
        SideHBox(vbox, "← Trans", () => DoTranslate(-_transStep, 0),
                       "→ Trans",  () => DoTranslate( _transStep, 0));
        SideHBox(vbox, "↑ Trans",  () => DoTranslate(0, -_transStep),
                       "↓ Trans",  () => DoTranslate(0,  _transStep));
        SideHBox(vbox, "↻ CW",     () => DoRotate(+1f),
                       "↺ CCW",    () => DoRotate(-1f));
        SideHBox(vbox, "⊕ Scale+", () => DoScale(_scaleStep),
                       "⊖ Scale−", () => DoScale(1f / _scaleStep));
        SideHBox(vbox, "CisH+",    () => DoShearH(+1f),
                       "CisH−",    () => DoShearH(-1f));
        SideHBox(vbox, "CisV+",    () => DoShearV(+1f),
                       "CisV−",    () => DoShearV(-1f));
        SideLabel(vbox, "Pas :");
        _transStepLbl  = SideStepRow(vbox, "T:", "30px",  TransStepDown, TransStepUp);
        _rotStepLbl    = SideStepRow(vbox, "R:", "15°",   RotStepDown,   RotStepUp);
        _scaleStepLbl  = SideStepRow(vbox, "S:", "10%",   ScaleStepDown, ScaleStepUp);
        _shearStepLbl  = SideStepRow(vbox, "H:", "0.10",  ShearStepDown, ShearStepUp);
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

    /// <summary>Two equal-width buttons on one row.</summary>
    private static void SideHBox(VBoxContainer parent,
        string labelA, Action actionA,
        string labelB, Action actionB)
    {
        var hbox = new HBoxContainer();
        hbox.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        parent.AddChild(hbox);

        var btnA = new Button();
        btnA.Text = labelA;
        btnA.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        btnA.AddThemeFontSizeOverride("font_size", 11);
        btnA.Pressed += actionA;
        hbox.AddChild(btnA);

        var btnB = new Button();
        btnB.Text = labelB;
        btnB.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        btnB.AddThemeFontSizeOverride("font_size", 11);
        btnB.Pressed += actionB;
        hbox.AddChild(btnB);
    }

    /// <summary>[−] value-label [+] step control row. Returns the label.</summary>
    private static Label SideStepRow(VBoxContainer parent,
        string prefix, string initVal,
        Action onMinus, Action onPlus)
    {
        var hbox = new HBoxContainer();
        hbox.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        parent.AddChild(hbox);

        var minus = new Button(); minus.Text = "−";
        minus.AddThemeFontSizeOverride("font_size", 11);
        minus.CustomMinimumSize = new Vector2(22, 0);
        minus.Pressed += onMinus;
        hbox.AddChild(minus);

        var lbl = new Label();
        lbl.Text = $"{prefix}{initVal}";
        lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.AddThemeFontSizeOverride("font_size", 10);
        hbox.AddChild(lbl);

        var plus = new Button(); plus.Text = "+";
        plus.AddThemeFontSizeOverride("font_size", 11);
        plus.CustomMinimumSize = new Vector2(22, 0);
        plus.Pressed += onPlus;
        hbox.AddChild(plus);

        return lbl;
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
