using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node2D
{
	//Modes
	public enum EMode { Draw, Eraser, MovePoint, MovePolygon }

	//liste est en cours de dessin
	private enum EDrawPhase { Polygon, Window, Done }

	private EMode     _currentMode  = EMode.Draw;
	private EDrawPhase _drawPhase   = EDrawPhase.Polygon;
	
	//Node
	private VBoxContainer _container;

	public EMode CurrentMode
	{
		get => _currentMode;
		set
		{
			_currentMode = value;
			//si on change de mode ce qui est drag est relaché
			_draggedPoint   = null;
			_draggedPolygon = null;
		}
	}

	//Listes polygones
	private List<Point> _polygonPoints = new List<Point>();
	private List<Point> _windowPoints  = new List<Point>();
	private List<Point> _resultPoints  = new List<Point>();

	private bool _polygonClosed = false;
	private bool _windowClosed  = false;

	private Point       _draggedPoint   = null;
	private List<Point> _draggedPolygon = null;
	private Vector2     _dragOffset     = Vector2.Zero;

	//Exports
	[Export] private float  _mouseRadius   = 20f;
	[Export] private Color  _polygonColor  = new Color(0.2f, 0.6f, 1f, 0.4f);
	[Export] private Color  _windowColor   = new Color(1f, 0.6f, 0.2f, 0.4f);
	[Export] private Color  _resultColor   = new Color(0.2f, 1f, 0.4f, 0.7f);
	[Export] private float  _lineWidth     = 2f;
	[Export] private float  _pointRadius   = 6f;

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

		QueueRedraw();
	}

	//Gestion des clics gauche
	private void HandleLeftClick(Vector2 mousePos)
	{
		if (!Input.IsActionJustPressed("ClicGauche")) return;

		if (_container.GetGlobalRect().HasPoint(mousePos)) return;

		switch (_currentMode)
		{
			case EMode.Draw:
				HandleDraw(mousePos);
				break;

			case EMode.Eraser:
				HandleErase(mousePos);
				break;

			case EMode.MovePoint:
				var pt = GetNearestPoint(mousePos);
				if (pt != null)
				{
					_draggedPoint  = pt;
					_dragOffset    = mousePos - pt.ToVector2();
				}
				break;

			case EMode.MovePolygon:
				var anchor = GetNearestPoint(mousePos);
				if (anchor != null)
				{
					_draggedPoint = anchor;
					_draggedPolygon = GetPolygonOf(anchor);
					_dragOffset     = mousePos - anchor.ToVector2();
				}
				break;
		}
	}

	private void HandleDraw(Vector2 mousePos)
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

		if (removed) RecalculateResult();
	}

	//Gestion du clic droit (fermeture du polygone)
	private void HandleRightClick()
	{
		if (!Input.IsActionJustPressed("ClicDroit")) return;
		if (_currentMode != EMode.Draw) return;

		if (_drawPhase == EDrawPhase.Polygon && !_polygonClosed)
		{
			if (_polygonPoints.Count >= 3)
			{
				_polygonClosed = true;
				_drawPhase     = EDrawPhase.Window;
				RecalculateResult();
			}
		}
		else if (_drawPhase == EDrawPhase.Window && !_windowClosed)
		{
			if (_windowPoints.Count >= 3)
			{
				_windowClosed = true;
				_drawPhase    = EDrawPhase.Done;
				RecalculateResult();
			}
		}
	}

	//Gestion du Drag en cours
	private void HandleDrag(Vector2 mousePos)
	{
		if (Input.IsActionJustReleased("ClicGauche"))
		{
			_draggedPoint   = null;
			_draggedPolygon = null;
			return;
		}

		if (!Input.IsActionPressed("ClicGauche")) return;

		if (_draggedPolygon != null && _draggedPoint != null)
		{
			Vector2 anchorPos = _draggedPoint.ToVector2();
			Vector2 newPos    = mousePos - _dragOffset;
			Vector2 delta     = newPos - anchorPos;

			foreach (var p in _draggedPolygon)
			{
				p.X += delta.X;
				p.Y += delta.Y;
			}
			RecalculateResult();
		}
		else if (_draggedPoint != null)
		{
			_draggedPoint.X = mousePos.X - _dragOffset.X;
			_draggedPoint.Y = mousePos.Y - _dragOffset.Y;
			RecalculateResult();
		}
	}

	//Dessin
	public override void _Draw()
	{
		DrawPolygonWithOutline(_polygonPoints, _polygonClosed, _polygonColor);
		DrawPolygonWithOutline(_windowPoints,  _windowClosed,  _windowColor);

		if (_resultPoints.Count >= 3)
		{
			DrawPolygonWithOutline(_resultPoints, true, _resultColor);
		}

		DrawPoints(_polygonPoints, _polygonColor);
		DrawPoints(_windowPoints,  _windowColor);
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
		_polygonClosed  = false;
		_windowClosed   = false;
		_drawPhase      = EDrawPhase.Polygon;
		_draggedPoint   = null;
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

		foreach (var list in new[] { _polygonPoints, _windowPoints })
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
		return null;
	}
}
