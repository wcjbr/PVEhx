using Godot;
using System;

public partial class Plant : Sprite2D
{
	// Called when the node enters the scene tree for the first time.
	int HP;
	bool isPlanted;
	public override void _Ready()
	{
		HP=-1;
		isPlanted=false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
