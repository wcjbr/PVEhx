using Godot;
using System;

/// <summary>
/// 简化的植物基类
/// </summary>
public partial class Plant : Node2D
{
	protected AnimatedSprite2D _sprite;
	protected bool _isPlanted = false;

	public override void _Ready()
	{
		// 尝试多种方法获取AnimatedSprite2D
		FindSprite();
		
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
					Scale = new Vector2(0.72f, 0.72f);
					_sprite.Animation = "Carded";
					_sprite.Play();
				}
			}
		}
	}
	
	public virtual void SetPlanted(bool planted)
	{
		_isPlanted = planted;
		
		// 如果种植后需要改变动画，可以在这里调用
		if (planted && _sprite != null)
		{
			UpdateAnimation();
		}
	}
	
	public bool IsPlanted()
	{
		return _isPlanted;
	}
}
