using Godot;

/// <summary>
/// 游戏管理器
/// </summary>
public partial class GameManager : Node
{
	private int Sun=200;
	private int Score;
	private int Level; 
	
	[Export]
	public Godot.Collections.Array<PackedScene> PlantCardScene { get; set; }
	public override void _Ready()
	{
		GD.Print("GameManager is ready.");
	}
	public void AddSun(int amount)
	{
		Sun += amount;
		GD.Print($"Sun increased by {amount}. Total Sun: {Sun}");
	}

	public bool SpendSun(int amount)
	{
		if (Sun >= amount)
		{
			Sun -= amount;
			GD.Print($"Spent {amount} Sun. Remaining Sun: {Sun}");
			return true;
		}
		else
		{
			GD.Print("Not enough Sun!");
			return false;
		}
	}

	public int GetSun()
	{
		return Sun;
	}

	public override void _Process(double delta)
	{
		// 游戏管理器的每帧逻辑可以放在这里
		Label sunLabel=GetNodeOrNull<Label>("SunLabel");
		if (sunLabel != null)
		{
			sunLabel.Text = $"{this.Sun}";
		}
		else
		{
			GD.PrintErr("SunLabel not found in GameManager scene!");
		}
	}
}
