using Godot;
using System;

/// <summary>
/// 植物类，继承自 AnimatedSprite2D，用于表示游戏中的植物实体。
/// 管理生命值、种植与拖动放置状态，并处理动画与输入事件。
/// </summary>
public partial class Plant : AnimatedSprite2D
{
	private int _hp;                     // 植物血量（HP）。-1 表示未初始化或不可用。
	private bool _isPlanted;             // 是否已种植（位于场地上）。
	private bool _isDragging;            // 是否在拖动（用于被复制的实例）
	private int _dragButton = -1;        // 用于开始拖动的鼠标按钮索引

	private bool _isBullet;
	private Vector2 _dragOffset = Vector2.Zero; // 鼠标相对节点位置的偏移（保持拖动时的相对位置）

	// 初始化：节点加入场景树后调用一次
	public override void _Ready()
	{
		_hp = -1;
		_isPlanted = false;
		_isDragging = false;
		_dragButton = -1;
		_dragOffset = Vector2.Zero;
		_isBullet = false;
	}

	// 每帧处理：更新动画状态 & 拖动跟随鼠标
	public override void _Process(double delta)
	{
		// 动画：已种植显示 Planted，否则显示 Carded（若存在）
		if (SpriteFrames != null && _isPlanted && SpriteFrames.HasAnimation("Planted"))
		{
			if (Animation != "Planted")
			{
				Animation = "Planted";
				Play();
			}
		}
		else if (SpriteFrames != null && SpriteFrames.HasAnimation("Carded"))
		{
			if (Animation != "Carded")
			{
				Animation = "Carded";
				Play();
			}
		}else if(_isBullet&& SpriteFrames != null && SpriteFrames.HasAnimation("Bullet"))
		{
			if (Animation != "Bullet")
			{
				Animation = "Bullet";
				Play();
			}
		}

		// 拖动时每帧让实例跟随鼠标（保留偏移）
		if (_isDragging)
		{
			GlobalPosition = GetGlobalMousePosition() + _dragOffset;
		}
		if (_isPlanted)
		{
			Plant NewPlant = new Plant();
			NewPlant._isBullet = true;
			NewPlant.Position = new Vector2(100, 100);
			GetParent().AddChild(NewPlant);
		}
		if(_isBullet)
		{
			Position += new Vector2(100, 0) * (float)delta;
		}
	}

	// 处理输入事件：按下时在原卡片上创建副本并开始拖动，释放时放置并种植
	public override void _Input(InputEvent @event)
	{
		// 鼠标按下/抬起处理
		if (@event is InputEventMouseButton mouseEvent)
		{
			int btn = (int)mouseEvent.ButtonIndex;

			// 按下：在原卡片上开始拖动副本（仅左键，且当前实例未种植且不是正在被拖动的副本）
			if (mouseEvent.Pressed && btn == (int)MouseButton.Left)
			{
				if (_isPlanted)
					return;

				// 判断鼠标是否在该植物图像上
				Vector2 mousePos = GetGlobalMousePosition();
				if (SpriteFrames == null || string.IsNullOrEmpty(Animation))
					return;

				var tex = SpriteFrames.GetFrameTexture(Animation, Frame);
				if (tex == null)
					return;

				Vector2 size = tex.GetSize() * GlobalScale;
				Rect2 rect = new Rect2(GlobalPosition - size * 0.5f, size);
				if (!rect.HasPoint(mousePos))
					return; // 鼠标未点中，忽略

				// 创建副本并开始拖动副本（原卡片保持不变）
				var dup = Duplicate() as Plant;
				if (dup != null && GetParent() != null)
				{
					GetParent().AddChild(dup);
					// 让副本立刻位于和原来相同位置并开始跟随鼠标，保持鼠标与精灵的相对偏移
					dup.GlobalPosition = GlobalPosition;
					dup._isDragging = true;
					dup._dragButton = btn;
					dup._dragOffset = dup.GlobalPosition - mousePos;
				}
			}
			// 抬起：如果是拖动状态并且抬起的是开始拖动的按钮，则完成放置并种植
			else if (!mouseEvent.Pressed)
			{
				// 只处理当前实例自己的拖动结束（防止原卡片响应）
				if (_isDragging && btn == _dragButton)
				{
					_isDragging = false;
					_dragButton = -1;
					_dragOffset = Vector2.Zero;
					_isPlanted = true;
					GD.Print("Plant placed at: " + GlobalPosition);
				}
			}
		}
	}
}
