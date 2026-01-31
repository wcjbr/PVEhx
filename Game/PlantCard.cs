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

	public override void _Ready()
	{
		// 尝试获取精灵节点
		FindSprite();
		
		// 查找放置区域
		FindPlacementArea();
		
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
		// 假设放置区域是场景根节点的子节点，并且名称为"PlacementArea"
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

	public override void _Process(double delta)
	{
		if (_isDragging && _draggedPlant != null)
		{
			// 更新植物实例位置，使其跟随鼠标
			_draggedPlant.GlobalPosition = GetGlobalMousePosition() + _dragOffset;
			
			// 检查是否可以放置
			UpdatePlacementPreview();
		}
	}
	
	private void UpdatePlacementPreview()
	{
		if (_draggedPlant == null || _placementArea == null) return;
		
		// 使用放置区域检查是否可以放置
		bool canPlaceHere = _placementArea.CanPlacePlantHere(_draggedPlant);
		
		// 更新植物颜色
		_draggedPlant.Modulate = canPlaceHere ? 
			new Color(1, 1, 1, 0.7f) : // 可以放置，半透明白色
			new Color(1, 0.5f, 0.5f, 0.7f); // 不能放置，半透明红色
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
			_draggedPlant.Modulate = new Color(1, 1, 1, 0.7f);
			
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
			if (_placementArea != null && _placementArea.CanPlacePlantHere(_draggedPlant))
			{
				// 可以放置：恢复不透明度，设置种植状态
				_draggedPlant.Modulate = Colors.White;
				_draggedPlant.SetPlanted(true);
				GD.Print($"Plant placed at {_draggedPlant.GlobalPosition}");
			}
			else
			{
				// 不能放置：销毁植物实例
				_draggedPlant.QueueFree();
				GD.Print("Cannot place plant here (outside placement area or overlapping with other plants)");
			}
		}
		
		// 重置状态
		_isDragging = false;
		_draggedPlant = null;
		_dragOffset = Vector2.Zero;
	}
	
	/// <summary>
	/// 检查是否正在拖动
	/// </summary>
	public bool IsDragging()
	{
		return _isDragging;
	}
}
