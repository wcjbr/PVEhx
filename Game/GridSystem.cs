using Godot;
using System;

/// <summary>
/// 网格系统类，用于实现植物种植的格子吸附功能
/// </summary>
public partial class GridSystem : Node
{
	/// <summary>
	/// 每个格子的大小
	/// </summary>
	[Export]
	public Vector2 GridSize { get; set; } = new Vector2(80, 80);
	
	/// <summary>
	/// 网格起始位置（左上角）
	/// </summary>
	[Export]
	public Vector2 GridOffset { get; set; } = Vector2.Zero;
	
	/// <summary>
	/// 网格行数
	/// </summary>
	[Export]
	public int GridRows { get; set; } = 5;
	
	/// <summary>
	/// 网格列数
	/// </summary>
	[Export]
	public int GridCols { get; set; } = 9;
	
	/// <summary>
	/// 记录哪些格子已被占用
	/// </summary>
	private bool[,] _occupiedGrids;
	
	/// <summary>
	/// 最大搜索半径
	/// </summary>
	[Export]
	public int MaxSearchRadius { get; set; } = 3;
	
	/// <summary>
	/// 是否显示调试网格线
	/// </summary>
	[Export]
	public bool ShowDebugGrid { get; set; } = false;
	
	private Node2D _debugGridNode;
	
	public override void _Ready()
	{
		InitializeGrid();
		
		if (ShowDebugGrid)
		{
			CreateDebugGrid();
		}
	}
	
	/// <summary>
	/// 初始化网格系统
	/// </summary>
	private void InitializeGrid()
	{
		_occupiedGrids = new bool[GridRows, GridCols];
		
		// 初始化所有格子为空闲状态
		for (int row = 0; row < GridRows; row++)
		{
			for (int col = 0; col < GridCols; col++)
			{
				_occupiedGrids[row, col] = false;
			}
		}
		
		GD.Print($"GridSystem initialized: {GridCols}x{GridRows} grid, cell size {GridSize}");
	}
	
	/// <summary>
	/// 创建调试网格可视化
	/// </summary>
	private void CreateDebugGrid()
	{
		_debugGridNode = new Node2D();
		AddChild(_debugGridNode);
		
		var gridColor = new Color(0, 1, 0, 0.3f);
		
		// 绘制垂直线
		for (int col = 0; col <= GridCols; col++)
		{
			var line = new Line2D();
			line.DefaultColor = gridColor;
			line.Width = 1;
			
			Vector2 startPos = GridOffset + new Vector2(col * GridSize.X, 0);
			Vector2 endPos = GridOffset + new Vector2(col * GridSize.X, GridRows * GridSize.Y);
			
			line.AddPoint(startPos);
			line.AddPoint(endPos);
			
			_debugGridNode.AddChild(line);
		}
		
		// 绘制水平线
		for (int row = 0; row <= GridRows; row++)
		{
			var line = new Line2D();
			line.DefaultColor = gridColor;
			line.Width = 1;
			
			Vector2 startPos = GridOffset + new Vector2(0, row * GridSize.Y);
			Vector2 endPos = GridOffset + new Vector2(GridCols * GridSize.X, row * GridSize.Y);
			
			line.AddPoint(startPos);
			line.AddPoint(endPos);
			
			_debugGridNode.AddChild(line);
		}
	}
	
	/// <summary>
	/// 世界坐标转网格坐标
	/// </summary>
	public Vector2I WorldToGrid(Vector2 worldPos)
	{
		int gridX = Mathf.FloorToInt((worldPos.X - GridOffset.X) / GridSize.X);
		int gridY = Mathf.FloorToInt((worldPos.Y - GridOffset.Y) / GridSize.Y);
		
		// 边界检查
		gridX = Mathf.Clamp(gridX, 0, GridCols - 1);
		gridY = Mathf.Clamp(gridY, 0, GridRows - 1);
		
		return new Vector2I(gridX, gridY);
	}
	
	/// <summary>
	/// 网格坐标转世界坐标（格子中心）
	/// </summary>
	public Vector2 GridToWorld(int gridX, int gridY)
	{
		float worldX = GridOffset.X + gridX * GridSize.X + GridSize.X * 0.5f;
		float worldY = GridOffset.Y + gridY * GridSize.Y + GridSize.Y * 0.5f;
		return new Vector2(worldX, worldY);
	}
	
	/// <summary>
	/// 检查格子是否可用
	/// </summary>
	public bool IsGridAvailable(Vector2I gridPos)
	{
		if (gridPos.X < 0 || gridPos.X >= GridCols || gridPos.Y < 0 || gridPos.Y >= GridRows)
			return false;
			
		return !_occupiedGrids[gridPos.Y, gridPos.X];
	}
	
	/// <summary>
	/// 寻找最近可用格子
	/// </summary>
	public Vector2I? FindNearestAvailableGrid(Vector2 worldPos)
	{
		Vector2I currentGrid = WorldToGrid(worldPos);
		
		// 检查当前格子是否可用
		if (IsGridAvailable(currentGrid))
		{
			return currentGrid;
		}
		
		// 如果当前格子被占用，搜索附近格子
		for (int radius = 1; radius <= MaxSearchRadius; radius++)
		{
			var nearbyGrids = GetGridsWithinRadius(currentGrid, radius);
			
			foreach (var grid in nearbyGrids)
			{
				if (IsGridAvailable(grid))
				{
					return grid;
				}
			}
		}
		
		return null; // 没有可用格子
	}
	
	/// <summary>
	/// 获取指定半径内的所有格子
	/// </summary>
	private System.Collections.Generic.List<Vector2I> GetGridsWithinRadius(Vector2I centerGrid, int radius)
	{
		var grids = new System.Collections.Generic.List<Vector2I>();
		
		for (int dy = -radius; dy <= radius; dy++)
		{
			for (int dx = -radius; dx <= radius; dx++)
			{
				int newX = centerGrid.X + dx;
				int newY = centerGrid.Y + dy;
				
				// 检查边界
				if (newX >= 0 && newX < GridCols && newY >= 0 && newY < GridRows)
				{
					grids.Add(new Vector2I(newX, newY));
				}
			}
		}
		
		return grids;
	}
	
	/// <summary>
	/// 标记格子为已占用
	/// </summary>
	public void MarkGridOccupied(Vector2I gridPos)
	{
		if (gridPos.X >= 0 && gridPos.X < GridCols && gridPos.Y >= 0 && gridPos.Y < GridRows)
		{
			_occupiedGrids[gridPos.Y, gridPos.X] = true;
		}
	}
	
	/// <summary>
	/// 标记格子为空闲
	/// </summary>
	public void MarkGridFree(Vector2I gridPos)
	{
		if (gridPos.X >= 0 && gridPos.X < GridCols && gridPos.Y >= 0 && gridPos.Y < GridRows)
		{
			_occupiedGrids[gridPos.Y, gridPos.X] = false;
		}
	}
	
	/// <summary>
	/// 获取格子的边界矩形（世界坐标）
	/// </summary>
	public Rect2 GetGridRect(int gridX, int gridY)
	{
		Vector2 topLeft = GridOffset + new Vector2(gridX * GridSize.X, gridY * GridSize.Y);
		return new Rect2(topLeft, GridSize);
	}
	
	/// <summary>
	/// 检查世界坐标是否在网格区域内
	/// </summary>
	public bool IsPointInGridArea(Vector2 worldPos)
	{
		Rect2 gridArea = new Rect2(GridOffset, new Vector2(GridCols * GridSize.X, GridRows * GridSize.Y));
		return gridArea.HasPoint(worldPos);
	}
	
	/// <summary>
	/// 获取所有已占用的格子
	/// </summary>
	public System.Collections.Generic.List<Vector2I> GetOccupiedGrids()
	{
		var occupied = new System.Collections.Generic.List<Vector2I>();
		
		for (int row = 0; row < GridRows; row++)
		{
			for (int col = 0; col < GridCols; col++)
			{
				if (_occupiedGrids[row, col])
				{
					occupied.Add(new Vector2I(col, row));
				}
			}
		}
		
		return occupied;
	}
	
	/// <summary>
	/// 重置所有格子为未占用状态
	/// </summary>
	public void ResetGrid()
	{
		for (int row = 0; row < GridRows; row++)
		{
			for (int col = 0; col < GridCols; col++)
			{
				_occupiedGrids[row, col] = false;
			}
		}
	}
}
