using Godot;
using System;

/// <summary>
/// 植物卡片类
/// </summary>
public partial class PlantCard : Node2D
{
	[Export]
	public PackedScene PlantScene { get; set; }
	private AnimatedSprite2D _sprite;
	private bool _isDragging = false;
	private Plant _draggedPlant = null;
	private Vector2 _dragOffset = Vector2.Zero;

	private GameManager _gameManager; // 游戏管理器引用

	private PlacementArea _placementArea; // 放置区域引用
	private GridSystem _gridSystem; // 网格系统引用
	private bool _useGridSnapping = true; // 是否启用格子吸附
	private float _snapSmoothness = 0.3f; // 吸附平滑度

	public override void _Ready()
	{
		// 尝试获取精灵节点
		FindSprite();

		// 查找放置区域
		FindPlacementArea();

		// 查找网格系统
		FindGridSystem();

		FindGameManager();


		if (_sprite != null)
		{
			if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation("Carded"))
			{
				_sprite.Animation = "Carded";
				_sprite.Play();
			}
		}
		else
		{
			GD.PrintErr($"PlantCard._Ready: No AnimatedSprite2D found in {Name}!");
		}

		// 调试信息
		GD.Print($"PlantCard ready. PlantScene = {PlantScene != null}, Sprite = {_sprite != null}");
	}

	private void FindSprite()
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

	private void FindPlacementArea()
	{
		// 查找场景中的放置区域
		var root = GetTree().Root;
		if (root != null)
		{
			// 尝试几种常见的查找方式
			_placementArea = root.GetNodeOrNull<PlacementArea>("PlacementArea");

			if (_placementArea == null)
			{
				// 尝试在当前场景中查找
				_placementArea = GetTree().CurrentScene?.GetNodeOrNull<PlacementArea>("PlacementArea");
			}

			if (_placementArea == null)
			{
				// 尝试在父节点中查找
				Node currentNode = this;
				while (currentNode != null && _placementArea == null)
				{
					_placementArea = currentNode.GetNodeOrNull<PlacementArea>("PlacementArea");
					currentNode = currentNode.GetParent();
				}
			}
		}

		if (_placementArea == null)
		{
			GD.PrintErr("PlacementArea not found in scene!");
		}
		else
		{
			GD.Print($"Found PlacementArea: {_placementArea.Name}");
		}
	}

	private void FindGridSystem()
	{
		// 查找场景中的网格系统
		var root = GetTree().Root;
		if (root != null)
		{
			_gridSystem = root.GetNodeOrNull<GridSystem>("GridSystem");

			if (_gridSystem == null)
			{
				// 尝试在当前场景中查找
				_gridSystem = GetTree().CurrentScene?.GetNodeOrNull<GridSystem>("GridSystem");
			}

			if (_gridSystem == null)
			{
				// 尝试在父节点中查找
				Node currentNode = this;
				while (currentNode != null && _gridSystem == null)
				{
					_gridSystem = currentNode.GetNodeOrNull<GridSystem>("GridSystem");
					currentNode = currentNode.GetParent();
				}
			}
		}

		if (_gridSystem == null)
		{
			GD.Print("GridSystem not found, falling back to basic placement");
			_useGridSnapping = false;
		}
		else
		{
			GD.Print($"Found GridSystem: {_gridSystem.Name}");
			_useGridSnapping = true;
		}
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

	public override void _Process(double delta)
	{
		if (_isDragging && _draggedPlant != null)
		{
			Vector2 mousePos = GetGlobalMousePosition();

			// 基本拖动（无网格吸附），直接跟随鼠标
			_draggedPlant.GlobalPosition = mousePos + _dragOffset;

			// 检查是否可以放置（基于当前位置）
			UpdatePlacementPreview();
		}
	}

	private Vector2I? _currentTargetGrid; // 当前目标格子

	private void UpdatePlacementPreview()
	{
		if (_draggedPlant == null || _placementArea == null) return;

		// 使用放置区域检查是否可以放置
		bool canPlaceHere = _placementArea.CanPlacePlantHere(_draggedPlant);

		// 更新植物颜色
		if (canPlaceHere&&_gameManager.GetSun()>=_draggedPlant.Sun)
		{
			// 可以放置，显示正常颜色（略带透明）
			_draggedPlant.Modulate = new Color(1, 1, 1, 0.8f);
		}
		else
		{
			// 不能放置，显示明显的红色滤镜
			_draggedPlant.Modulate = new Color(1, 0.3f, 0.3f, 0.8f); // 更红更明显
		}
	}

	public override void _Input(InputEvent @event)
	{
		// 只处理鼠标按钮事件
		if (@event is InputEventMouseButton mouseEvent)
		{
			int buttonIndex = (int)mouseEvent.ButtonIndex;

			// 鼠标按下：开始拖动
			if (mouseEvent.Pressed && buttonIndex == (int)MouseButton.Left)
			{
				// 检查是否点击了这个卡片
				Vector2 mousePos = GetGlobalMousePosition();

				// 只有当卡片有精灵且鼠标在精灵上时才处理
				if (_sprite != null && IsPointInsideSprite(mousePos))
				{
					// 开始拖动
					StartDragging(mousePos);

					// 标记事件已处理，防止其他节点响应
					GetViewport().SetInputAsHandled();
				}
			}
			// 鼠标释放：停止拖动
			else if (!mouseEvent.Pressed && buttonIndex == (int)MouseButton.Left && _isDragging)
			{
				StopDragging();

				// 标记事件已处理
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private bool IsPointInsideSprite(Vector2 point)
	{
		if (_sprite == null || _sprite.SpriteFrames == null || string.IsNullOrEmpty(_sprite.Animation))
			return false;

		// 获取当前帧的纹理
		var tex = _sprite.SpriteFrames.GetFrameTexture(_sprite.Animation, _sprite.Frame);
		if (tex == null)
		{
			return false;
		}

		// 计算精灵的边界矩形
		Vector2 size = tex.GetSize() * _sprite.GlobalScale;
		Rect2 rect = new Rect2(_sprite.GlobalPosition - size * 0.5f, size);

		// 检查点是否在矩形内
		return rect.HasPoint(point);
	}

	private void StartDragging(Vector2 mousePos)
	{
		if (PlantScene == null)
		{
			GD.PrintErr("PlantScene is not set!");
			return;
		}

		// 创建植物实例
		_draggedPlant = PlantScene.Instantiate() as Plant;
		_draggedPlant.Scale = new Vector2(0.75f, 0.75f);
		if (_draggedPlant == null)
		{
			GD.PrintErr("Failed to instantiate plant!");
			return;
		}
		// 添加到当前场景（不是添加到卡片，这样植物不会被卡片位置限制）
		Node parent = GetTree().CurrentScene;
		if (parent != null)
		{
			parent.AddChild(_draggedPlant);


			// 重要：使用鼠标位置作为起始位置
			_draggedPlant.GlobalPosition = mousePos;

			// 设置半透明，表示正在拖动
			_draggedPlant.Modulate = new Color(1, 1, 1, 0.8f);

			//_draggedPlant.Scale = new Vector2(0.75f, 0.75f);

			// 设置拖动状态
			_isDragging = true;

			// 计算偏移量（鼠标位置与植物位置的差值）
			// 这样植物会保持在鼠标的相对位置
			_dragOffset = Vector2.Zero; // 从鼠标位置开始，不偏移

			GD.Print($"Started dragging plant from {Name} at {mousePos}");
		}
		else
		{
			GD.PrintErr("Cannot find current scene!");
		}
	}

	private void StopDragging()
	{
		if (_draggedPlant != null)
		{
			bool canPlace = false;
			Vector2 finalPosition = _draggedPlant.GlobalPosition;

			// 检查是否在网格区域内，如果是，则找到对应的网格位置
			if (_useGridSnapping && _gridSystem != null && _gridSystem.IsPointInGridArea(_draggedPlant.GlobalPosition))
			{
				// 获取当前鼠标位置对应的网格坐标
				Vector2I gridPos = _gridSystem.WorldToGrid(_draggedPlant.GlobalPosition);

				// 检查该网格是否可用
				if (_gridSystem.IsGridAvailable(gridPos))
				{
					// 检查放置区域是否允许放置（双重检查）
					if (_placementArea == null || _placementArea.CanPlacePlantHere(_draggedPlant))
					{
						canPlace = true;
						// 强制吸附到格子中心
						finalPosition = _gridSystem.GridToWorld(gridPos.X, gridPos.Y);
						_draggedPlant.GlobalPosition = finalPosition;
						_currentTargetGrid = gridPos;
					}
				}
			}
			else if (_placementArea != null)
			{
				// 基本模式下的放置检查
				canPlace = _placementArea.CanPlacePlantHere(_draggedPlant);
			}

			if (canPlace)
			{
				// 尝试种植植物（这会检查阳光是否足够）
				if (_draggedPlant.SetPlanted(true))
				{
					// 种植成功：恢复不透明度，设置种植状态
					_draggedPlant.Modulate = Colors.White;

					// 标记格子为已占用
					if (_useGridSnapping && _gridSystem != null && _currentTargetGrid.HasValue)
					{
						_gridSystem.MarkGridOccupied(_currentTargetGrid.Value);
					}

					GD.Print($"Plant placed at {finalPosition}");
				}
				else
				{
					// 阳光不足，无法种植：销毁植物实例
					_draggedPlant.QueueFree();
					GD.Print("Not enough sun to place plant");
				}
			}
			else
			{
				// 不能放置：销毁植物实例
				_draggedPlant.QueueFree();
				GD.Print("Cannot place plant here");
			}
		}

		// 重置状态
		_isDragging = false;
		_draggedPlant = null;
		_dragOffset = Vector2.Zero;
		_currentTargetGrid = null;
	}



	/// <summary>
	/// 检查是否正在拖动
	/// </summary>
	public bool IsDragging()
	{
		return _isDragging;
	}
}
