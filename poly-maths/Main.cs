using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node2D
{
	private int pixelWidth = 192;
	private int pixelHeight = 108;

	private Image img;
	private ImageTexture imgTexture;
	private TextureRect textRect;
	
	private List<Vector2> polygonPoints = new List<Vector2>();
	private List<Vector2> windowPoints = new List<Vector2>();
	private List<Vector2> resultPoints = new List<Vector2>();
	private bool polygonClosed = false;
	private bool windowClosed = false;
	private bool resultClosed = false;
	
	[Export] private int radius = 5;
	
	[Export] private Color polygonColor;
	[Export] private Color windowColor;
	[Export] private Color resultColor;

	public int PixelWidth
	{
		get => pixelWidth;
		set => pixelWidth = value;
	}
	
	public int PixelHeight
	{
		get => pixelHeight;
		set => pixelHeight = value;
	}

	public override void _Ready()
	{
		base._Ready();
		
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		
		if (Input.IsActionPressed("Quitter"))
		{
			GetTree().Quit();
		}
		
		if (Input.IsActionJustPressed("ClicGauche"))
		{
			if (!polygonClosed)
			{
				polygonPoints.Add(GetViewport().GetMousePosition());
				QueueRedraw();
			}
			else if (!windowClosed)
			{
				List<Vector2> temp = new List<Vector2>(windowPoints);
				temp.Add(GetViewport().GetMousePosition());
				if (IsConvex(temp))
				{
					windowPoints.Add(GetViewport().GetMousePosition());
					QueueRedraw();
				}
			}
		}
		
		if (Input.IsActionJustPressed("ClicDroit"))
		{
			if (!polygonClosed)
			{
				if (polygonPoints.Count >= 3)
				{
					polygonClosed = true;
					QueueRedraw();
				}
			}
			else if (!windowClosed)
			{
				if (windowPoints.Count >= 3)
				{
					windowClosed = true;
					resultPoints = AlgoSH(polygonPoints, windowPoints);
					resultClosed = true;
					QueueRedraw();
				}
			}
		}
	}
	
	public override void _Draw()
	{
		// Dessine les segments entre les points
		for (int i = 0; i < polygonPoints.Count - 1; i++)
		{
			DrawLine(polygonPoints[i], polygonPoints[i + 1], polygonColor, 2);
		}
		
		for (int i = 0; i < windowPoints.Count - 1; i++)
		{
			DrawLine(windowPoints[i], windowPoints[i + 1], windowColor, 2);
		}

		if (resultClosed)
		{
			for (int i = 0; i < resultPoints.Count - 1; i++)
			{
				DrawLine(resultPoints[i], resultPoints[i + 1], resultColor, 2);
			}
			
			DrawLine(resultPoints[resultPoints.Count - 1], resultPoints[0], resultColor, 2);

			DrawPolygon(resultPoints.ToArray(), new Color[] { resultColor });
		}
		

		if (polygonClosed)
		{
			DrawLine(polygonPoints[polygonPoints.Count - 1], polygonPoints[0], polygonColor, 2);

			if (!resultClosed)
			{
				DrawPolygon(polygonPoints.ToArray(), new Color[] { polygonColor });
			}
		}
		
		if (windowClosed)
		{
			DrawLine(windowPoints[windowPoints.Count - 1], windowPoints[0], windowColor, 2);

			if (!resultClosed)
			{
				DrawPolygon(windowPoints.ToArray(), new Color[] { windowColor });
			}
		}

		// affiche des cercles aux sommets
		if (!resultClosed)
		{
			foreach (var p in polygonPoints)
			{
				DrawCircle(p, radius, new Color(0, 0, 0));
			}
			foreach (var p in windowPoints)
			{
				DrawCircle(p, radius, new Color(0, 0, 0));
			}
		}
		foreach (var p in resultPoints)
		{
			DrawCircle(p, radius, new Color(0, 0, 0));
		}
	}
	
	public void ResetPolygons()
	{
		polygonPoints.Clear();
		windowPoints.Clear();
		resultPoints.Clear();
		polygonClosed = false;
		windowClosed = false;
		QueueRedraw();
	}
	
	//ne marche pas
	private bool IsConvex(List<Vector2> pts)
	{
		if (pts.Count < 4)
			return true; // moins de 4 points = convexe

		bool gotNegative = false;
		bool gotPositive = false;

		int n = pts.Count;

		for (int i = 0; i < n; i++)
		{
			Vector2 a = pts[i];
			Vector2 b = pts[(i + 1) % n];
			Vector2 c = pts[(i + 2) % n];

			float cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
			if (cross < 0) gotNegative = true;
			else if (cross > 0) gotPositive = true;

			if (gotNegative && gotPositive) return false; // concave détecté
		}

		return true;
	}

	
	List<Vector2> AlgoSH(List<Vector2> P, List<Vector2> F)
	{
		Vector2 S = new Vector2(), f = new Vector2(), I;
		List<Vector2> tempP = new List<Vector2>(P);

		for (int i = 0; i < F.Count - 1; i++) {
			List<Vector2> PS = new List<Vector2>();
			for (int j = 0; j < tempP.Count; j++) {
				if (j == 1) {
					f = tempP[j];
				}
				else if (coupe(S, tempP[j], F[i], F[i+1])) {
					I = intersection(S, tempP[j], F[i], F[i+1]);
					PS.Add(I);
				}
				S = tempP[j];
				if (visible(S, F[i], F[i+1])) {
					PS.Add(S);
				}
			}

			if (PS.Count > 0) {
				if (coupe(S, f, F[i], F[i+1])) {
					I = intersection(S, f, F[i], F[i+1]);
					PS.Add(I);
				}

				tempP = new List<Vector2>(PS);
			}
		}

		for (int i = 0; i < tempP.Count - 1; i++)
		{
			GD.Print("PS : ",tempP[i]);
		}
		return tempP;
	}

	//retourne un booléen si S est visible par rapport à la droite (F1F2)
	bool visible(Vector2 S, Vector2 F1, Vector2 F2) {
		//Vecteur F1F2
		Vector2 F1F2 = new Vector2(F2.X - F1.X, F2.Y - F1.Y);
		//Vecteur F1S
		Vector2 F1S = new Vector2(S.X - F1.X, S.Y - F1.Y);
		return (F1S.X * F1F2.Y - F1S.Y * F1F2.X) > 0;
	}

	// retourne un booléen suivant l'intersection possible entre le côté [SP] du polygone et le bord prolongé (une droite) (F1F2) de la fenêtre
	bool coupe(Vector2 S, Vector2 P, Vector2 F1, Vector2 F2) {
		return visible(S, F1, F2) && visible(P, F1, F2);
	}

	// retourne le point d'intersection entre le segement [SP] et la droite (F1F2)
	Vector2 intersection(Vector2 P1, Vector2 P2, Vector2 P3, Vector2 P4) {
		//Matrice A
		int a = (int)(P2.X - P1.X), b = (int)(P3.X - P4.X);
		int c = (int)(P2.Y - P1.Y), d = (int)(P3.Y - P4.Y);
		//
		int detA = a*d - b*c;
		if (detA == 0) {
			return P1;
		}
		//Matrice A^-1
		List<List<int>> X = new List<List<int>>()
		{
			new List<int>() { (1/detA) * d , (1/detA) * (-b) },
			new List<int>() { (1/detA) * (-c) , (1/detA) * a }
		};
		
		int BX = (int)(P3.X - P1.X);
		int BY = (int)(P3.Y - P1.Y);
		int t = X[0][0] * BX + X[0][1] * BY;
		Vector2 Result = new Vector2(P1.X + (P2.X * t) - (P1.X * t) , P1.Y + (P2.Y * t) - (P1.Y * t) );
		GD.Print("intersection : ", Result);
		return Result;

	}

	
}
