using Godot;
using System;

/// <summary>
/// 网格覆盖层，用于显示网格状态（如不可种植的格子）
/// </summary>
public partial class GridOverlay : Node2D
{
	[Export]
	public GridSystem GridSystem { get; set; }
	
	// 可用格子颜色（透明，无特殊显示）
	private Color _availableColor = new Color(1, 1, 1, 0);
	
	// 不可用格子颜色（红色滤镜）
	private Color _unavailableColor = new Color(1, 0.3f, 0.3f, 0.4f);
	
	// 用于绘制网格覆盖层的Sprite2D数组
	private Sprite2D[,] _gridSprites;
	
	public override void _Ready()
	{
		if (GridSystem != null)
		{
			CreateGridOverlay();
		}
	}
	
	/// <summary>
	/// 创建网格覆盖层
	/// </summary>
	private void CreateGridOverlay()
	{
		if (GridSystem == null) return;
		
		_gridSprites = new Sprite2D[GridSystem.GridRows, GridSystem.GridCols];
		
		for (int row = 0; row < GridSystem.GridRows; row++)
		{
			for (int col = 0; col < GridSystem.GridCols; col++)
			{
				// 创建一个Sprite用于显示格子状态
				var sprite = new Sprite2D();
				sprite.Texture = GD.Load<Texture2D>("res://Assets/UI/white_pixel.png"); // 使用白色像素纹理
				sprite.Scale = new Vector2(GridSystem.GridSize.X / 16.0f, GridSystem.GridSize.Y / 16.0f); // 假设白色像素是16x16
				sprite.Centered = false;
				
				// 设置格子位置
				sprite.Position = GridSystem.GridToWorld(col, row) - GridSystem.GridSize * 0.5f;
				
				// 初始设置为可用状态（透明）
				sprite.Modulate = _availableColor;
				
				AddChild(sprite);
				_gridSprites[row, col] = sprite;
			}
		}
		
		// 定期更新网格状态
		UpdateGridOverlay();
	}
	
	/// <summary>
	/// 更新网格覆盖层显示
	/// </summary>
	public void UpdateGridOverlay()
	{
		if (GridSystem == null || _gridSprites == null) return;
		
		for (int row = 0; row < GridSystem.GridRows; row++)
		{
			for (int col = 0; col < GridSystem.GridCols; col++)
			{
				var gridPos = new Vector2I(col, row);
				var sprite = _gridSprites[row, col];
				
				if (!GridSystem.IsGridAvailable(gridPos))
				{
					// 格子不可用，显示红色滤镜
					sprite.Modulate = _unavailableColor;
				}
				else
				{
					// 格子可用，保持透明
					sprite.Modulate = _availableColor;
				}
			}
		}
	}
	
	public override void _Process(double delta)
	{
		// 持续更新网格状态显示
		UpdateGridOverlay();
	}
}