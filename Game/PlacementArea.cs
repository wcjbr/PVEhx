using Godot;
using System.Collections.Generic;

/// <summary>
/// 放置区域，用于定义植物可以放置的区域
/// </summary>
public partial class PlacementArea : Area2D
{
	[Export]
	public bool ShowDebugVisual = true; // 是否显示调试视觉
	
	[Export]
	public Color ValidColor = new Color(0, 1, 0, 0.3f); // 有效区域颜色
	[Export]
	public Color InvalidColor = new Color(1, 0, 0, 0.3f); // 无效区域颜色
	
	private List<Plant> _plantsInArea = new List<Plant>();
	private ColorRect _debugRect;
	private GridSystem _gridSystem; // 网格系统引用

	public override void _Ready()
	{
		// 连接信号
		AreaEntered += OnAreaEntered;
		AreaExited += OnAreaExited;
		
		// 查找网格系统
		FindGridSystem();
		
		// 创建调试视觉（可选）
		if (ShowDebugVisual)
		{
			CreateDebugVisual();
		}
	}
	
	/// <summary>
	/// 查找网格系统
	/// </summary>
	private void FindGridSystem()
	{
		var root = GetTree().Root;
		if (root != null)
		{
			_gridSystem = root.GetNodeOrNull<GridSystem>("GridSystem");
			
			if (_gridSystem == null)
			{
				_gridSystem = GetTree().CurrentScene?.GetNodeOrNull<GridSystem>("GridSystem");
			}
			
			if (_gridSystem == null)
			{
				Node currentNode = this;
				while (currentNode != null && _gridSystem == null)
				{
					_gridSystem = currentNode.GetNodeOrNull<GridSystem>("GridSystem");
					currentNode = currentNode.GetParent();
				}
			}
		}
		
		if (_gridSystem != null)
		{
			GD.Print($"PlacementArea found GridSystem: {_gridSystem.Name}");
		}
	}
	
	private void CreateDebugVisual()
	{
		_debugRect = new ColorRect();
		AddChild(_debugRect);
		
		// 获取碰撞形状的大小
		var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collisionShape != null && collisionShape.Shape is RectangleShape2D rectShape)
		{
			Vector2 size = rectShape.Size;
			_debugRect.Size = size;
			_debugRect.Position = -size * 0.5f; // 中心对齐
			_debugRect.Color = ValidColor;
		}
	}
	
	private void OnAreaEntered(Area2D area)
	{
		// 检查进入的区域是否是植物
		if (area.GetParent() is Plant plant)
		{
			_plantsInArea.Add(plant);
			GD.Print($"Plant entered placement area: {plant.Name}");
			
			// 更新植物状态（如果正在拖动）
			if (plant.Modulate.A < 1.0f) // 半透明表示正在拖动
			{
				plant.Modulate = new Color(1, 1, 1, 0.8f); // 可以放置，半透明正常色
			}
		}
	}
	
	private void OnAreaExited(Area2D area)
	{
		// 检查退出的区域是否是植物
		if (area.GetParent() is Plant plant)
		{
			_plantsInArea.Remove(plant);
			GD.Print($"Plant exited placement area: {plant.Name}");
			
			// 更新植物状态（如果正在拖动）
			if (plant.Modulate.A < 1.0f) // 半透明表示正在拖动
			{
				plant.Modulate = new Color(1, 0.3f, 0.3f, 0.8f); // 不能放置，明显的红色滤镜
			}
		}
	}
	
	/// <summary>
	/// 检查位置是否在放置区域内
	/// </summary>
	public bool IsPointInside(Vector2 globalPoint)
	{
		// 转换为局部坐标
		Vector2 localPoint = ToLocal(globalPoint);
		
		// 检查是否在碰撞形状内
		var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collisionShape != null && collisionShape.Shape is RectangleShape2D rectShape)
		{
			Rect2 rect = new Rect2(-rectShape.Size * 0.5f, rectShape.Size);
			return rect.HasPoint(localPoint);
		}
		
		return false;
	}
	
	/// <summary>
	/// 检查植物是否可以放置在此处（考虑网格系统）
	/// </summary>
	public bool CanPlacePlantHere(Plant plant)
	{
		// 如果有网格系统，优先使用网格检查
		if (_gridSystem != null)
		{
			return CanPlacePlantWithGrid(plant);
		}
		
		// 否则使用原有的检查方式
		return CanPlacePlantBasic(plant);
	}
	
	/// <summary>
	/// 使用网格系统的植物放置检查
	/// </summary>
	private bool CanPlacePlantWithGrid(Plant plant)
	{
		// 检查是否在网格区域内
		if (!_gridSystem.IsPointInGridArea(plant.GlobalPosition))
			return false;
			
		// 找到对应的网格坐标
		Vector2I gridPos = _gridSystem.WorldToGrid(plant.GlobalPosition);
		
		// 检查格子是否可用
		if (!_gridSystem.IsGridAvailable(gridPos))
			return false;
			
		// 检查是否与其他植物重叠（使用原有的碰撞检测）
		return !HasOverlapWithExistingPlants(plant);
	}
	
	/// <summary>
	/// 基本的植物放置检查（原有逻辑）
	/// </summary>
	private bool CanPlacePlantBasic(Plant plant)
	{
		// 首先检查是否在区域内
		if (!IsPointInside(plant.GlobalPosition))
			return false;
			
		// 检查是否与其他植物重叠
		return !HasOverlapWithExistingPlants(plant);
	}
	
	/// <summary>
	/// 检查是否与其他植物重叠
	/// </summary>
	private bool HasOverlapWithExistingPlants(Plant plant)
	{
		var plantCollision = plant.GetNodeOrNull<Area2D>("PlantCollision");
		if (plantCollision == null)
			return false; // 如果没有碰撞区域，假设可以放置
			
		foreach (var otherPlant in _plantsInArea)
		{
			if (otherPlant == plant) continue;
			
			// 检查otherPlant是否是子弹，如果是子弹则跳过
			if (IsBullet(otherPlant))
			{
				continue; // 忽略子弹
			}
			
			var otherCollision = otherPlant.GetNodeOrNull<Area2D>("PlantCollision");
			if (otherCollision != null && plantCollision.OverlapsArea(otherCollision))
			{
				return true; // 与其他植物重叠，不能放置
			}
		}
		
		return false;
	}
	
	/// <summary>
	/// 获取区域内的所有植物
	/// </summary>
	public List<Plant> GetPlantsInArea()
	{
		return new List<Plant>(_plantsInArea);
	}
	
	/// <summary>
	/// 清除区域内的所有植物（用于重置关卡）
	/// </summary>
	public void ClearPlants()
	{
		foreach (var plant in _plantsInArea)
		{
			if (IsInstanceValid(plant))
			{
				// 如果有网格系统，释放对应的格子
				if (_gridSystem != null)
				{
					Vector2I gridPos = _gridSystem.WorldToGrid(plant.GlobalPosition);
					_gridSystem.MarkGridFree(gridPos);
				}
				
				plant.QueueFree();
			}
		}
		_plantsInArea.Clear();
		
		// 如果有网格系统，重置所有格子
		if (_gridSystem != null)
		{
			_gridSystem.ResetGrid();
		}
	}
	
	/// <summary>
	/// 获取网格系统引用
	/// </summary>
	public GridSystem GetGridSystem()
	{
		return _gridSystem;
	}
	
	/// <summary>
	/// 设置网格系统引用
	/// </summary>
	public void SetGridSystem(GridSystem gridSystem)
	{
		_gridSystem = gridSystem;
	}
	
	/// <summary>
	/// 检查植物是否是子弹
	/// </summary>
	private bool IsBullet(Plant plant)
	{
		var sprite = plant.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite != null && sprite.Animation == "Bullet")
		{
			return true;
		}
		
		// 检查是否是射击植物并且是子弹状态
		if (plant is ShootingPlant shootingPlant)
		{
			// 尝试访问_shootingPlant的私有字段_isBullet
			// 由于无法直接访问私有字段，我们使用动画状态来判断
			return sprite?.Animation == "Bullet";
		}
		
		return false;
	}
}
