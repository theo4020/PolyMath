using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node2D
{
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
