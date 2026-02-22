using Godot;

public partial class ControlScript : Control
{
	private Button _btnDraw;
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
		_btnDraw = GetNode<Button>("VBoxContainer/BtnDraw");
		_btnEraser = GetNode<Button>("VBoxContainer/BtnEraser");
		_btnMovePoint = GetNode<Button>("VBoxContainer/BtnMovePoint");
		_btnMovePolygon = GetNode<Button>("VBoxContainer/BtnMovePolygon");
		_btnReset = GetNode<Button>("VBoxContainer/BtnReset");

		_btnDraw.Pressed += () => SetMode(Main.EMode.Draw);
		_btnEraser.Pressed += () => SetMode(Main.EMode.Eraser);
		_btnMovePoint.Pressed += () => SetMode(Main.EMode.MovePoint);
		_btnMovePolygon.Pressed += () => SetMode(Main.EMode.MovePolygon);
		_btnReset.Pressed += OnResetPressed;

		// Mode par défaut
		SetMode(Main.EMode.Draw);
	}

	private void SetMode(Main.EMode mode)
	{
		_main.CurrentMode = mode;

		// Feedback visuel : désactive le bouton du mode actif
		_btnDraw.Disabled = (mode == Main.EMode.Draw);
		_btnEraser.Disabled = (mode == Main.EMode.Eraser);
		_btnMovePoint.Disabled = (mode == Main.EMode.MovePoint);
		_btnMovePolygon.Disabled = (mode == Main.EMode.MovePolygon);
	}

	private void OnResetPressed()
	{
		_main.ResetPolygons();
		SetMode(Main.EMode.Draw);
	}
}
