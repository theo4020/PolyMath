using Godot;

public partial class ControlScript : Control
{
	private Button _btnDrawPolygon;
	private Button _btnDrawBezier;
	private Button _btnShowPascal;
	private Button _btnShowCasteljau;
	private Button _btnEraser;
	private Button _btnMovePoint;
	private Button _btnMovePolygon;
	private Button _btnReset;

	private Main _main;

	public override void _Ready()
	{
		// Récupère le noeud Main
		_main = GetNode<Main>("../Canva");

		// Récupère les boutons
		_btnDrawPolygon = GetNode<Button>("VBoxContainer/BtnDrawPolygon");
		_btnDrawBezier = GetNode<Button>("VBoxContainer/BtnDrawBezier");
		_btnShowPascal = GetNode<Button>("VBoxContainer/BtnShowPascal");
		_btnShowCasteljau = GetNode<Button>("VBoxContainer/BtnShowCasteljau");
		_btnEraser = GetNode<Button>("VBoxContainer/BtnEraser");
		_btnMovePoint = GetNode<Button>("VBoxContainer/BtnMovePoint");
		_btnMovePolygon = GetNode<Button>("VBoxContainer/BtnMovePolygon");
		_btnReset = GetNode<Button>("VBoxContainer/BtnReset");

		_btnDrawPolygon.Pressed += () => SetMode(Main.EMode.DrawPolygon);
		_btnDrawBezier.Pressed += () => SetMode(Main.EMode.DrawBezier);
		_btnShowPascal.Pressed += () => _main.HandleShowPascal();
		_btnShowCasteljau.Pressed += () => _main.HandleShowCasteljau();
		_btnEraser.Pressed += () => SetMode(Main.EMode.Eraser);
		_btnMovePoint.Pressed += () => SetMode(Main.EMode.MovePoint);
		_btnMovePolygon.Pressed += () => SetMode(Main.EMode.MovePolygon);
		_btnReset.Pressed += OnResetPressed;

		// Mode par défaut
		SetMode(Main.EMode.DrawBezier);
	}

	private void SetMode(Main.EMode mode)
	{
		_main.CurrentMode = mode;
		

		// Feedback visuel : désactive le bouton du mode actif
		_btnDrawPolygon.Disabled = (mode == Main.EMode.DrawPolygon);
		_btnDrawBezier.Disabled = (mode == Main.EMode.DrawBezier);
		_btnEraser.Disabled = (mode == Main.EMode.Eraser);
		_btnMovePoint.Disabled = (mode == Main.EMode.MovePoint);
		_btnMovePolygon.Disabled = (mode == Main.EMode.MovePolygon);
	}

	private void OnResetPressed()
	{
		_main.ResetPolygons();
		SetMode(Main.EMode.DrawBezier);
	}
}
