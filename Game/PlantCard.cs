using Godot;
using System;

/// <summary>
/// 简化的植物卡片类
/// </summary>
public partial class PlantCard : Node2D
{
	[Export]
	public PackedScene PlantScene { get; set; }
	
	private AnimatedSprite2D _sprite;
	private bool _isDragging = false;
	private Plant _draggedPlant = null;
	private Vector2 _dragOffset = Vector2.Zero;
	
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
			
			// 同时查找网格覆盖层
			FindGridOverlay();
		}
	}

	public override void _Process(double delta)
	{
		if (_isDragging && _draggedPlant != null)
		{
			Vector2 mousePos = GetGlobalMousePosition();
			
			if (_useGridSnapping && _gridSystem != null)
			{
				// 使用网格吸附
				UpdatePlantPositionWithGridSnapping(mousePos, (float)delta);
			}
			else
			{
				// 基本拖动（无网格吸附）
				_draggedPlant.GlobalPosition = mousePos + _dragOffset;
			}
			
			// 检查是否可以放置
			UpdatePlacementPreview();
		}
	}
	
	/// <summary>
	/// 使用网格吸附更新植物位置
	/// </summary>
	private void UpdatePlantPositionWithGridSnapping(Vector2 mousePos, float delta)
	{
		// 找到最近的可用格子
		Vector2I? nearestGrid = _gridSystem.FindNearestAvailableGrid(mousePos);
		
		if (nearestGrid.HasValue)
		{
			// 获取目标格子的中心位置
			Vector2 targetPos = _gridSystem.GridToWorld(nearestGrid.Value.X, nearestGrid.Value.Y);
			
			// 平滑移动到目标位置
			_draggedPlant.GlobalPosition = _draggedPlant.GlobalPosition.Lerp(targetPos, _snapSmoothness);
			
			// 存储当前目标格子用于放置判断
			_currentTargetGrid = nearestGrid.Value;
		}
		else
		{
			// 没有可用格子，跟随鼠标但保持在网格区域内
			if (_gridSystem.IsPointInGridArea(mousePos))
			{
				_draggedPlant.GlobalPosition = mousePos + _dragOffset;
			}
			_currentTargetGrid = null;
		}
	}
	
	private Vector2I? _currentTargetGrid; // 当前目标格子
	private GridOverlay _gridOverlay; // 网格覆盖层引用
	
	private void FindGridOverlay()
	{
		// 查找场景中的网格覆盖层
		var root = GetTree().Root;
		if (root != null)
		{
			_gridOverlay = root.GetNodeOrNull<GridOverlay>("GridOverlay");
			
			if (_gridOverlay == null)
			{
				// 尝试在当前场景中查找
				_gridOverlay = GetTree().CurrentScene?.GetNodeOrNull<GridOverlay>("GridOverlay");
			}
			
			if (_gridOverlay == null)
			{
				// 尝试在父节点中查找
				Node currentNode = this;
				while (currentNode != null && _gridOverlay == null)
				{
					_gridOverlay = currentNode.GetNodeOrNull<GridOverlay>("GridOverlay");
					currentNode = currentNode.GetParent();
				}
			}
		}
	}
	
	private void UpdatePlacementPreview()
	{
		if (_draggedPlant == null || _placementArea == null) return;
		
		// 使用放置区域检查是否可以放置
		bool canPlaceHere = _placementArea.CanPlacePlantHere(_draggedPlant);
		
		// 更新植物颜色
		if (canPlaceHere)
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
			
			if (_useGridSnapping && _gridSystem != null && _currentTargetGrid.HasValue)
			{
				// 网格吸附模式下的放置检查
				canPlace = CanPlaceInGridMode();
				if (canPlace)
				{
					// 强制吸附到格子中心
					finalPosition = _gridSystem.GridToWorld(_currentTargetGrid.Value.X, _currentTargetGrid.Value.Y);
					_draggedPlant.GlobalPosition = finalPosition;
				}
			}
			else if (_placementArea != null)
			{
				// 基本模式下的放置检查
				canPlace = _placementArea.CanPlacePlantHere(_draggedPlant);
			}
			
			if (canPlace)
			{
				// 可以放置：恢复不透明度，设置种植状态
				_draggedPlant.Modulate = Colors.White;
				_draggedPlant.SetPlanted(true);
				
				// 标记格子为已占用
				if (_useGridSnapping && _gridSystem != null && _currentTargetGrid.HasValue)
				{
					_gridSystem.MarkGridOccupied(_currentTargetGrid.Value);
				}
				
				GD.Print($"Plant placed at {finalPosition}");
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
	/// 网格模式下的放置检查
	/// </summary>
	private bool CanPlaceInGridMode()
	{
		if (!_currentTargetGrid.HasValue) return false;
		
		// 检查格子是否可用
		if (!_gridSystem.IsGridAvailable(_currentTargetGrid.Value)) 
			return false;
			
		// 检查是否在网格区域内
		Vector2 gridCenter = _gridSystem.GridToWorld(_currentTargetGrid.Value.X, _currentTargetGrid.Value.Y);
		if (!_gridSystem.IsPointInGridArea(gridCenter))
			return false;
			
		// 如果有放置区域，也进行检查
		if (_placementArea != null)
		{
			// 临时设置植物位置到格子中心进行检查
			Vector2 originalPos = _draggedPlant.GlobalPosition;
			_draggedPlant.GlobalPosition = gridCenter;
			bool canPlace = _placementArea.CanPlacePlantHere(_draggedPlant);
			_draggedPlant.GlobalPosition = originalPos; // 恢复原位置
			return canPlace;
		}
		
		return true;
	}
	
	/// <summary>
	/// 检查是否正在拖动
	/// </summary>
	public bool IsDragging()
	{
		return _isDragging;
	}
}
