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
        _menu.AddItem("Remplir active",        M_BEZ_FILL);
        _menu.AddItem("Remplir marquées",      M_BEZ_FILL_MARKED);
        _menu.AddItem("Remplir toutes",        M_BEZ_FILL_ALL);
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
            case M_BEZ_FILL_MARKED:   _bezMgr.FillMarked();      break;
            case M_BEZ_FILL_ALL:      _bezMgr.FillAll();         break;
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
        SideBtn(vbox, "Remplir active",  () => _bezMgr.FillActiveCurve());
        SideBtn(vbox, "Remplir marquées",() => _bezMgr.FillMarked());
        SideBtn(vbox, "Remplir toutes", () => _bezMgr.FillAll());
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
	//Modes
	public enum EMode { DrawPolygon, DrawBezier, Eraser, MovePoint, MovePolygon }

	//liste est en cours de dessin
	private enum EDrawPhase { Polygon, Window, Done }

	private EMode _currentMode = EMode.DrawBezier;
	private EDrawPhase _drawPhase = EDrawPhase.Polygon;

	public EMode CurrentMode
	{
		get => _currentMode;
		set
		{
			if (_currentMode != value)
			{
				if (value == EMode.DrawPolygon)
				{
					_controlPoints.Clear();
					_pascalPoints.Clear();
					_casteljauPoints.Clear();
				}
				else if (value == EMode.DrawBezier)
				{
					_polygonPoints.Clear();
					_windowPoints.Clear();
					_resultPoints.Clear();
					_polygonClosed = false;
					_windowClosed = false;
					_drawPhase = EDrawPhase.Polygon;
					QueueRedraw();
				}
			}
			_currentMode = value;
			//si on change de mode ce qui est drag est relaché
			_draggedPoint   = null;
			_draggedPolygon = null;
		}
	}

	//Listes polygonales
	private List<Point> _polygonPoints = new List<Point>();
	private List<Point> _windowPoints = new List<Point>();
	private List<Point> _resultPoints = new List<Point>();
	private List<Point> _controlPoints = new List<Point>();
	private List<Point> _pascalPoints = new List<Point>();
	private List<Point> _casteljauPoints = new List<Point>();

	private bool _polygonClosed = false;
	private bool _windowClosed = false;

	private Point _draggedPoint = null;
	private List<Point> _draggedPolygon = null;
	private Vector2 _dragOffset = Vector2.Zero;

	//Exports
	[Export] private float _mouseRadius = 20f;
	[Export] private Color _polygonColor = new Color(0.2f, 0.6f, 1f, 0.4f);
	[Export] private Color _windowColor = new Color(1f, 0.6f, 0.2f, 0.4f);
	[Export] private Color _resultColor = new Color(0.2f, 1f, 0.4f, 0.7f);
	[Export] private Color _controlColor = new Color(0f, 0f, 0f);
	[Export] private Color _pascalColor = new Color(0f, 0f, 1f);
	[Export] private Color _casteljauColor = new Color(1f, 0f, 0f);
	[Export] private float _lineWidth = 2f;
	[Export] private float _pointRadius = 6f;
	[Export] private int pas = 10;
	
	//autre
	private VBoxContainer _container;
	private bool showPascal = false;
	private bool showCasteljau = false;

	
	public override void _Ready()
	{
		_container = GetNode<VBoxContainer>("../Control/VBoxContainer");
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionPressed("Quitter"))
			GetTree().Quit();

		Vector2 mousePos = GetViewport().GetMousePosition();

		HandleLeftClick(mousePos);
		HandleRightClick();
		HandleDrag(mousePos);
		HandlePlus();
		HandleMinus();

		QueueRedraw();
	}

	private void HandlePlus()
	{
		if (!Input.IsActionJustPressed("AugmenterPas")) return;

		pas++;
		RecalculateBezier();
	}
	
	private void HandleMinus()
	{
		if (!Input.IsActionJustPressed("DiminuerPas")) return;

		if (pas >= 3) pas--;
		RecalculateBezier();
	}

	//Gestion des clics gauche
	private void HandleLeftClick(Vector2 mousePos)
	{
		if (!Input.IsActionJustPressed("ClicGauche")) return;

		if (_container.GetGlobalRect().HasPoint(mousePos)) return;

		switch (_currentMode)
		{
			case EMode.DrawPolygon:
				HandleDrawPolygon(mousePos);
				break;
			
			case EMode.DrawBezier:
				HandleDrawBezier(mousePos);
				break;

			case EMode.Eraser:
				HandleErase(mousePos);
				break;

			case EMode.MovePoint:
				var pt = GetNearestPoint(mousePos);
				if (pt != null)
				{
					_draggedPoint = pt;
					_dragOffset = mousePos - pt.ToVector2();
				}
				break;

			case EMode.MovePolygon:
				var anchor = GetNearestPoint(mousePos);
				if (anchor != null)
				{
					_draggedPoint = anchor;
					_draggedPolygon = GetPolygonOf(anchor);
					_dragOffset = mousePos - anchor.ToVector2();
				}
				break;
		}
	}

	private void HandleDrawPolygon(Vector2 mousePos)
	{
		if (_drawPhase == EDrawPhase.Polygon && !_polygonClosed)
		{
			_polygonPoints.Add(new Point(mousePos, Point.EOwner.Polygon));
		}
		else if (_drawPhase == EDrawPhase.Window && !_windowClosed)
		{
			var temp = new List<Point>(_windowPoints);
			temp.Add(new Point(mousePos, Point.EOwner.Window));
			if (IsConvex(temp))
			{
				_windowPoints.Add(new Point(mousePos, Point.EOwner.Window));
				RecalculateResult();
			}
		}
	}
	
	private void HandleDrawBezier(Vector2 mousePos)
	{
		_controlPoints.Add(new Point(mousePos, Point.EOwner.Bezier));
		RecalculateBezier();
	}

	public void HandleShowPascal()
	{
		showPascal = !showPascal;
		RecalculateBezier();
	}
	
	public void HandleShowCasteljau()
	{
		showCasteljau = !showCasteljau;
		RecalculateBezier();
	}

	private void HandleErase(Vector2 mousePos)
	{
		var pt = GetNearestPoint(mousePos);
		if (pt == null) return;

		bool removed = false;

		if (_polygonPoints.Contains(pt) && !_polygonClosed)
		{
			_polygonPoints.Remove(pt);
			removed = true;
		}
		else if (_windowPoints.Contains(pt) && !_windowClosed)
		{
			_windowPoints.Remove(pt);
			removed = true;
		}
		else if (_controlPoints.Contains(pt))
		{
			_controlPoints.Remove(pt);
			removed = true;
		}

		if (removed)
		{
			RecalculateResult();
			RecalculateBezier();
		}
	}

	//Gestion du clic droit (fermeture du polygone)
	private void HandleRightClick()
	{
		if (!Input.IsActionJustPressed("ClicDroit")) return;
		if (_currentMode != EMode.DrawPolygon) return;

		if (_drawPhase == EDrawPhase.Polygon && !_polygonClosed)
		{
			if (_polygonPoints.Count >= 3)
			{
				_polygonClosed = true;
				_drawPhase = EDrawPhase.Window;
				RecalculateResult();
			}
		}
		else if (_drawPhase == EDrawPhase.Window && !_windowClosed)
		{
			if (_windowPoints.Count >= 3)
			{
				_windowClosed = true;
				_drawPhase = EDrawPhase.Done;
				RecalculateResult();
			}
		}
	}

	//Gestion du Drag en cours
	private void HandleDrag(Vector2 mousePos)
	{
		if (Input.IsActionJustReleased("ClicGauche"))
		{
			_draggedPoint = null;
			_draggedPolygon = null;
			return;
		}

		if (!Input.IsActionPressed("ClicGauche")) return;

		if (_draggedPolygon != null && _draggedPoint != null)
		{
			Vector2 anchorPos = _draggedPoint.ToVector2();
			Vector2 newPos = mousePos - _dragOffset;
			Vector2 delta = newPos - anchorPos;

			foreach (var p in _draggedPolygon)
			{
				p.X += delta.X;
				p.Y += delta.Y;
			}
			RecalculateResult();
			RecalculateBezier();
		}
		else if (_draggedPoint != null)
		{
			_draggedPoint.X = mousePos.X - _dragOffset.X;
			_draggedPoint.Y = mousePos.Y - _dragOffset.Y;
			RecalculateResult();
			RecalculateBezier();
		}
	}

	//Dessin
	public override void _Draw()
	{
		DrawPolygonWithOutline(_polygonPoints, _polygonClosed, _polygonColor);
		DrawPolygonWithOutline(_windowPoints, _windowClosed, _windowColor);
		DrawPolygonWithOutline(_controlPoints, false, _controlColor);
		DrawPolygonWithOutline(_pascalPoints, false, _pascalColor);
		DrawPolygonWithOutline(_casteljauPoints, false, _casteljauColor);

		if (_resultPoints.Count >= 3)
		{
			DrawPolygonWithOutline(_resultPoints, true, _resultColor);
		}

		DrawPoints(_polygonPoints, _polygonColor);
		DrawPoints(_windowPoints, _windowColor);
		DrawPoints(_controlPoints, _controlColor);
		DrawPoints(_pascalPoints, _pascalColor);
		DrawPoints(_casteljauPoints, _casteljauColor);
	}

	private void DrawPolygonWithOutline(List<Point> pts, bool closed, Color color)
	{
		if (pts.Count == 0) return;

		for (int i = 0; i < pts.Count - 1; i++)
			DrawLine(pts[i].ToVector2(), pts[i + 1].ToVector2(), color, _lineWidth);

		if (closed && pts.Count >= 3)
		{
			DrawLine(pts[pts.Count - 1].ToVector2(), pts[0].ToVector2(), color, _lineWidth);

			var arr = new Vector2[pts.Count];
			for (int i = 0; i < pts.Count; i++) arr[i] = pts[i].ToVector2();
			DrawPolygon(arr, new Color[] { color });
		}
	}

	private void DrawPoints(List<Point> pts, Color color)
	{
		foreach (var p in pts)
		{
			DrawCircle(p.ToVector2(), _pointRadius, new Color(1, 1, 1));
			DrawCircle(p.ToVector2(), _pointRadius - 2f, color);
		}
	}

	//Reset
	public void ResetPolygons()
	{
		_polygonPoints.Clear();
		_windowPoints.Clear();
		_resultPoints.Clear();
		_controlPoints.Clear();
		_pascalPoints.Clear();
		_casteljauPoints.Clear();
		_polygonClosed = false;
		_windowClosed = false;
		_drawPhase = EDrawPhase.Polygon;
		_draggedPoint = null;
		_draggedPolygon = null;
		QueueRedraw();
	}

	//Algo de fenêtrage
	private void RecalculateResult()
	{
		if (_polygonClosed && _windowClosed && _polygonPoints.Count >= 3 && _windowPoints.Count >= 3)
		{
			// Ferme le polygone fenêtre pour SH (dernier point = premier)
			var windowClosed = new List<Point>(_windowPoints);
			windowClosed.Add(_windowPoints[0]);
			_resultPoints = AlgoSH(_polygonPoints, windowClosed);
		}
		else
		{
			_resultPoints.Clear();
		}
	}

	private void RecalculateBezier()
	{
		if (showPascal)
		{
			//recalculatePascal
		}
		else
		{
			_pascalPoints.Clear();
		}

		if (showCasteljau)
		{
			_casteljauPoints = AlgoCasteljau(_controlPoints);
		}
		else
		{
			_casteljauPoints.Clear();
		}
	}

	private bool IsConvex(List<Point> pts)
	{
		if (pts.Count < 4) return true;

		bool gotNeg = false, gotPos = false;
		int n = pts.Count;

		for (int i = 0; i < n; i++)
		{
			Point a = pts[i], b = pts[(i + 1) % n], c = pts[(i + 2) % n];
			float cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
			if (cross < 0) gotNeg = true;
			else if (cross > 0) gotPos = true;
			if (gotNeg && gotPos) return false;
		}
		return true;
	}

	private List<Point> AlgoSH(List<Point> P, List<Point> F)
	{
		List<Point> tempP = new List<Point>(P);

		for (int i = 0; i <= F.Count - 2; i++)
		{
			List<Point> PS = new List<Point>();
			Point S = tempP[tempP.Count - 1];
			Point f = tempP[0];

			for (int j = 0; j < tempP.Count; j++)
			{
				Point current = tempP[j];

				if (Coupe(S, current, F[i], F[i + 1]))
					PS.Add(Intersection(S, current, F[i], F[i + 1]));

				if (Visible(current, F[i], F[i + 1]))
					PS.Add(current);

				S = current;
			}

			if (PS.Count > 0 && Coupe(S, f, F[i], F[i + 1]))
				PS.Add(Intersection(S, f, F[i], F[i + 1]));

			if (PS.Count == 0) return new List<Point>();
			tempP = new List<Point>(PS);
		}
		return tempP;
	}

	private bool Visible(Point S, Point F1, Point F2)
	{
		float cross = (S.X - F1.X) * (F2.Y - F1.Y) - (S.Y - F1.Y) * (F2.X - F1.X);
		return cross > 0;
	}

	private bool Coupe(Point S, Point P, Point F1, Point F2)
		=> Visible(S, F1, F2) ^ Visible(P, F1, F2);

	private Point Intersection(Point P1, Point P2, Point P3, Point P4)
	{
		float a = P2.X - P1.X, b = P3.X - P4.X;
		float c = P2.Y - P1.Y, d = P3.Y - P4.Y;
		float det = a * d - b * c;

		if (MathF.Abs(det) < 1e-6f) return P1;

		float bx = P3.X - P1.X;
		float by = P3.Y - P1.Y;
		float t  = (d * bx - b * by) / det;

		return new Point(P1.X + (P2.X - P1.X) * t, P1.Y + (P2.Y - P1.Y) * t);
	}

	private Point GetNearestPoint(Vector2 mousePos)
	{
		float minDist = _mouseRadius;
		Point nearest = null;

		foreach (var list in new[] { _polygonPoints, _windowPoints, _controlPoints })
		{
			foreach (var p in list)
			{
				float d = p.ToVector2().DistanceTo(mousePos);
				if (d < minDist) { minDist = d; nearest = p; }
			}
		}
		return nearest;
	}

	private List<Point> GetPolygonOf(Point p)
	{
		if (_polygonPoints.Contains(p)) return _polygonPoints;
		if (_windowPoints.Contains(p))  return _windowPoints;
		if (_controlPoints.Contains(p))  return _controlPoints;
		return null;
	}

	private List<Point> AlgoCasteljau(List<Point> P)
	{
		//la liste finale des point de la courbe de Bézier
		List<Point> Q = new List<Point>();
		
		for (int k = 0; k <= pas; k++)
		{
			float t = k / (float)pas;
			//la liste des P(j-1) à chaque itération
			List<Point> P2 = new List<Point>();
			
			for (int j = 1; j < P.Count; j++)
			{
				for (int i = 0; i < P.Count - j; i++)
				{
					if (j == 1)
					{
						P2.Add(new Point());
						P2[i].X = (1 - t) * P[i].X + t * P[i+1].X;
						P2[i].Y = (1 - t) * P[i].Y + t * P[i+1].Y;
					}
					else
					{
						P2[i].X = (1 - t) * P2[i].X + t * P2[i+1].X;
						P2[i].Y = (1 - t) * P2[i].Y + t * P2[i+1].Y;
					}
				}
			}

			Q.Add(P2[0]);
		}
		
		return Q;
	}
}
