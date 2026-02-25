using Godot;
using System;

public partial class Point : Node
{
	private float x;
	private float y;

	public enum EOwner
	{
		None,
		Polygon,
		Bezier,
		Window,
		Result
	}

	private EOwner owner;
	private Color color;

	public Point()
	{
		x = 0;
		y = 0;
		owner = EOwner.None;
		color = new Color(0, 0, 0);
	}

	public Point(float x, float y, EOwner owner = EOwner.None, Color color = new Color())
	{
		this.x = x;
		this.y = y;
		this.owner = owner;
		this.color = color;
	}

	public Point(Vector2 point, EOwner owner = EOwner.None, Color color = new Color())
	{
		this.x = point.X;
		this.y = point.Y;
		this.owner = owner;
		this.color = color;
	}

	public float X
	{
		get => x;
		set => x = value;
	}

	public float Y
	{
		get => y;
		set => y = value;
	}

	public EOwner Owner
	{
		get => owner;
		set => owner = value;
	}

	public Color Color
	{
		get => color;
		set => color = value;
	}

	public Vector2 ToVector2() => new Vector2(x, y);
}
