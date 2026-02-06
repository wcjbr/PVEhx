using Godot;
using System.Collections.Generic;

/// <summary>
/// 植物卡片管理器
/// 这个节点应该放在场景中，它会自动生成卡片
/// </summary>
public partial class PlantCardManager : Node2D
{
	//[Export]
	//public Godot.Collections.Array<PackedScene> PlantCardScene { get; set; } // 植物卡片模板场景

	[Export]
	public Vector2 StartPosition = new Vector2(100, 700); // 起始位置

	[Export]
	public float Spacing = 120; // 卡片间距

	private List<PlantCard> _cards = new List<PlantCard>();

	private GameManager _gameManager;

	public override void _Ready()
	{
		// 查找游戏管理器
		FindGameManager();
		// 生成卡片
		GenerateCards();
	}

	/// <summary>
	/// 生成所有卡片
	/// </summary>
	private void GenerateCards()
	{
		if (_gameManager.PlantCardScene == null)
		{
			GD.PrintErr("PlantCardScene is not set! Please assign a PlantCard scene in the inspector.");
			return;
		}
		int i = 0;
		foreach (PackedScene packedScene in _gameManager.PlantCardScene)
		{
			var card = packedScene.Instantiate() as PlantCard;
			if (card != null)
			{
				AddChild(card);
				card.Position = StartPosition + new Vector2(i * Spacing, 0);
				_cards.Add(card);

				GD.Print($"Created card {i} at position {card.Position}");
			}
			i++;
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

	private void FindGameManager()
	{
		// 查找场景中的游戏管理器
		var root = GetTree().Root;
		if (root != null)
		{
			_gameManager = root.GetNodeOrNull<GameManager>("GameManager");

			if (_gameManager == null)
			{
				// 尝试在当前场景中查找
				_gameManager = GetTree().CurrentScene?.GetNodeOrNull<GameManager>("GameManager");
			}

			if (_gameManager == null)
			{
				// 尝试在父节点中查找
				Node currentNode = this;
				while (currentNode != null && _gameManager == null)
				{
					_gameManager = currentNode.GetNodeOrNull<GameManager>("GameManager");
					currentNode = currentNode.GetParent();
				}
			}
		}

		if (_gameManager == null)
		{
			GD.PrintErr("GameManager not found in scene!");
		}
		else
		{
			GD.Print($"Found GameManager: {_gameManager.Name}");
		}
	}

}
