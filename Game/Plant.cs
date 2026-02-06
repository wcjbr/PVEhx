using Godot;

/// <summary>
/// 植物基类
/// </summary>
public partial class Plant : Node2D
{
	protected AnimatedSprite2D _sprite;
	protected bool _isPlanted = false;

	[Export]
	public int Sun = 100;  // 默认阳光消耗值，可以根据具体植物类型调整
	private GameManager _gameManager; // 游戏管理器引用

	[Export]
	public int _hp = 100; // 植物生命值

	public RandomNumberGenerator _rng;
	public override void _Ready()
	{
		// 尝试多种方法获取AnimatedSprite2D
		FindSprite();
		// 获取游戏管理器
		FindGameManager();

		_rng = new  RandomNumberGenerator();		

		if (_sprite == null)
		{
			GD.PrintErr($"Plant._Ready: No AnimatedSprite2D found in {Name}!");
		}
	}

	/// <summary>
	/// 尝试找到场景中的AnimatedSprite2D节点
	/// </summary>
	protected virtual void FindSprite()
	{
		// 方法1: 尝试按名称获取
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		// 方法2: 如果失败，尝试获取任何AnimatedSprite2D类型的子节点
		if (_sprite == null)
		{
			foreach (var child in GetChildren())
			{
				if (child is AnimatedSprite2D sprite)
				{
					_sprite = sprite;
					break;
				}
			}
		}
	}

	/// <summary>
	/// 尝试找到场景中的GameManager节点
	/// </summary>
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


	public override void _Process(double delta)
	{
		UpdateAnimation();
	}

	protected virtual void UpdateAnimation()
	{
		if (_sprite != null && _sprite.SpriteFrames != null)
		{
			if (_isPlanted && _sprite.SpriteFrames.HasAnimation("Planted"))
			{
				if (_sprite.Animation != "Planted")
				{
					Scale = new Vector2(1f, 1f);
					_sprite.Animation = "Planted";
					_sprite.Play();
				}
			}
			else if (_sprite.SpriteFrames.HasAnimation("Carded"))
			{
				if (_sprite.Animation != "Carded")
				{
					Scale = new Vector2(0.75f, 0.75f);
					_sprite.Animation = "Carded";
					_sprite.Play();
				}
			}
		}
	}

	public virtual bool SetPlanted(bool planted)
	{
		if (_gameManager.SpendSun(Sun) == false)
		{
			return false;
		}
		_isPlanted = planted;

		// 如果种植后需要改变动画，可以在这里调用
		if (planted && _sprite != null)
		{
			UpdateAnimation();
		}
		return true;
	}

	public bool IsPlanted()
	{
		return _isPlanted;
	}
	
	//<summary>
	// 植物被攻击时调用此方法
	//</summary>
	public virtual void Onattacked(int damage)
	{
		// 这里可以实现植物被攻击时的逻辑
		GD.Print($"{Name} was attacked and took {damage} damage!");
		_hp -= damage;
		if (_hp <= 0)
		{
			GD.Print($"{Name} has been destroyed!");
			QueueFree();
		}
	}
	///<summary>
	///获取植物当前生命值
	///</summary>
	public int GetHP()
	{
		return _hp;
	}
}
