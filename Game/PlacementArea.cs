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

	public override void _Ready()
	{
		// 连接信号
		AreaEntered += OnAreaEntered;
		AreaExited += OnAreaExited;
		
		// 创建调试视觉（可选）
		if (ShowDebugVisual)
		{
			CreateDebugVisual();
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
				plant.Modulate = new Color(1, 1, 1, 0.7f); // 可以放置，半透明白色
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
				plant.Modulate = new Color(1, 0.5f, 0.5f, 0.7f); // 不能放置，半透明红色
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
	/// 检查植物是否可以放置在此处（考虑与其他植物的重叠）
	/// </summary>
	public bool CanPlacePlantHere(Plant plant)
	{
		// 首先检查是否在区域内
		if (!IsPointInside(plant.GlobalPosition))
			return false;
			
		// 检查是否与其他植物重叠
		var plantCollision = plant.GetNodeOrNull<Area2D>("PlantCollision");
		if (plantCollision == null)
			return true; // 如果没有碰撞区域，假设可以放置
			
		foreach (var otherPlant in _plantsInArea)
		{
			if (otherPlant == plant) continue;
			
			var otherCollision = otherPlant.GetNodeOrNull<Area2D>("PlantCollision");
			if (otherCollision != null && plantCollision.OverlapsArea(otherCollision)&&otherCollision.GetParent().GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D").Animation!="Bullet")
			{
				return false; // 与其他植物重叠，不能放置
			}
		}
		
		return true;
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
				plant.QueueFree();
			}
		}
		_plantsInArea.Clear();
	}
}
