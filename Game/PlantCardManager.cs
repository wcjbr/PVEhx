using Godot;
using System.Collections.Generic;

/// <summary>
/// 植物卡片管理器 - 简单版本
/// 这个节点应该放在场景中，它会自动生成卡片
/// </summary>
public partial class PlantCardManager : Node2D
{
	[Export]
	public PackedScene PlantCardScene { get; set; } // 植物卡片模板场景
	
	[Export]
	public int CardCount = 7; // 要生成的卡片数量
	
	[Export]
	public Vector2 StartPosition = new Vector2(100, 700); // 起始位置
	
	[Export]
	public float Spacing = 120; // 卡片间距
	
	private List<PlantCard> _cards = new List<PlantCard>();

	public override void _Ready()
	{
		// 生成卡片
		GenerateCards();
	}
	
	/// <summary>
	/// 生成所有卡片
	/// </summary>
	private void GenerateCards()
	{
		if (PlantCardScene == null)
		{
			GD.PrintErr("PlantCardScene is not set! Please assign a PlantCard scene in the inspector.");
			return;
		}
		
		for (int i = 0; i < CardCount; i++)
		{
			var card = PlantCardScene.Instantiate() as PlantCard;
			if (card != null)
			{
				AddChild(card);
				card.Position = StartPosition + new Vector2(i * Spacing, 0);
				_cards.Add(card);
				
				GD.Print($"Created card {i} at position {card.Position}");
			}
		}
		
		GD.Print($"Total cards created: {_cards.Count}");
	}
	
	/// <summary>
	/// 移除所有卡片
	/// </summary>
	public void RemoveAllCards()
	{
		foreach (var card in _cards)
		{
			if (card != null && IsInstanceValid(card))
			{
				card.QueueFree();
			}
		}
		_cards.Clear();
	}
	
	/// <summary>
	/// 重新生成卡片
	/// </summary>
	public void RegenerateCards()
	{
		RemoveAllCards();
		GenerateCards();
	}
	
	/// <summary>
	/// 获取卡片数量
	/// </summary>
	public int GetCardCount()
	{
		return _cards.Count;
	}
}
